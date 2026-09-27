using Mirage.Common;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Windowing;

/// <summary>
/// Manages the lifecycle and event processing of the application's windows.
/// </summary>
/// <remarks>
/// Window event processing is driven by the scheduler through
/// <see cref="IUpdatable.Update(UpdateContext)"/>; individual windows do not own an update loop.
/// The manager owns and destroys its registered windows.
/// </remarks>
public class WindowManager : Module, IUpdatable
{
    private readonly Dictionary<string, Window> _windows = [];
    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the managed windows, indexed by identifier.
    /// </summary>
    public readonly IReadOnlyDictionary<string, Window> Windows;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> module.
    /// </summary>
    /// <param name="windows">The initial windows, or <see langword="null"/> for none.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple windows have the same identifier.
    /// </exception>
    public WindowManager(IEnumerable<Window>? windows = null)
        : base("WindowManager", dependencies: ["Scheduler"])
    {
        foreach (var window in windows ?? [])
        {
            if (!_windows.TryAdd(window.Identifier, window))
                throw new InvalidOperationException(
                    $"Duplicate window identifier found: '{window.Identifier}'."
                );
        }

        Windows = _windows.AsReadOnly();
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the module is not running, including before configuration has completed.
    /// </exception>
    public void Update(UpdateContext context)
    {
        ThrowIfDestroyed();

        if (State.Get() != ModuleState.Running)
            throw new InvalidOperationException(
                "The module must be running before processing updates."
            );

        foreach (var window in _windows.Values)
            window.Update(context);
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<string> identifiers = [];

        foreach (var window in composedObjects)
        {
            if (_windows.ContainsKey(window.Identifier) || !identifiers.Add(window.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate window identifier found: '{window.Identifier}'."
                );
        }

        foreach (var window in composedObjects)
            _windows.Add(window.Identifier, window);

        _composed = true;
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        _configurationStarted = true;
        Configure();
        _configured = true;
    }

    /// <summary>
    /// Composes the windows managed by this object.
    /// </summary>
    /// <returns>The windows to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the manager first starts, before windows are opened.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The manager owns and destroys its registered windows.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<Window> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition, before startup or first use.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed objects are available here.
    /// This hook is invoked at most once, including across later lifecycle cycles.
    /// If configuration throws, later lifecycle calls reject further initialization
    /// rather than repeating configuration side effects.
    /// </remarks>
    protected virtual void Configure() { }

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
        EnsureConfigured();

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
}
