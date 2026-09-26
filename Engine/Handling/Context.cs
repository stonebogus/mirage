using Mirage.Windowing;
using SDL3;

namespace Mirage.Handling;

public class InputContext
{
    public readonly IReadOnlyList<SDL.Event> FrameEvents;
    public readonly Window Window;

    public InputContext(Window window, IEnumerable<SDL.Event> events)
    {
        Window = window;
        FrameEvents = [.. events];
    }
}
