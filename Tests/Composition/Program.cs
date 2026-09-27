using System.Numerics;
using Mirage.Common;
using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Graphics.Interfaces;
using Mirage.Handling;
using Mirage.Handling.Devices;
using Mirage.Handling.Devices.Keyboard;
using Mirage.Handling.Devices.Mouse;
using Mirage.Handling.Devices.Mouse.Events;
using Mirage.Loading;
using Mirage.Noding;
using Mirage.Physics.Interfaces;
using Mirage.Physics.Resources;
using Mirage.Rendering;
using Mirage.Scheduling;
using Mirage.Scheduling.Channels;
using Mirage.Scheduling.Interfaces;
using Mirage.Simulating;
using Mirage.Windowing;

// Override Window's platform default without requiring a display or shell setup.
SDL3.SDL.SetHintWithPriority("SDL_VIDEO_DRIVER", "dummy", SDL3.SDL.HintPriority.Override);
SDL3.SDL.SetHintWithPriority("SDL_RENDER_DRIVER", "software", SDL3.SDL.HintPriority.Override);

var passed = 0;
Run<Module>(
    "Game",
    id => new TestModule(id),
    state =>
    {
        var subject = new ProbeGame(state);
        return new Fixture(
            subject.Start,
            subject.Stop,
            () =>
            {
                subject.Destroy();
            },
            () => subject.Modules.Values.Cast<object>().ToArray()
        );
    },
    true
);

Run<UpdateChannel>(
    "Scheduler",
    id => new UpdateChannel(id),
    state =>
    {
        var subject = new ProbeScheduler(state);
        var game = new TestGame([subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
            },
            () => subject.Channels.Values.Cast<object>().ToArray()
        );
    },
    false
);

Run<IUpdatable>(
    "UpdateChannel",
    id => new Entry(),
    state =>
    {
        var subject = new ProbeUpdateChannel(state);
        return new Fixture(
            () => subject.Update(1),
            () => { },
            () =>
            {
                subject.Destroy();
            },
            () => subject.Entries.Cast<object>().ToArray()
        );
    },
    false
);

Run<IDrawable>(
    "DrawLayer",
    id => new Entry(),
    state =>
    {
        var subject = new ProbeDrawLayer(state);
        return new Fixture(
            () => subject.Draw(null!),
            () => { },
            () =>
            {
                subject.Destroy();
            },
            () => subject.Entries.Cast<object>().ToArray()
        );
    },
    false
);

Run<DrawLayer>(
    "Renderer",
    id => new DrawLayer(id),
    state =>
    {
        var window = new Window(new WindowOptions { Visible = false });
        var subject = new ProbeRenderer(state, window);
        var game = new TestGame([new Scheduler(), new WindowManager([window]), subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
            },
            () => subject.Layers.Values.Cast<object>().ToArray()
        );
    },
    true
);

Run<Window>(
    "WindowManager",
    id => new Window(new WindowOptions { Identifier = id, Visible = false }),
    state =>
    {
        var subject = new ProbeWindowManager(state);
        var game = new TestGame([new Scheduler(), subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
            },
            () => subject.Windows.Values.Cast<object>().ToArray()
        );
    },
    true
);

Run<InputDevice>(
    "InputHandler",
    id => new TestDevice(id),
    state =>
    {
        var window = new Window(new WindowOptions { Visible = false });
        var subject = new ProbeInputHandler(state, window);
        var game = new TestGame([subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
                window.Destroy();
            },
            () => subject.Devices.Select(entry => entry.Value).Cast<object>().ToArray()
        );
    },
    true
);

Run<InputEvent<KeyboardEventPayload, KeyboardKey>>(
    "Keyboard",
    id => new KeyboardEvent(id, KeyboardKey.A),
    state =>
    {
        var window = new Window(new WindowOptions { Visible = false });
        var subject = new ProbeKeyboard(state);
        return new Fixture(
            () => subject.Process(new InputContext(window, [])),
            () => { },
            () =>
            {
                subject.Destroy();
                window.Destroy();
            },
            () => subject.Events.Values.Cast<object>().ToArray()
        );
    },
    true
);

Run<MouseButtonEvent>(
    "Mouse",
    id => new MouseButtonEvent(id, MouseButton.Left),
    state =>
    {
        var window = new Window(new WindowOptions { Visible = false });
        var subject = new ProbeMouse(state);
        return new Fixture(
            () => subject.Process(new InputContext(window, [])),
            () => { },
            () =>
            {
                subject.Destroy();
                window.Destroy();
            },
            () => subject.Events.Values.Cast<object>().ToArray()
        );
    },
    true
);

Run<Decoder>(
    "Loader",
    id => new TestDecoder(),
    state =>
    {
        var subject = new ProbeLoader(state);
        var game = new TestGame([subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
            },
            () => subject.Decoders.Cast<object>().ToArray()
        );
    },
    false
);

