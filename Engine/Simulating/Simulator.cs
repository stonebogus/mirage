using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;
using Mirage.Simulating.Bindings;

namespace Mirage.Simulating;

public class Simulator : Module, IUpdatable
{
    private readonly Dictionary<SimulationSpace, SpaceBinding> _bindings = new(
        ReferenceEqualityComparer.Instance
    );

    private readonly List<(SimulationSpace Space, bool Add)> _pendingChanges = [];

    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;
    private bool _updating;

    public readonly ReactiveDictionary<string, SimulationSpace> Spaces = [];

    public readonly int SubstepCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Simulator"/> class.
    /// </summary>
    /// <param name="spaces">The initial simulation spaces, or <see langword="null"/> for none.</param>
    /// <param name="substepCount">The positive number of physics substeps performed for each update. The default is <c>4</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="substepCount"/> is not positive.
    /// </exception>
    public Simulator(IEnumerable<SimulationSpace>? spaces = null, int substepCount = 4)
        : base("Simulator")
    {
        if (substepCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(substepCount));

        SubstepCount = substepCount;

        foreach (var space in spaces ?? [])
        {
            Spaces.Add(space.Identifier, space);
        }

        Spaces.OnAdd.Connect(entry =>
        {
            if (State.Get() == ModuleState.Running)
                QueueOrRegister(entry.Value, add: true);
        });

        Spaces.OnRemove.Connect(entry =>
        {
            if (State.Get() == ModuleState.Running)
                QueueOrRegister(entry.Value, add: false);
        });
    }

    private void ApplyPendingChanges()
    {
        for (var i = 0; i < _pendingChanges.Count; i++)
        {
            var (space, add) = _pendingChanges[i];

            if (add)
                Register(space);
            else
                Unregister(space);
        }

        _pendingChanges.Clear();
    }

    private void DestroyBindings()
    {
        Exception? firstException = null;

        foreach (var binding in _bindings.Values)
        {
            try
            {
                binding.Destroy();
            }
            catch (Exception exception)
            {
                firstException ??= exception;
            }
        }

        _bindings.Clear();
        _pendingChanges.Clear();

        if (firstException is not null)
            throw firstException;
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

        foreach (var space in composedObjects)
        {
            if (Spaces.ContainsKey(space.Identifier) || !identifiers.Add(space.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate simulation space identifier found: '{space.Identifier}'."
                );
        }

        foreach (var space in composedObjects)
            Spaces.Add(space.Identifier, space);

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

    private void QueueOrRegister(SimulationSpace space, bool add)
    {
        if (_updating)
        {
            _pendingChanges.Add((space, add));
            return;
        }

        if (add)
            Register(space);
        else
            Unregister(space);
    }

    private void Register(SimulationSpace space)
    {
        if (_bindings.ContainsKey(space))
            return;

        space.Prepare();

        var binding = new SpaceBinding(space);

        try
        {
            _bindings.Add(space, binding);
        }
        catch
        {
            binding.Destroy();
            throw;
        }
    }

    private void Unregister(SimulationSpace space)
    {
        if (!_bindings.Remove(space, out var binding))
            return;

        binding.Destroy();
    }

    /// <summary>
    /// Composes the spaces managed by this object.
    /// </summary>
    /// <returns>The spaces to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the simulator first starts, before simulation bindings are created.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The simulator references spaces without owning or destroying them.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<SimulationSpace> Compose()
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

    protected override void OnDestroy()
    {
        base.OnDestroy();

        DestroyBindings();
        Spaces.Destroy();
    }

    protected override void OnStart()
    {
        EnsureComposed();
        EnsureConfigured();

        try
        {
            foreach (var space in Spaces.Values)
                Register(space);
        }
        catch
        {
            DestroyBindings();
            throw;
        }

        base.OnStart();
    }

    protected override void OnStop()
    {
        DestroyBindings();

        base.OnStop();
    }

    public void Update(UpdateContext context)
    {
        ThrowIfDestroyed();

        if (State.Get() != ModuleState.Running || context.DeltaTime <= 0)
            return;

        var deltaTime = (float)context.DeltaTime;

        if (!float.IsFinite(deltaTime))
            throw new ArgumentOutOfRangeException(nameof(context));

        _updating = true;

        try
        {
            foreach (var binding in _bindings.Values)
                binding.Step(deltaTime, SubstepCount);
        }
        finally
        {
            _updating = false;
            ApplyPendingChanges();
        }
    }
}
