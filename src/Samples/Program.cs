using System.Device.Spi;
using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Peripherals.Displays;

namespace Haukcode.MeadowDotNetIoT.Sample;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("ST7789 sample");

        using var gpioController = new System.Device.Gpio.GpioController();

        var myAdapter = new MeadowAdapter(gpioController);

        var spiAdapter = myAdapter.CreateSpiBus(0, new Meadow.Units.Frequency(1500, Meadow.Units.Frequency.UnitType.Kilohertz));
        var chipSelectPin = myAdapter.Pins.GPIO8;
        var dataCommandPin = myAdapter.Pins.GPIO25;

        var backlightPin = myAdapter.Pins.GPIO22;
        var backlightPort = backlightPin.CreateDigitalOutputPort();
        backlightPort.State = true;

        var buttonA = new PollingPushButton(myAdapter.Pins.GPIO23);
        var buttonB = new PushButton(myAdapter.Pins.GPIO24);

        buttonA.PressStarted += (s, e) =>
        {
            Console.WriteLine("ButtonA pressed");
        };
        buttonA.PressEnded += (s, e) =>
        {
            Console.WriteLine("ButtonA released");
        };

        buttonB.PressStarted += (s, e) =>
        {
            Console.WriteLine("ButtonB pressed");
        };
        buttonB.PressEnded += (s, e) =>
        {
            Console.WriteLine("ButtonB released");
        };

        var display = new St7789(
            spiBus: spiAdapter,
            chipSelectPin: chipSelectPin,
            dcPin: dataCommandPin,
            resetPin: null,
            width: 135,
            height: 240);

        display.Clear(Color.AliceBlue);
        display.Show();

        var graphics = new MicroGraphics(display)
        {
            Rotation = RotationType._90Degrees,
            IgnoreOutOfBoundsPixels = true
        };

        var testClass = new TestClass(graphics);

        testClass.Run();
    }
}