Run<Node>(
    "Node",
    id => new Node(id),
    state =>
    {
        var subject = new ProbeNode(state);
        return new Fixture(
            subject.Load,
            subject.Unload,
            () =>
            {
                subject.Destroy();
            },
            () => subject.Subnodes.Cast<object>().ToArray()
        );
    },
    true
);

Run<SimulationSpace>(
    "Simulator",
    id => new SimulationSpace(id),
    state =>
    {
        var subject = new ProbeSimulator(state);
        var game = new TestGame([subject]);
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
            },
            () => subject.Spaces.Values.Cast<object>().ToArray()
        );
    },
    false
);

Run<ISimulatable>(
    "SimulationSpace",
    id => new Entry(),
    state =>
    {
        var subject = new ProbeSimulationSpace(state);
        var game = new TestGame([new Simulator([subject])]);
        game.Started = () => state.Trace.Add("use");
        return new Fixture(
            game.Start,
            game.Stop,
            () =>
            {
                game.Destroy();
                subject.Destroy();
            },
            () => subject.Simulatables.Cast<object>().ToArray()
        );
    },
    false
);

passed += LifecycleTests.Run();
Console.WriteLine($"Passed {passed} composition regression cases.");

void Run<T>(string name, Func<string, T> create, Func<ProbeState<T>, Fixture> fixture, bool owns)
    where T : class
{
    foreach (var mode in Enum.GetValues<Failure>())
    {
        if (name == "DrawLayer" && mode is Failure.ExistingDuplicate or Failure.BatchDuplicate)
            continue;
        if (
            mode == Failure.IdentifierCollision
            && name is "UpdateChannel" or "DrawLayer" or "Loader" or "Node" or "SimulationSpace"
        )
            continue;
        var state = new ProbeState<T>(
            create("initial"),
            create(mode == Failure.IdentifierCollision ? "initial" : "composed"),
            mode
        );
        var test = fixture(state);
        state.Reenter = test.Begin;
        Check.That(state.Trace.Count == 0, $"{name}: construction invoked hooks");
        if (mode == Failure.None)
        {
            test.Begin();
            Check.That(
                state.Trace.SequenceEqual(["compose", "configure", "use"]),
                $"{name}: order"
            );
            test.End();
            test.Begin();
            test.End();
            Check.That(
                state.Compositions == 1 && state.Configurations == 1,
                $"{name}: restart repeated hooks"
            );
            Check.That(
                test.Objects().SequenceEqual([state.Initial, state.Additional]),
                $"{name}: registration order"
            );
            state.Fire();
            Check.That(state.Callbacks == 1, $"{name}: duplicate event subscription");
            test.Destroy();
            Check.That(
                ((IDestroyable)state.Initial).Destroyed == owns,
                $"{name}: initial ownership"
            );
            Check.That(
                ((IDestroyable)state.Additional).Destroyed == owns,
                $"{name}: composed ownership"
            );
        }
        else
        {
            Check.Throws(test.Begin);
            Check.Throws(test.Begin);
            Check.That(state.Compositions == 1, $"{name}: composition retried");
            var configuring = mode is Failure.Configuration or Failure.ReentrantConfiguration;
            Check.That(
                state.Configurations == (configuring ? 1 : 0),
                $"{name}: configuration count"
            );
            Check.That(!state.Trace.Contains("use"), $"{name}: runtime after failure");
            Check.That(
                test.Objects().Length == (configuring ? 2 : 1),
                $"{name}: partial registration"
            );
            state.Fire();
            Check.That(
                state.Callbacks == (configuring ? 1 : 0),
                $"{name}: duplicated side effects"
            );
            test.Destroy();
        }
        foreach (var item in new[] { state.Initial, state.Additional }.Cast<IDestroyable>())
            if (!item.Destroyed)
                item.Destroy();
        passed++;
        Console.WriteLine($"PASS {name}: {mode}");
    }
}

internal enum Failure
{
    None,
    Enumeration,
    Null,
    ExistingDuplicate,
    BatchDuplicate,
    IdentifierCollision,
    Configuration,
    ReentrantConfiguration,
}

internal sealed record Fixture(Action Begin, Action End, Action Destroy, Func<object[]> Objects);

internal static class Check
{
    public static void That(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    public static void Throws(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new Exception("Expected lifecycle failure.");
    }
}

internal sealed class ProbeState<T>(T initial, T additional, Failure failure)
    where T : class
{
    public T Initial { get; } = initial;
    public T Additional { get; } = additional;
    public List<string> Trace { get; } = [];
    public int Compositions;
    public int Configurations;
    public int Callbacks;
    public Action? Reenter;
    private event Action? Connected;

    public IEnumerable<T> Compose()
    {
        Compositions++;
        Trace.Add("compose");
        yield return Additional;
        switch (failure)
        {
            case Failure.Enumeration:
                throw new InvalidOperationException("Enumeration failed.");
            case Failure.Null:
                yield return null!;
                break;
            case Failure.ExistingDuplicate:
                yield return Initial;
                break;
            case Failure.BatchDuplicate:
                yield return Additional;
                break;
        }
    }

    public void Configure(IEnumerable<T> values)
    {
        Configurations++;
        Trace.Add("configure");
        Check.That(
            values.SequenceEqual([Initial, Additional]),
            "All objects must be registered before configuration."
        );
        Connected += () => Callbacks++;
        if (failure == Failure.Configuration)
            throw new InvalidOperationException("Configuration failed after connecting an event.");
        if (failure == Failure.ReentrantConfiguration)
        {
            // Reentry must not proceed into runtime while configuration is incomplete.
            Reenter!();
            throw new Exception("Reentrant initialization was allowed.");
        }
    }

    public void Fire() => Connected?.Invoke();
}

internal sealed class TestGame(IEnumerable<Module> modules) : Game(modules)
{
    public Action? Started;

