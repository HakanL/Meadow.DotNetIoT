using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Haukcode.MeadowDotNetIoT;

/// <summary>
/// Represents digital channel information for a GPIO pin controlled by GPIO character device (gpiod).
/// </summary>
internal class DigitalChannelInfo : DigitalChannelInfoBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DigitalChannelInfo"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the GPIO digital channel.</param>
    public DigitalChannelInfo(string name)
        : base(name, true, true, true, true, true, false, null)
    {
    }
}
