using Mirage.Handling.Devices;
using Mirage.Windowing.SDL3;
using Mirage.Windowing.Windows.SDL3;

namespace Mirage.Handling.SDL3;

/// <summary>
/// Provides SDL3 input contexts to registered input devices.
/// </summary>
public class SDL3InputHandler(
    int index,
    SDL3Window window,
    IEnumerable<InputDevice>? devices = null
) : InputHandler(index, window, devices)
{
    private SDL3Window SDLWindow => (SDL3Window)Window;

    /// <inheritdoc />
    protected override InputContext CreateContext()
    {
        return new SDL3InputContext(SDLWindow, SDLWindow.FrameEvents);
    }
}
