using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Handling.Devices;
using Mirage.Logging;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Handling;

/// <summary>
/// Registers input devices and updates them with events from one window.
/// </summary>
/// <remarks>Constructor-provided and composed devices belong to the handler.
/// The window is borrowed and is never destroyed by the handler.</remarks>
public class InputHandler : Module, IUpdatable
{
    private readonly IdentifiableSet<string, InputDevice> _devices = [];
    private SourcedLogger Logger => Require<Logger>("Logger").From(Identifier);
    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the read-only identifiable set of devices owned by this handler.
    /// </summary>
    public readonly IReadOnlyIdentifiableSet<string, InputDevice> Devices;

    /// <summary>
    /// Gets the window whose frame events are processed by this handler.
    /// </summary>
    public readonly Window Window;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputHandler"/> class.
    /// </summary>
    /// <param name="index">The index used to distinguish the module identifier.</param>
    /// <param name="window">The window supplying input events.</param>
    /// <param name="devices">The devices to register initially, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial devices contain duplicate identifiers.
    /// </exception>
    public InputHandler(int index, Window window, IEnumerable<InputDevice>? devices = null)
        : base($"InputHandler-{index}", ["Logger"])
    {
        Window = window;
        foreach (var device in devices ?? [])
        {
            _devices.Add(device);
        }
        Devices = _devices;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the module is not running, including before configuration has completed.
    /// </exception>
    public virtual void Update(UpdateContext updateContext)
    {
        ThrowIfDestroyed();

        if (State.Get() != ModuleState.Running)
            throw new InvalidOperationException(
                "The module must be running before processing updates."
            );

        var context = new InputContext(Window, Window.FrameEvents);
        foreach (var device in _devices)
        {
            try
            {
                device.Process(context);
            }
            catch (Exception exception)
            {
                Logger.Log(
                    $"Input device '{device.Identifier}' failed: {exception.Message}",
                    LogMessageKind.Warn
                );
                throw;
            }
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        Logger.Log("Composing module contents.", LogMessageKind.Debug);
        _compositionStarted = true;

        foreach (var device in Compose())
        {
            try
            {
                _devices.Add(device);
            }
            catch
            {
                if (!_devices.Contains(device) && !device.Destroyed)
                    device.Destroy();

                throw;
            }
        }

        _composed = true;
        Logger.Log("Composition completed.", LogMessageKind.Debug);
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        Logger.Log("Configuring module.", LogMessageKind.Debug);
        _configurationStarted = true;
        Configure();
        _configured = true;
        Logger.Log("Configuration completed.", LogMessageKind.Debug);
    }

    /// <summary>
    /// Composes the devices managed by this object.
    /// </summary>
    /// <returns>The devices to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the handler first starts, before input processing.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The handler owns and destroys its devices, but not its window.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<InputDevice> Compose()
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
    /// <remarks>This handler destroys its registered devices.</remarks>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var device in _devices.ToArray())
        {
            device.Destroy();
            if (InjectedDependencies is not null)
                Logger.Log($"Destroyed input device '{device.Identifier}'.", LogMessageKind.Debug);
        }

        _devices.Destroy();
        if (InjectedDependencies is not null)
            Logger.Log("Module resources destroyed.");
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        Logger.Log("Module stopped.");
        base.OnStop();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        Logger.Log("Starting module.");
        EnsureComposed();
        EnsureConfigured();
        foreach (var device in _devices)
            Logger.Log(
                $"Input device '{device.Identifier}' ready for window '{Window.Identifier}'.",
                LogMessageKind.Debug
            );
        Logger.Log("Module started.");
    }
}
