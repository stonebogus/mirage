using Mirage.Common;
using Mirage.Common.Lifecycle;
using Mirage.Handling;
using Mirage.Handling.Devices.Keyboard;
using Mirage.Handling.Devices.Mouse;
using Mirage.Loading;
using Mirage.Noding;
using Mirage.Rendering;
using Mirage.Scheduling;
using Mirage.Scheduling.Channels;
using Mirage.Scheduling.Interfaces;
using Mirage.Simulating;
using Mirage.Windowing;

internal static class LifecycleTests
{
    public static int Run()
    {
        GameOrderingAndRollback();
        LazyLoader();
        ChannelPreparationAndPacing();
        NodeLoadingFailure();
        RegistrationFailure();
        ConstructorValidation();
        RuntimeGuards();
        return 7;
    }

    private static void ConstructorValidation()
    {
        var window = new Window();
        try
        {
            Check.Throws(() => new TestGame([null!]));
            Check.Throws(() => new Scheduler(channels: [null!]));
            Check.Throws(() => new UpdateChannel("null", entries: [null!]));
            Check.Throws(() => new DrawLayer("null", entries: [null!]));
            Check.Throws(() => new Renderer(window, layers: [null!]));
            Check.Throws(() => new WindowManager([null!]));
            Check.Throws(() => new InputHandler(0, window, [null!]));
            Check.Throws(() => new Keyboard([null!]));
            Check.Throws(() => new Mouse([null!]));
            Check.Throws(() => new Loader(".", [null!]));
            Check.Throws(() => new Node("null", new NodeOptions { Subnodes = [null!] }));
            Check.Throws(() => new Simulator([null!]));
            Check.Throws(() => new SimulationSpace("null", [null!]));
        }
        finally
        {
            window.Destroy();
        }
    }

    private static void RuntimeGuards()
    {
        var window = new Window();
        var manager = new WindowManager();
        var handler = new InputHandler(0, window);
        Check.Throws(() => manager.Update(new UpdateContext(1, 0)));
        Check.Throws(() => handler.Update(new UpdateContext(1, 0)));
        manager.Destroy();
        handler.Destroy();
        Check.That(!window.Destroyed, "Input handler must not own its window.");
        window.Destroy();
    }

    private static void GameOrderingAndRollback()
    {
        var game = new OrderingGame();
        Check.Throws(game.Start);
        Check.That(game.State.Get() == GameState.Idle, "Failed startup must restore idle state.");
        Check.That(
            game.Dependency.State.Get() == ModuleState.Idle,
            "Started modules must be rolled back."
        );
        Check.That(
            game.Trace.SequenceEqual([
                "compose",
                "configure",
                "dependency",
                "dependent",
                "stop dependency",
            ]),
            "Dependency resolution and rollback order."
        );
        game.Start();
        Check.That(
            game.Trace.TakeLast(3).SequenceEqual(["dependency", "dependent", "game start"]),
            "Game startup must follow module startup."
        );
        Check.That(
            game.Trace.Count(entry => entry == "configure") == 1,
            "Runtime startup failure must not repeat configuration."
        );
        Check.Throws(game.Destroy);
        Check.That(
            !game.Dependency.Destroyed,
            "Running game destruction must not destroy modules."
        );
        game.Stop();
        game.Destroy();
    }

    private static void LazyLoader()
    {
        var path = Path.GetTempFileName();
        var decoder = new TestDecoder();
        var state = new ProbeState<Decoder>(decoder, new OtherDecoder(), Failure.None);
        var loader = new ProbeLoader(state);
        try
        {
            // The second decoder deliberately does not match this resource type.
            var resource = loader.Load<TestResource>(path);
            Check.That(
                state.Trace.SequenceEqual(["compose", "configure"]),
                "Loading must lazily configure before decoding."
            );
            Check.That(
                ReferenceEquals(resource, loader.Load<TestResource>(path)),
                "Resource cache must remain intact."
            );
            var game = new TestGame([loader]);
            game.Start();
            game.Stop();
            Check.That(
                state.Compositions == 1 && state.Configurations == 1,
                "Starting a used loader must not configure again."
            );
            game.Destroy();
            Check.That(resource.Destroyed, "Loader owns its loaded resources.");
            Check.That(!decoder.Destroyed, "Loader borrows its decoders.");
        }
        finally
        {
            File.Delete(path);
            if (!loader.Destroyed)
                loader.Destroy();
            decoder.Destroy();
            state.Additional.Destroy();
        }
    }

