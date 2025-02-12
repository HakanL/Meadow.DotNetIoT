using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Haukcode.MeadowDotNetIoT;

internal class InterruptPortAdapter : DigitalInterruptPortBase
{
    private readonly GpioController gpioController;
    private readonly GpioPin pin;
    private int? lastInterrupt = null;
    private InterruptMode interruptMode;

    public override TimeSpan DebounceDuration { get; set; }

    public InterruptPortAdapter(
        GpioController gpioController,
        IPin pin,
        DigitalChannelInfo channel,
        InterruptMode interruptMode,
        ResistorMode resistorMode,
        TimeSpan debounceDuration,
        TimeSpan glitchDuration)
        : base(pin, channel)
    {
        this.gpioController = gpioController;
        this.interruptMode = interruptMode;

        DebounceDuration = debounceDuration;
        GlitchDuration = glitchDuration;

        var myPin = pin as AdapterPin;

        if (myPin == null)
        {
            throw new ArgumentException("Invalid pin type");
        }

        Pin = pin;
        var pinMode = GetPinModeFromResistorMode(resistorMode);

        this.pin = gpioController.OpenPin(myPin.GpioPinId, pinMode);
        this.pin.ValueChanged += Pin_ValueChanged;
    }

    private void Pin_ValueChanged(object sender, PinValueChangedEventArgs e)
    {
        switch (InterruptMode)
        {
            case InterruptMode.None:
                return;

            case InterruptMode.EdgeRising:
                if (e.ChangeType != PinEventTypes.Rising)
                    return;
                break;

            case InterruptMode.EdgeFalling:
                if (e.ChangeType != PinEventTypes.Falling)
                    return;
                break;
        }

        if (DebounceDuration.TotalMilliseconds > 0)
        {
            int now = Environment.TickCount;

            if (this.lastInterrupt != null && now - this.lastInterrupt < DebounceDuration.TotalMilliseconds)
                return;

            this.lastInterrupt = now;
        }

        bool state = e.ChangeType == PinEventTypes.Rising ? true : false;

        RaiseChangedAndNotify(new DigitalPortResult { New = new DigitalState(state, DateTime.UtcNow) }); // TODO: convert event time?
    }

    private PinMode GetPinModeFromResistorMode(ResistorMode input)
    {
        return input switch
        {
            ResistorMode.InternalPullDown => PinMode.InputPullDown,
            ResistorMode.InternalPullUp => PinMode.InputPullUp,
            _ => PinMode.Input
        };
    }

    private ResistorMode GetResistorModeFromPinMode(PinMode input)
    {
        return input switch
        {
            PinMode.InputPullDown => ResistorMode.InternalPullDown,
            PinMode.InputPullUp => ResistorMode.InternalPullUp,
            _ => ResistorMode.Disabled
        };
    }

    public override bool State => this.pin.Read() == PinValue.High;

    /// <inheritdoc/>
    public override ResistorMode Resistor
    {
        get => GetResistorModeFromPinMode(this.pin.GetPinMode());
        set => this.pin.SetPinMode(GetPinModeFromResistorMode(value));
    }

    /// <inheritdoc/>
    public override InterruptMode InterruptMode
    {
        get => this.interruptMode;
        set => this.interruptMode = value;
    }

    /// <inheritdoc/>
    public override TimeSpan GlitchDuration
    {
        get => TimeSpan.Zero;
        set
        {
            if (GlitchDuration == TimeSpan.Zero)
            { return; }

            throw new NotSupportedException("Glitch filtering is not currently supported on this platform.");
        }
    }

    protected override void Dispose(bool disposing)
    {
        this.gpioController.ClosePin(this.pin.PinNumber);

        base.Dispose(disposing);
    }
}
