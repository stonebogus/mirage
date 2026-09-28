using Box2D.NET;
using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Physics.Interfaces;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Mirage.Simulating.Bindings;

internal class SpaceBinding : Destroyable
{
    private readonly Dictionary<ISimulatable, BodyBinding> _bodies = [];
    private readonly EventConnection<ISimulatable> _onAdd;
    private readonly EventConnection<ISimulatable> _onRemove;

    public SpaceBinding(SimulationSpace space)
    {
        Space = space;

        var gravity = space.Gravity.Get();

        var definition = b2DefaultWorldDef();
        definition.gravity = new B2Vec2(gravity.X, gravity.Y);

        World = b2CreateWorld(definition);

        try
        {
            foreach (var simulatable in space.Simulatables)
                Add(simulatable);
        }
        catch
        {
            foreach (var binding in _bodies.Values)
                binding.Destroy();

            _bodies.Clear();
            b2DestroyWorld(World);
            throw;
        }

        _onAdd = space.Simulatables.OnAdd.Connect(Add);
        _onRemove = space.Simulatables.OnRemove.Connect(Remove);
    }

    public SimulationSpace Space { get; }

    public B2WorldId World { get; }

    public void Add(ISimulatable simulatable)
    {
        ThrowIfDestroyed();
        if (_bodies.ContainsKey(simulatable))
            return;

        var binding = new BodyBinding(World, simulatable);
        try
        {
            _bodies.Add(simulatable, binding);
        }
        catch
        {
            binding.Destroy();
            throw;
        }
    }

    protected override void OnDestroy()
    {
        _onAdd.Disconnect();
        _onRemove.Disconnect();

        foreach (var binding in _bodies.Values)
            binding.Destroy();

        _bodies.Clear();
        b2DestroyWorld(World);
        base.OnDestroy();
    }

    public void Remove(ISimulatable simulatable)
    {
        ThrowIfDestroyed();
        if (!_bodies.Remove(simulatable, out var binding))
            return;

        binding.Destroy();
    }

    public void Step(float deltaTime, int substepCount)
    {
        ThrowIfDestroyed();
        b2World_Step(World, deltaTime, substepCount);

        foreach (var binding in _bodies.Values.ToArray())
        {
            if (!binding.Destroyed)
                binding.Synchronize();
        }
    }
}
