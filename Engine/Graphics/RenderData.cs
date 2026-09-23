using Mirage.Graphics.Commands;

namespace Mirage.Graphics;

/// <summary>
/// Contains the rendering commands produced by a renderable object.
/// </summary>
public sealed class RenderData
{
    private readonly List<RenderCommand> _commands = [];

    /// <summary>
    /// Gets the commands in their execution order.
    /// </summary>
    public IReadOnlyList<RenderCommand> Commands => _commands;

    /// <summary>
    /// Adds a rendering command.
    /// </summary>
    public void Add(RenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Add(command);
    }
}
