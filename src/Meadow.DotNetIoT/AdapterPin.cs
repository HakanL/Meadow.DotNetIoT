using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Haukcode.MeadowDotNetIoT;

public class AdapterPin : Pin
{
    public int GpioPinId { get; private set; }

    public int LineId { get; private set; }

    /// <summary>
    /// Creates a LinuxFlexiPin instance
    /// </summary>
    /// <param name="controller">The owner of this pin</param>
    /// <param name="name">The friendly pin name</param>
    /// <param name="key">The pin's internal key</param>
    /// <param name="supportedChannels">A list of supported channels</param>
    public AdapterPin(IPinController? controller, string name, object key, int gpioPinId, int lineId, IList<IChannelInfo>? supportedChannels = null)
        : base(controller, name, key, supportedChannels)
    {
        GpioPinId = gpioPinId;
        LineId = lineId;
    }
}