    private static void ChannelPreparationAndPacing()
    {
        var state = new ProbeState<IUpdatable>(new Entry(), new Entry(), Failure.None);
        var channel = new ProbeUpdateChannel(state);
        var scheduler = new Scheduler(channels: [channel]);
        var game = new TestGame([scheduler]);
        game.Start();
        Check.That(
            state.Trace.SequenceEqual(["compose", "configure"]),
            "Scheduler prepares channels before first tick."
        );
        channel.Update(1);
        game.Stop();
        game.Start();
        game.Stop();
        Check.That(state.Configurations == 1, "Scheduler restart must not reconfigure channels.");
        game.Destroy();
        Check.That(!channel.Destroyed, "Scheduler borrows its channels.");
        channel.Destroy();
        Check.Throws(() => channel.Update(1));
        ((IDestroyable)state.Initial).Destroy();
        ((IDestroyable)state.Additional).Destroy();

        var paced = new PacedChannel();
        paced.Update(0.25);
        Check.That(
            paced.Configurations == 1 && paced.Updates == 0,
            "Configuration precedes rate limiting."
        );
        paced.Update(0.25);
        Check.That(paced.Updates == 1, "Channel pacing must be preserved.");
        paced.Destroy();
    }

    private static void NodeLoadingFailure()
    {
        var node = new FailingNode();
        Check.Throws(node.Load);
        Check.That(!node.Loaded, "A failing OnLoad must restore the unloaded state.");
        node.Load();
        node.Unload();
        Check.That(node.Configurations == 1, "OnLoad failure must not repeat configuration.");
        node.Destroy();
    }

    private static void RegistrationFailure()
    {
        var state = new ProbeState<IUpdatable>(new Entry(), new Entry(), Failure.None);
        var channel = new ProbeUpdateChannel(state);
        channel.Entries.OnAdd.Connect(_ => throw new InvalidOperationException("Observer failed."));
        Check.Throws(() => channel.Update(1));
        Check.Throws(() => channel.Update(1));
        Check.That(
            state.Compositions == 1 && state.Configurations == 0,
            "Registration failure must never mark composition complete."
        );
        channel.Destroy();
        ((IDestroyable)state.Initial).Destroy();
        ((IDestroyable)state.Additional).Destroy();
    }

    private sealed class OrderingGame : Game
    {
        public readonly List<string> Trace = [];
        public readonly DependencyModule Dependency;
        private readonly DependentModule _dependent;

        public OrderingGame()
        {
            Dependency = new DependencyModule(Trace);
            _dependent = new DependentModule(Trace);
        }

        protected override IEnumerable<Module> Compose()
        {
            Trace.Add("compose");
            yield return _dependent;
            yield return Dependency;
        }

        protected override void Configure()
        {
            Trace.Add("configure");
            Check.That(
                ReferenceEquals(Require<DependencyModule>(), Dependency),
                "Require must find composed modules during configuration."
            );
            Check.That(
                Dependency.State.Get() == ModuleState.Idle,
                "Configuration must precede module startup."
            );
            Check.Throws(_dependent.ResolveDependency);
        }

        protected override void OnStart() => Trace.Add("game start");
    }

    private sealed class DependencyModule(List<string> trace) : Module("dependency")
    {
        protected override void OnStart() => trace.Add("dependency");

        protected override void OnStop() => trace.Add("stop dependency");
    }

    private sealed class DependentModule(List<string> trace) : Module("dependent", ["dependency"])
    {
        private bool _fail = true;

        public void ResolveDependency() => Require<DependencyModule>("dependency");

        protected override void OnStart()
        {
            trace.Add("dependent");
            ResolveDependency();
            if (!_fail)
                return;
            _fail = false;
            throw new InvalidOperationException("Runtime startup failed.");
        }
    }

    private sealed class OtherResource : Resource;

    private sealed class OtherDecoder : Decoder<OtherResource>
    {
        public override bool Supports(string extension) => true;

        protected override OtherResource OnDecode(LoadContext context, Stream stream) => new();
    }

    private sealed class PacedChannel() : UpdateChannel("paced", updateRate: 2)
    {
        public int Configurations;
        public int Updates;

        protected override void Configure() => Configurations++;

        protected override void OnUpdate(UpdateContext context) => Updates++;
    }

    private sealed class FailingNode() : Node("failing")
    {
        public int Configurations;
        private bool _fail = true;

        protected override void Configure() => Configurations++;

        protected override void OnLoad()
        {
            if (!_fail)
                return;
            _fail = false;
            throw new InvalidOperationException("OnLoad failed.");
        }
    }
}
