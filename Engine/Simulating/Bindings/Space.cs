using Box2D.NET;
using Mirage.Physics.Interfaces;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Mirage.Simulating.Bindings;

internal class SpaceBinding
{
    private readonly Dictionary<ISimulatable, BodyBinding> _bodies = [];

    public SpaceBinding(SimulationSpace space)
    {
        Space = space;

        var gravity = space.Gravity.Get();

        var definition = b2DefaultWorldDef();
        definition.gravity = new B2Vec2(gravity.X, gravity.Y);

        World = b2CreateWorld(definition);

        foreach (var simulatable in space.Simulatables)
            Add(simulatable);
    }

    public SimulationSpace Space { get; }

    public B2WorldId World { get; }

    public void Add(ISimulatable simulatable)
    {
        if (_bodies.ContainsKey(simulatable))
            return;

        _bodies.Add(simulatable, new BodyBinding(World, simulatable));
    }

    public void Destroy()
    {
        _bodies.Clear();

        b2DestroyWorld(World);
    }

    public void Remove(ISimulatable simulatable)
    {
        if (!_bodies.Remove(simulatable, out var binding))
            return;

        binding.Destroy();
    }

    public void Step(float deltaTime, int substepCount)
    {
        b2World_Step(World, deltaTime, substepCount);

        foreach (var binding in _bodies.Values)
            binding.Synchronize();
    }
}
