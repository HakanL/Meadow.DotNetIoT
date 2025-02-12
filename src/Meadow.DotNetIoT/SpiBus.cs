using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Units;

namespace Haukcode.MeadowDotNetIoT;

public class SpiBus : ISpiBus, IDisposable
{
    private const int MAX_TX_BLOCK_SIZE_BYTES = 4096;
    private SemaphoreSlim _busSemaphore = new SemaphoreSlim(1, 1);
    private SpiDevice spiDevice;
    private SpiClockConfiguration? _clockConfig;
    private int busNumber;
    private int chipSelect;

    public SpiBus(int busNumber, int chipSelect, SpiMode mode, Meadow.Units.Frequency speed)
    {
        this.busNumber = busNumber;
        this.chipSelect = chipSelect;
        this.spiDevice = System.Device.Spi.SpiDevice.Create(new SpiConnectionSettings(busNumber, chipSelect)
        {
            ClockFrequency = (int)speed.Hertz,
            Mode = mode,
            DataBitLength = 8
        });

        Configuration = new SpiClockConfiguration(speed, mode switch
        {
            SpiMode.Mode0 => SpiClockConfiguration.Mode.Mode0,
            SpiMode.Mode1 => SpiClockConfiguration.Mode.Mode1,
            SpiMode.Mode2 => SpiClockConfiguration.Mode.Mode2,
            SpiMode.Mode3 => SpiClockConfiguration.Mode.Mode3,
            _ => throw new ArgumentOutOfRangeException(),
        });
    }

    public int SpeedHz => this.spiDevice.ConnectionSettings.ClockFrequency;

    internal SpiMode Mode => this.spiDevice.ConnectionSettings.Mode;

    /// <inheritdoc/>
    public int BitsPerWord => this.spiDevice.ConnectionSettings.DataBitLength;

    /// <inheritdoc/>
    public Frequency[] SupportedSpeeds
    {
        get =>
            [
                new Frequency(375, Frequency.UnitType.Kilohertz),
                new Frequency(750, Frequency.UnitType.Kilohertz),
                new Frequency(1500, Frequency.UnitType.Kilohertz),
                new Frequency(3000, Frequency.UnitType.Kilohertz),
                new Frequency(6000, Frequency.UnitType.Kilohertz),
                new Frequency(12000, Frequency.UnitType.Kilohertz),
                new Frequency(24000, Frequency.UnitType.Kilohertz),
                new Frequency(48000, Frequency.UnitType.Kilohertz),
            ];
    }

    /// <summary>
    /// Configuration to use for this instance of the SPIBus.
    /// </summary>
    public SpiClockConfiguration Configuration
    {
        get
        {
            if (_clockConfig == null)
            {
                Configuration = new SpiClockConfiguration(
                    new Meadow.Units.Frequency(375, Meadow.Units.Frequency.UnitType.Kilohertz),
                    SpiClockConfiguration.Mode.Mode0);

                return Configuration;
            }
            return _clockConfig;
        }
        internal set
        {
            if (value == null)
            { throw new ArgumentNullException(); }

            if (_clockConfig != null)
            {
                _clockConfig.Changed -= OnConfigChanged;
            }

            _clockConfig = value;

            HandleConfigChange();

            _clockConfig.Changed += OnConfigChanged;
        }
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        HandleConfigChange();
    }

    private void HandleConfigChange()
    {
        bool changed = false;

        if (SpeedHz != (int)Configuration.Speed.Hertz)
            changed = true;

        if (Mode != (SpiMode)Configuration.SpiMode)
            changed = true;

        var newMode = Configuration.SpiMode switch
        {
            SpiClockConfiguration.Mode.Mode0 => SpiMode.Mode0,
            SpiClockConfiguration.Mode.Mode1 => SpiMode.Mode1,
            SpiClockConfiguration.Mode.Mode2 => SpiMode.Mode2,
            SpiClockConfiguration.Mode.Mode3 => SpiMode.Mode3,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (Mode != newMode)
            changed = true;

        if (BitsPerWord != Configuration.BitsPerWord)
            changed = true;

        if (changed)
        {
            // Create new spiDevice
            _busSemaphore.Wait();

            try
            {
                this.spiDevice.Dispose();

                this.spiDevice = System.Device.Spi.SpiDevice.Create(new SpiConnectionSettings(this.busNumber, this.chipSelect)
                {
                    ClockFrequency = (int)Configuration.Speed.Hertz,
                    Mode = newMode,
                    DataBitLength = Configuration.BitsPerWord
                });
            }
            finally
            {
                _busSemaphore.Release();
            }

            //Console.WriteLine($"Changed to: Speed: {SpeedHz:N0}, Mode: {Mode}, BitsPerWord: {BitsPerWord}");
        }
    }

    public void Exchange(IDigitalOutputPort? chipSelect, Span<byte> writeBuffer, Span<byte> readBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
    {
        if (writeBuffer.Length != readBuffer.Length)
            throw new Exception("Both buffers must be equal size");

        _busSemaphore.Wait();

        try
        {
            if (chipSelect != null)
            {
                // activate the chip select
                chipSelect.State = csMode == ChipSelectMode.ActiveLow ? false : true;
            }

            // each write can't be bigger than MAX_TX_BLOCK_SIZE_BYTES
            var offset = 0;
            while (offset < writeBuffer.Length)
            {
                var length = (writeBuffer.Length - offset) > MAX_TX_BLOCK_SIZE_BYTES ? MAX_TX_BLOCK_SIZE_BYTES : (writeBuffer.Length - offset);

                this.spiDevice.TransferFullDuplex(writeBuffer.Slice(offset, length), readBuffer.Slice(offset, length));

                offset += length;
            }

            if (chipSelect != null)
            {
                // deactivate the chip select
                chipSelect.State = csMode == ChipSelectMode.ActiveLow ? true : false;
            }
        }
        finally
        {
            _busSemaphore.Release();
        }
    }

    public void Read(IDigitalOutputPort? chipSelect, Span<byte> readBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
    {
        byte[] writeBuffer = new byte[readBuffer.Length];
        Exchange(chipSelect, writeBuffer, readBuffer, csMode);
    }

    public void Write(IDigitalOutputPort? chipSelect, Span<byte> writeBuffer, ChipSelectMode csMode = ChipSelectMode.ActiveLow)
    {
        byte[] readBuffer = new byte[writeBuffer.Length];
        Exchange(chipSelect, writeBuffer, readBuffer, csMode);
    }

    public void Dispose()
    {
        this.spiDevice.Dispose();
    }
}
