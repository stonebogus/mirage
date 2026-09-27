<p align="center">
  <a href="Assets/Wide/Transparent/wide.png">
    <img src="Assets/Wide/Transparent/wide.png" width="500" alt="Mirage Logo">
  </a>
</p>

<p align="center">
  A modular 2D game engine for C# and .NET.
</p>

---

Mirage is an experimental 2D game engine for C# and .NET, built around small, modular, and composable systems.

It provides the foundations for building complete 2D games while keeping the relationship between the game and the engine explicit. Instead of hiding everything behind a large framework, Mirage is composed from focused systems that can work together without becoming one.

> [!WARNING]
> Mirage is experimental software and is under active development. APIs, behavior, architecture, and project structure may change as the engine evolves.

## Features

- Hierarchical **node system** for structuring games and scenes
- 2D **spatial transforms** and cameras
- Layer-based **rendering**
- Primitives, textures, and arbitrary meshes
- Event-driven **keyboard and mouse input**
- Resource loading and decoding
- Ordered update scheduling
- Window and application lifecycle management
- Reactive state and event primitives
- Modular game composition

## A Small Example

A Mirage game is composed from modules.

```csharp
internal sealed class MyGame : Game
{
    public readonly Window MainWindow;
    public readonly WindowManager WindowManager;
    public readonly NodeManager NodeManager;
    public readonly Renderer Renderer;
    public readonly Scheduler Scheduler;

    public MyGame()
    {
        MainWindow = new Window(
            new WindowOptions
            {
                Identifier = "Primary",
                Title = "My Game",
                Size = new Vector2(1280, 720),
                Resizable = true,
                VSync = true,
            }
        );

        WindowManager = new WindowManager(
            windows: [MainWindow]
        );

        var camera = new Camera();

        NodeManager = new NodeManager(
            new Node(
                "Root",
                new NodeOptions
                {
                    Subnodes = [camera],
                }
            )
        );

        Renderer = new Renderer(
            window: MainWindow,
            camera: camera
        );

        Scheduler = new Scheduler();
    }

    protected override IEnumerable<Module> Compose()
    {
        yield return WindowManager;
        yield return NodeManager;
        yield return Renderer;
        yield return Scheduler;
    }
}
```

The engine takes care of the lifecycle of the composed modules while the game decides how those systems are connected.

Composition follows `Construct → Compose → Configure → Start/use`. Constructors establish
required dependencies and initial values. Override `Compose()` to declare managed objects,
and `Configure()` to connect events or configure relationships after all composed objects
have been registered. Constructor-provided objects are registered first, followed by composed
objects in enumeration order.

`Game.Configure()` runs before module dependency resolution, injection, and startup;
`Require<TModule>()` can access composed modules there. `Game.OnStart()` remains runtime
startup behavior after all modules have started. Each child system configures at its own
startup or first-use boundary; configuring a parent does not eagerly initialize its children.

Composition and configuration are each attempted at most once. Stop/start and load/unload
cycles reuse the configured structure. If either structural phase throws, later lifecycle
calls reject initialization instead of repeating partial event connections or other side
effects. Destroy the failed object and create a new instance. Objects registered before a
failure retain the containing type's normal ownership rules; objects rejected before
registration remain the caller's responsibility.

The configuration phase applies to games, schedulers, update channels, renderers, draw layers,
window managers, input handlers, keyboards, mice, loaders, nodes, simulators, and simulation
spaces. Lazy systems stay lazy: draw layers configure on their first draw, input devices on
first processing, and loaders on startup or first load. Update channels configure when
prepared by their scheduler or before a direct first update; simulation spaces configure when
prepared by their simulator. Ownership is documented on each composition hook.


## Nodes

Nodes are the structural foundation of Mirage.

They form hierarchical trees that can represent objects, scenes, cameras, or any other part of a game.

```csharp
var root = new Node(
    "World",
    new NodeOptions
    {
        Subnodes =
        [
            new Node("Player"),
            new Node("Enemies"),
            new Node("Environment"),
        ],
    }
);
```

