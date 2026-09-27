using System.Numerics;
using Mirage.Common;
using Mirage.Common.Events;
using Mirage.Handling;
using Mirage.Handling.Devices;
using Mirage.Handling.Devices.Keyboard;
using Mirage.Handling.Devices.Mouse;
using Mirage.Handling.Devices.Mouse.Events;
using Mirage.Noding;
using Mirage.Physics.Interfaces;
using Mirage.Physics.Resources;
using Mirage.Rendering;
using Mirage.Scheduling;
using Mirage.Scheduling.Channels;
using Mirage.Simulating;
using Mirage.Windowing;

namespace Mirage.Tests;

public class CollectionMigrationTests
{
    private sealed class TestGame(params Module[] modules) : Game(modules);

    private sealed class Device(string identifier) : InputDevice(identifier);

    private sealed class ComposedKeyboard(KeyboardEvent initial, KeyboardEvent[] composed)
        : Keyboard(initial)
    {
        public int Compositions;
        public int Configurations;
        public string[] ConfiguredIdentifiers = [];

        protected override IEnumerable<InputEvent<KeyboardEventPayload, KeyboardKey>> Compose()
        {
            Compositions++;
            return composed;
        }

        protected override void Configure()
        {
            Configurations++;
            ConfiguredIdentifiers = Events.Select(item => item.Identifier).ToArray();
        }
    }

