using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Units;

namespace Haukcode.MeadowDotNetIoT;

public class MeadowAdapter : IPinController,
    IDigitalOutputController,
    IDigitalInputOutputController,
    IDigitalInterruptController,
    ISpiController
{
    private readonly GpioController gpioController;

    public MeadowAdapter(GpioController gpioController)
    {
        this.gpioController = gpioController;

        Pins = new RpiHardwarePinout(this);
    }

    public RpiHardwarePinout Pins { get; }

    /// <inheritdoc/>
    public IDigitalInputPort CreateDigitalInputPort(IPin pin)
    {
        return CreateDigitalInputPort(pin, ResistorMode.Disabled);
    }

    /// <inheritdoc/>
    public IDigitalInputPort CreateDigitalInputPort(IPin pin, ResistorMode resistorMode)
    {
        return new InputPortAdapter(this.gpioController, pin, new DigitalChannelInfo(pin.Name), resistorMode);
    }

    /// <inheritdoc/>
    public IDigitalInterruptPort CreateDigitalInterruptPort(IPin pin, InterruptMode interruptMode, ResistorMode resistorMode, TimeSpan debounceDuration, TimeSpan glitchDuration)
    {
        return new InterruptPortAdapter(this.gpioController, pin, new DigitalChannelInfo(pin.Name), interruptMode, resistorMode, debounceDuration, glitchDuration);
    }

    /// <inheritdoc/>
    public IDigitalOutputPort CreateDigitalOutputPort(IPin pin, bool initialState = false, OutputType initialOutputType = OutputType.PushPull)
    {
        return new OutputPortAdapter(this.gpioController, pin, initialState);
    }

    /// <inheritdoc/>
    public IDigitalSignalAnalyzer CreateDigitalSignalAnalyzer(IPin pin, bool captureDutyCycle)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual ISpiBus CreateSpiBus(int busNumber, Meadow.Units.Frequency speed)
    {
        return new SpiBus(busNumber, -1, System.Device.Spi.SpiMode.Mode0, speed);
    }

    /// <inheritdoc/>
    public ISpiBus CreateSpiBus(IPin clock, IPin mosi, IPin miso, SpiClockConfiguration.Mode mode, Meadow.Units.Frequency speed)
    {
        // TODO: validate pins for both buses

        // just switch on the clock, assume they did the rest right
        if (clock.Key.ToString() == "PIN40")
        {
            return new SpiBus(1, 0, (System.Device.Spi.SpiMode)mode, speed);
        }

        return new SpiBus(0, 0, (System.Device.Spi.SpiMode)mode, speed);
    }

    /// <inheritdoc/>
    public ISpiBus CreateSpiBus(IPin clock, IPin mosi, IPin miso, SpiClockConfiguration config)
    {
        return CreateSpiBus(clock, mosi, miso, config.SpiMode, config.Speed);
    }

    /// <inheritdoc/>
    public ISpiBus CreateSpiBus(IPin clock, IPin copi, IPin cipo, Frequency speed)
    {
        return CreateSpiBus(clock, copi, cipo, SpiClockConfiguration.Mode.Mode0, speed);
    }
}