    protected override void OnStart() => Started?.Invoke();
}

internal sealed class TestModule(string identifier) : Module(identifier);

internal sealed class TestDevice(string identifier) : InputDevice(identifier);

internal sealed class TestResource : Resource;

internal sealed class TestDecoder : Decoder<TestResource>
{
    public override bool Supports(string extension) => true;

    protected override TestResource OnDecode(LoadContext context, Stream stream) => new();
}

internal sealed class Entry : Destroyable, IUpdatable, IDrawable, ISimulatable
{
    public Store<PhysicsBody> Body { get; } = new(new PhysicsBody());
    public Vector2 GlobalPosition { get; set; }
    public float GlobalRotation { get; set; }

    public void Update(UpdateContext context) { }

    public void Draw(IDrawContext context) { }

    protected override void OnDestroy()
    {
        Body.Get().Destroy();
        Body.Destroy();
    }
}

internal sealed class ProbeGame(ProbeState<Module> state) : Game([state.Initial])
{
    protected override IEnumerable<Module> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Modules.Values);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeScheduler(ProbeState<UpdateChannel> state)
    : Scheduler(channels: [state.Initial])
{
    protected override IEnumerable<UpdateChannel> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Channels.Values);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeUpdateChannel(ProbeState<IUpdatable> state)
    : UpdateChannel("probe", entries: [state.Initial])
{
    protected override IEnumerable<IUpdatable> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Entries);
    }

    protected override void OnUpdate(UpdateContext context) => state.Trace.Add("use");
}

internal sealed class ProbeDrawLayer(ProbeState<IDrawable> state)
    : DrawLayer("probe", entries: [state.Initial])
{
    protected override IEnumerable<IDrawable> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Entries);
    }

    protected override void OnDraw(RenderContext context) => state.Trace.Add("use");
}

internal sealed class ProbeRenderer(ProbeState<DrawLayer> state, Window window)
    : Renderer(window, layers: [state.Initial])
{
    protected override IEnumerable<DrawLayer> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Layers.Values);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeWindowManager(ProbeState<Window> state) : WindowManager([state.Initial])
{
    protected override IEnumerable<Window> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Windows.Values);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeInputHandler(ProbeState<InputDevice> state, Window window)
    : InputHandler(0, window, [state.Initial])
{
    protected override IEnumerable<InputDevice> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Devices.Select(entry => entry.Value));
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeKeyboard(ProbeState<InputEvent<KeyboardEventPayload, KeyboardKey>> state)
    : Keyboard([state.Initial])
{
    protected override IEnumerable<InputEvent<KeyboardEventPayload, KeyboardKey>> Compose() =>
        state.Compose();

    protected override void Configure()
    {
        state.Configure(Events.Values);
    }

    protected override void OnProcess(InputContext context)
    {
        base.OnProcess(context);
        state.Trace.Add("use");
    }
}

internal sealed class ProbeMouse(ProbeState<MouseButtonEvent> state) : Mouse([state.Initial])
{
    protected override IEnumerable<MouseButtonEvent> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Events.Values);
    }

    protected override void OnProcess(InputContext context)
    {
        base.OnProcess(context);
        state.Trace.Add("use");
    }
}

internal sealed class ProbeLoader(ProbeState<Decoder> state)
    : Loader(Path.GetTempPath(), [state.Initial])
{
    protected override IEnumerable<Decoder> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Decoders);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeNode(ProbeState<Node> state)
    : Node("probe", new NodeOptions { Subnodes = [state.Initial] })
{
    protected override IEnumerable<Node> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Subnodes);
    }

    protected override void OnLoad() => state.Trace.Add("use");
}

internal sealed class ProbeSimulator(ProbeState<SimulationSpace> state) : Simulator([state.Initial])
{
    protected override IEnumerable<SimulationSpace> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Spaces.Values);
    }

    protected override void OnStart()
    {
        base.OnStart();
        state.Trace.Add("use");
    }
}

internal sealed class ProbeSimulationSpace(ProbeState<ISimulatable> state)
    : SimulationSpace("probe", [state.Initial])
{
    protected override IEnumerable<ISimulatable> Compose() => state.Compose();

    protected override void Configure()
    {
        state.Configure(Simulatables);
    }
}