    [Fact]
    public void CompositionRunsOnceBeforeConfigurationAndPreservesOrder()
    {
        var first = new KeyboardEvent("first", KeyboardKey.A);
        var second = new KeyboardEvent("second", KeyboardKey.B);
        var keyboard = new ComposedKeyboard(first, [second]);
        var window = new Window();
        var context = new InputContext(window, []);
        keyboard.Process(context);
        keyboard.Process(context);
        Assert.Equal(1, keyboard.Compositions);
        Assert.Equal(1, keyboard.Configurations);
        Assert.Equal(["first", "second"], keyboard.ConfiguredIdentifiers);
        Assert.Same(second, keyboard.Events["second"]);
        keyboard.Destroy();
        Assert.True(first.Destroyed);
        Assert.True(second.Destroyed);
        window.Destroy();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplicateCompositionAddsNothingAndCannotBeRetried(bool existingConflict)
    {
        var first = new KeyboardEvent("first", KeyboardKey.A);
        var keyboard = new ComposedKeyboard(
            first,
            [
                new("second", KeyboardKey.B),
                new(existingConflict ? "first" : "second", KeyboardKey.C),
            ]
        );
        var window = new Window();
        var context = new InputContext(window, []);
        var additions = 0;
        keyboard.Events.OnAdd.Connect(_ => additions++);
        Assert.Throws<InvalidOperationException>(() => keyboard.Process(context));
        Assert.Same(first, Assert.Single(keyboard.Events));
        Assert.Equal(0, additions);
        Assert.Equal(0, keyboard.Configurations);
        Assert.Throws<InvalidOperationException>(() => keyboard.Process(context));
        Assert.Equal(1, keyboard.Compositions);
        keyboard.Destroy();
        window.Destroy();
    }

    [Fact]
    public void OwnersDestroyItemsButBorrowersDoNot()
    {
        var window = new Window();
        var device = new Device("device");
        var handler = new InputHandler(0, window, [device]);
        Assert.Same(device, handler.Devices["device"]);
        handler.Destroy();
        Assert.True(device.Destroyed);
        Assert.False(window.Destroyed);

        var layer = new DrawLayer("layer");
        var renderer = new Renderer(window, layers: [layer]);
        renderer.Destroy();
        Assert.True(layer.Destroyed);
        Assert.False(window.Destroyed);

        var channel = new UpdateChannel("channel");
        var scheduler = new Scheduler(channels: [channel]);
        scheduler.Destroy();
        Assert.False(channel.Destroyed);
        channel.Destroy();

        var space = new SimulationSpace("space");
        var simulator = new Simulator([space]);
        simulator.Destroy();
        Assert.False(space.Destroyed);
        space.Destroy();

        var action = new MouseButtonEvent("click", MouseButton.Left);
        var mouse = new Mouse(action);
        Assert.Same(action, mouse.Events["click"]);
        mouse.Destroy();
        Assert.True(action.Destroyed);
        window.Destroy();
    }

    private sealed class ComposedHandler(Window window, Device initial, Device[] composed)
        : InputHandler(0, window, [initial])
    {
        public int Configurations;

        protected override IEnumerable<InputDevice> Compose() => composed;

        protected override void Configure()
        {
            Configurations++;
            Assert.Equal(["initial", "composed"], Devices.Select(device => device.Identifier));
            throw new ApplicationException("Configuration failure");
        }
    }

    [Fact]
    public void FailedConfigurationRetainsComposedItemsAndIsNotRepeated()
    {
        var window = new Window();
        var initial = new Device("initial");
        var composed = new Device("composed");
        var handler = new ComposedHandler(window, initial, [composed]);
        var game = new TestGame(handler);
        Assert.Throws<ApplicationException>(() => game.Start());
        Assert.Same(composed, handler.Devices["composed"]);
        Assert.Throws<InvalidOperationException>(() => game.Start());
        Assert.Equal(1, handler.Configurations);
        game.Destroy();
        Assert.True(initial.Destroyed);
        Assert.True(composed.Destroyed);
        Assert.False(window.Destroyed);
        window.Destroy();
    }

    private sealed class DuplicateScheduler(UpdateChannel initial, UpdateChannel extra)
        : Scheduler(channels: [initial])
    {
        protected override IEnumerable<UpdateChannel> Compose() => [extra, initial];
    }

    private sealed class DuplicateSimulator(SimulationSpace initial, SimulationSpace extra)
        : Simulator([initial])
    {
        protected override IEnumerable<SimulationSpace> Compose() => [extra, initial];
    }

    private sealed class DuplicateRenderer(Window window, DrawLayer initial, DrawLayer extra)
        : Renderer(window, layers: [initial])
    {
        protected override IEnumerable<DrawLayer> Compose() => [extra, initial];
    }

    [Fact]
    public void ModuleCompositionRejectsDuplicatesBeforeRegisteringAnyItems()
    {
        var window = new Window();
        var channel = new UpdateChannel("initial");
        var extraChannel = new UpdateChannel("extra");
        var scheduler = new DuplicateScheduler(channel, extraChannel);
        var space = new SimulationSpace("initial");
        var extraSpace = new SimulationSpace("extra");
        var simulator = new DuplicateSimulator(space, extraSpace);
        var layer = new DrawLayer("initial");
        var extraLayer = new DrawLayer("extra");
        var renderer = new DuplicateRenderer(window, layer, extraLayer);
        var additions = 0;
        scheduler.Channels.OnAdd.Connect(_ => additions++);
        simulator.Spaces.OnAdd.Connect(_ => additions++);
        renderer.Layers.OnAdd.Connect(_ => additions++);
        foreach (var module in new Module[] { scheduler, simulator, renderer })
        {
            var game = new TestGame(module);
            Assert.Throws<InvalidOperationException>(() => game.Start());
            Assert.Throws<InvalidOperationException>(() => game.Start());
            Assert.Equal(0, additions);
            if (module == scheduler)
                Assert.Same(channel, Assert.Single(scheduler.Channels));
            else if (module == simulator)
                Assert.Same(space, Assert.Single(simulator.Spaces));
            else
                Assert.Same(layer, Assert.Single(renderer.Layers));
            game.Destroy();
        }
        Assert.True(layer.Destroyed);
        Assert.False(extraLayer.Destroyed);
        extraLayer.Destroy();
        channel.Destroy();
        extraChannel.Destroy();
        space.Destroy();
        extraSpace.Destroy();
        window.Destroy();
    }

    private sealed class Channel(string id, UpdateChannelPriority priority, Action update)
        : UpdateChannel(id, priority: priority)
    {
        public int Configurations;

        protected override void Configure() => Configurations++;

        protected override void OnUpdate(UpdateContext context) => update();
    }

    [Fact]
    public void SchedulerPreparesAddedChannelsAndRunsInPriorityOrder()
    {
        List<string> order = [];
        var high = new Channel("high", UpdateChannelPriority.High, () => order.Add("high"));
        var scheduler = new Scheduler(targetFramerate: 0, channels: [high]);
        var game = new TestGame(scheduler);
        var low = new Channel(
            "low",
            UpdateChannelPriority.Low,
            () =>
            {
                order.Add("low");
                game.Stop();
            }
        );
        game.Start();
        Assert.Equal(1, high.Configurations);
        scheduler.Channels.Add(low);
        Assert.Equal(1, low.Configurations);
        scheduler.Run();
        Assert.Equal(["high", "low"], order);
        game.Start();
        Assert.Equal(1, high.Configurations);
        Assert.Equal(1, low.Configurations);
        game.Stop();
        game.Destroy();
        high.Destroy();
        low.Destroy();
    }

    private sealed class Body : ISimulatable
    {
        private Vector2 _position;
        public Store<PhysicsBody> BodyResource { get; } = new(new PhysicsBody());
        Store<PhysicsBody> ISimulatable.Body => BodyResource;
        public Action? OnSynchronize;
        public int Synchronizations;
        public Vector2 GlobalPosition
        {
            get => _position;
            set
            {
                _position = value;
                Synchronizations++;
                OnSynchronize?.Invoke();
            }
        }
        public float GlobalRotation { get; set; }
    }

    [Fact]
    public void SimulatorDefersSpaceChangesUntilTheUpdateCompletes()
    {
        var firstBody = new Body();
        var secondBody = new Body();
        var first = new SimulationSpace("first", [firstBody]);
        var second = new SimulationSpace("second", [secondBody]);
        var simulator = new Simulator([first]);
        var game = new TestGame(simulator);
        game.Start();
        firstBody.OnSynchronize = () =>
        {
            simulator.Spaces.Remove(first);
            simulator.Spaces.Add(second);
        };
        simulator.Update(new UpdateContext(1d / 60, 0));
        Assert.Equal(1, firstBody.Synchronizations);
        Assert.Equal(0, secondBody.Synchronizations);
        simulator.Update(new UpdateContext(1d / 60, 0));
        Assert.Equal(1, firstBody.Synchronizations);
        Assert.Equal(1, secondBody.Synchronizations);
        simulator.Spaces.Remove("second");
        simulator.Update(new UpdateContext(1d / 60, 0));
        Assert.Equal(1, secondBody.Synchronizations);
        game.Stop();
        game.Destroy();
        Assert.False(first.Destroyed);
        Assert.False(second.Destroyed);
        first.Destroy();
        second.Destroy();
        firstBody.BodyResource.Get().Destroy();
        firstBody.BodyResource.Destroy();
        secondBody.BodyResource.Get().Destroy();
        secondBody.BodyResource.Destroy();
    }

    [Fact]
    public void RootAliasesRemainIndependentOfNodeIdentifiersAndNames()
    {
        var initial = new Node("initial");
        var other = new Node("other");
        var manager = new NodeManager(initial);
        manager.Roots.Add("alias", other);
        other.Name.Set("renamed");
        Assert.Same(other, manager.Roots["alias"]);
        Assert.False(manager.Roots.ContainsKey(other.Identifier.ToString()));
        manager.Switch("alias");
        Assert.Same(other, manager.ActiveRoot.Get());
        Assert.Throws<InvalidOperationException>(() => manager.Roots.Remove("alias"));
        Assert.Same(other, manager.Roots["alias"]);
        manager.Destroy();
    }
}
