using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Haukcode.MeadowDotNetIoT;

internal class OutputPortAdapter : IDigitalOutputPort
{
    private readonly GpioController gpioController;
    private readonly GpioPin pin;

    public OutputPortAdapter(GpioController gpioController, IPin pin, bool initialState)
    {
        this.gpioController = gpioController;

        var myPin = pin as AdapterPin;

        if (myPin == null)
        {
            throw new ArgumentException("Invalid pin type");
        }

        Pin = pin;
        InitialState = initialState;
        this.pin = gpioController.OpenPin(myPin.GpioPinId, PinMode.Output, initialState);
    }

    /// <inheritdoc/>
    public bool InitialState { get; private set; }

    private bool LastState { get; set; }

    /// <inheritdoc/>
    public bool State
    {
        get => LastState;
        set
        {
            this.pin.Write(value ? PinValue.High : PinValue.Low);

            LastState = value;
        }
    }

    public IDigitalChannelInfo Channel => throw new NotImplementedException();

    public IPin Pin { get; private set; }

    public void Dispose()
    {
        this.gpioController.ClosePin(this.pin.PinNumber);
    }
}
