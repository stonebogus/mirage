using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Common.Telemetry;

namespace Mirage.Common;

internal sealed class ModuleContainer(IEnumerable<Module> modules)
{
    private readonly IReadOnlyList<Module> _modules = [.. modules];

    public TModule Get<TModule>()
        where TModule : Module
    {
        return _modules.OfType<TModule>().Single();
    }

    public Module Get(string identifier)
    {
        return _modules.Single(module => module.Identifier == identifier);
    }
}

internal sealed class ModuleContext
{
    public required ModuleContainer Modules { get; init; }

    public required Telemetry.Telemetry Telemetry { get; init; }
}

/// <summary>
/// Represents the current lifecycle state of a module.
/// </summary>
public enum ModuleState
{
    /// <summary>
    /// Indicates that the module is idle and not currently running.
    /// </summary>
    Idle,

    /// <summary>
    /// Indicates that the module is currently starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Indicates that the module is running.
    /// </summary>
    Running,

    /// <summary>
    /// Indicates that the module is currently stopping.
    /// </summary>
    Stopping,
}

/// <summary>
/// Represents a game module with a managed lifecycle and declared dependencies.
/// </summary>
/// <remarks>
/// Modules are initialized and managed by a <see cref="Game"/> instance.
/// Their dependencies are injected before the module is started.
/// </remarks>
public abstract class Module : Destroyable
{
    private readonly Dictionary<string, Module> _injectedDependencies = [];
    private readonly Store<ModuleState> _state = new(ModuleState.Idle);
    private bool _injected;

    /// <summary>
    /// Gets the identifiers of the modules required by this module.
    /// </summary>
    public readonly IReadOnlyList<string> Dependencies;

    /// <summary>
    /// Gets the unique module identifier.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="Module"/> class.
    /// </summary>
    /// <param name="identifier">
    /// The unique identifier of the module.
    /// </param>
    /// <param name="dependencies">
    /// The identifiers of the modules required by this module.
    /// </param>
    protected Module(string identifier, IEnumerable<string>? dependencies = null)
    {
        Identifier = identifier;
        Dependencies = [.. dependencies ?? []];

        State = _state;
    }

    /// <summary>
    /// Gets the telemetry manager available to the module after injection.
    /// </summary>
    protected Telemetry.Telemetry Telemetry { get; private set; } = null!;

    /// <summary>
    /// Gets a read-only store for the current lifecycle state of the module.
    /// </summary>
    public IReadOnlyStore<ModuleState> State { get; }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (_state.Get() != ModuleState.Idle)
            throw new InvalidOperationException(
                $"Module '{Identifier}' cannot be destroyed while in state '{_state.Get()}'."
            );

        _state.Destroy();

        if (_injected)
            Telemetry.Send(
                $"Module '{Identifier}' has been destroyed.",
                Identifier,
                MessageKind.Debug
            );
    }

    /// <summary>
    /// Called when the module starts.
    /// </summary>
    /// <remarks>
    /// Override this method to perform module-specific startup logic.
    /// </remarks>
    protected virtual void OnStart() { }

    /// <summary>
    /// Called when the module stops.
    /// </summary>
    /// <remarks>
    /// Override this method to perform module-specific shutdown logic.
    /// </remarks>
    protected virtual void OnStop() { }

    /// <summary>
    /// Gets an injected dependency of the specified type.
    /// </summary>
    /// <typeparam name="TModule">The expected type of the dependency.</typeparam>
    /// <param name="name">The identifier of the dependency.</param>
    /// <returns>The injected dependency.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the module has not been injected, the dependency was not
    /// registered, or the dependency is not of the requested type.
    /// </exception>
    protected TModule Require<TModule>(string name)
        where TModule : Module
    {
        ThrowIfDestroyed();

        if (!_injectedDependencies.TryGetValue(name, out var module))
            throw new InvalidOperationException(
                $"Module '{Identifier}' requires dependency '{name}', but it was not injected."
            );

        if (module is not TModule typedModule)
            throw new InvalidOperationException(
                $"Module '{Identifier}' requires dependency '{name}' to be of type '{typeof(TModule).Name}', but it is '{module.GetType().Name}'."
            );

        return typedModule;
    }

    internal void Inject(ModuleContext context)
    {
        ThrowIfDestroyed();

        if (_injected)
            throw new InvalidOperationException(
                $"Module '{Identifier}' has already been injected."
            );

        Telemetry = context.Telemetry;

        foreach (var dependency in Dependencies)
            _injectedDependencies.Add(dependency, context.Modules.Get(dependency));

        _injected = true;

        Telemetry.Send($"Module '{Identifier}' has been injected.", Identifier, MessageKind.Debug);
    }

    internal void Start()
    {
        ThrowIfDestroyed();

        if (!_injected)
            throw new InvalidOperationException(
                $"Module '{Identifier}' has not been injected, cannot start."
            );

        var currentState = _state.Get();

        if (currentState != ModuleState.Idle)
            throw new InvalidOperationException(
                $"Module '{Identifier}' cannot be started from state '{currentState}'."
            );

        _state.Set(ModuleState.Starting);

        Telemetry.Send($"Starting module '{Identifier}'.", Identifier);

        try
        {
            OnStart();

            _state.Set(ModuleState.Running);

            Telemetry.Send($"Module '{Identifier}' started successfully.", Identifier);
        }
        catch (Exception)
        {
            _state.Set(ModuleState.Idle);

            throw;
        }
    }

    internal void Stop()
    {
        ThrowIfDestroyed();

        if (!_injected)
            throw new InvalidOperationException(
                $"Module '{Identifier}' has not been injected, cannot stop."
            );

        var currentState = _state.Get();

        if (currentState != ModuleState.Running)
            throw new InvalidOperationException(
                $"Module '{Identifier}' cannot be stopped from state '{currentState}'."
            );

        _state.Set(ModuleState.Stopping);

        Telemetry.Send($"Stopping module '{Identifier}'.", Identifier);

        try
        {
            OnStop();

            _state.Set(ModuleState.Idle);

            Telemetry.Send($"Module '{Identifier}' stopped successfully.", Identifier);
        }
        catch (Exception)
        {
            _state.Set(ModuleState.Running);

            throw;
        }
    }
}
