using Mirage.Common;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing.Windows;

namespace Mirage.Windowing;

/// <summary>
/// Manages the lifecycle and event processing of the application's windows.
/// </summary>
/// <remarks>
/// Window event processing is driven by the scheduler through
/// <see cref="IUpdatable.Update(double)"/>; individual windows do not own an update loop.
/// </remarks>
public class WindowManager : Module, IUpdatable
{
    private readonly Dictionary<string, Window> _windows = [];
    private bool _composed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> module.
    /// </summary>
    /// <param name="windows">The windows managed by the module.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple windows have the same identifier.
    /// </exception>
    public WindowManager(IEnumerable<Window>? windows = null)
        : base("WindowManager", dependencies: ["Scheduler"])
    {
        foreach (var window in windows ?? [])
        {
            ArgumentNullException.ThrowIfNull(window);

            if (!_windows.TryAdd(window.Identifier, window))
                throw new InvalidOperationException(
                    $"Duplicate window identifier found: '{window.Identifier}'."
                );
        }

        Windows = _windows.AsReadOnly();
    }

    /// <summary>
    /// Gets the managed windows, indexed by identifier.
    /// </summary>
    public IReadOnlyDictionary<string, Window> Windows { get; }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedWindows = Compose().ToArray();

        foreach (var window in composedWindows)
        {
            ArgumentNullException.ThrowIfNull(window);

            if (!_windows.TryAdd(window.Identifier, window))
                throw new InvalidOperationException(
                    $"Duplicate window identifier found: '{window.Identifier}'."
                );
        }

        _composed = true;
    }

    /// <summary>
    /// Composes the windows managed by this module.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the windows to register.
    /// </returns>
    /// <remarks>
    /// The default implementation does not compose any windows. Composition
    /// occurs once when the module starts, before any window is opened.
    /// Windows supplied to the constructor are registered before composed
    /// windows.
    /// </remarks>
    protected virtual IEnumerable<Window> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var window in _windows.Values)
            window.Destroy();

        _windows.Clear();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Opens every managed window. If opening a window fails, windows opened
    /// earlier during the same operation are closed in reverse order.
    /// </remarks>
    protected override void OnStart()
    {
        EnsureComposed();

        List<Window> openedWindows = [];

        try
        {
            foreach (var window in _windows.Values)
            {
                window.Open();
                openedWindows.Add(window);
            }
        }
        catch
        {
            for (var index = openedWindows.Count - 1; index >= 0; index--)
                openedWindows[index].Close();

            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Closes all open windows in reverse registration order.
    /// </remarks>
    protected override void OnStop()
    {
        foreach (var window in _windows.Values.Reverse())
        {
            if (window.Opened.Get())
                window.Close();
        }
    }

    /// <inheritdoc />
    public void Update(double _)
    {
        foreach (var window in _windows.Values)
            window.Process();
    }
}