Specialized nodes can extend this structure with additional behavior. Spatial nodes introduce position, rotation, scale, and hierarchical transforms for objects that exist in the 2D world.

```text
World
├── Camera
├── Player
├── Enemies
│   ├── Enemy
│   └── Enemy
└── Environment
```

The node tree provides structure without requiring every engine system to become part of the node hierarchy.

## Rendering

Mirage provides a 2D rendering system built around cameras and ordered drawing layers.

The graphics system supports common primitives alongside textures and arbitrary meshes, allowing simple shapes and custom geometry to participate in the same rendering pipeline.

```csharp
context.DrawLine(
    new Vector2(-100, 0),
    new Vector2(100, 0),
    Color.White,
    thickness: 8f
);

context.FillCircle(
    Vector2.Zero,
    48f,
    Color.White
);
```

Rendering remains separate from the node system. Layers decide what should be rendered, while cameras determine how the world is viewed.

## Input

Input is represented through devices and events.

Keyboard and mouse input can be connected directly to game behavior without requiring input polling to become part of the rest of the engine architecture.

```csharp
var jump = new KeyboardEvent(
    "Jump",
    KeyboardKey.Space
);

jump.Connect(payload =>
{
    if (payload.Down)
        Jump();
});
```

Multiple devices can be handled together through an input handler associated with a window.

## Scheduling

Mirage uses update channels to coordinate continuously running systems.

Channels can contain game logic, node updates, rendering, or other systems that need to participate in the main loop.

```text
Scheduler
│
├── Main
│   └── Game / Node updates
│
└── Render
    └── Renderer
```

Channels are ordered independently, keeping scheduling separate from the systems being scheduled.

## Loading

Game resources are loaded through a common resource loader.

Decoders transform files into resources understood by Mirage.

```csharp
var loader = new Loader(
    root: "Assets",
    decoders: [new ImageDecoder()]
);

var image = loader.Load<Image>("player.png");
var texture = new Texture(image);
```

This keeps file access and resource decoding separate from graphics and game logic.

## Architecture

Mirage is divided into focused systems with clearly defined responsibilities.

```text
                         Game
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
     Noding           Scheduling        Windowing
        │                 │                 │
     Spatial           Updates           Handling
        │
    Rendering
        │
     Graphics

              Loading ─── Resources
```

At the center is `Game`, which owns the application lifecycle and composes the modules that make up a game.

The major concepts are intentionally independent:

- **Noding** organizes hierarchical game state.
- **Spatial** brings nodes into the 2D world.
- **Graphics** provides graphics resources and primitives.
- **Rendering** turns game state into an image.
- **Handling** represents user input.
- **Scheduling** coordinates updates.
- **Loading** creates resources from external assets.
- **Windowing** manages native game windows.
- **Common** provides shared lifecycle, events, collections, resources, and other foundational primitives.

The goal is not to make every system interchangeable or abstract every implementation detail. Mirage instead tries to keep each responsibility small, understandable, and composable.

## Philosophy

Mirage is intentionally focused.

It is a **2D engine**, rather than a generalized engine where every concept must accommodate both 2D and 3D.

It prefers existing .NET types when they already represent a concept well, and introduces engine-specific abstractions when they provide meaningful behavior.

It favors composition over large inheritance hierarchies, explicit ownership over hidden global state, and small systems over monolithic managers.

Most importantly, Mirage is being built alongside real games. Its architecture is expected to evolve as those games expose what the engine actually needs.

## Development

Active development takes place on the `develop` branch.

To work with the latest development version:

```bash
git checkout develop
```

The default branch may not contain the latest changes.

> [!NOTE]
> Mirage is not considered stable yet. Documentation may occasionally lag behind development, and examples written against the current API may require changes in future versions.

## License

Mirage is licensed under the [MIT License](LICENSE).
## Composition regression tests

The repository includes a dependency-free executable regression suite. Run it with:

```sh
dotnet build Mirage.slnx -m:1
dotnet test Tests/Composition/Mirage.Composition.Tests.csproj -m:1
```

The suite uses SDL's dummy video driver and software renderer, so no display is needed.
It can also be run directly with `dotnet run --project Tests/Composition --no-build`.
