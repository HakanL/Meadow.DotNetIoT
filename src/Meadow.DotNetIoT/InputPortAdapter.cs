using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Haukcode.MeadowDotNetIoT;

internal class InputPortAdapter : DigitalInputPortBase
{
    private readonly GpioController gpioController;
    private readonly GpioPin pin;


    public InputPortAdapter(GpioController gpioController, IPin pin, DigitalChannelInfo channel, ResistorMode resistorMode)
        : base(pin, channel)
    {
        this.gpioController = gpioController;

        var myPin = pin as AdapterPin;

        if (myPin == null)
        {
            throw new ArgumentException("Invalid pin type");
        }

        Pin = pin;
        var pinMode = GetPinModeFromResistorMode(resistorMode);

        this.pin = gpioController.OpenPin(myPin.GpioPinId, pinMode);
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

    protected override void Dispose(bool disposing)
    {
        this.gpioController.ClosePin(this.pin.PinNumber);

        base.Dispose(disposing);
    }
}
