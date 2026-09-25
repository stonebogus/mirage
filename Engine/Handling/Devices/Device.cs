using Mirage.Common.Collections;
using Mirage.Common.Lifecycle;

namespace Mirage.Handling.Devices;

public abstract class InputDevice(string identifier) : Destroyable
{
    public string Identifier { get; } = identifier;
}
