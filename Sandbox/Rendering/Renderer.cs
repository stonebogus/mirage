using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Noding;
using Mirage.Rendering;
using Mirage.Rendering.Layers;
using Mirage.Windowing;

namespace Sandbox.Rendering;

internal sealed class MyRenderer : Renderer
{
    public readonly NodeDrawLayer Main;

    public MyRenderer(Window window, NodeManager nodes, ICamera camera)
        : base(window, clearColor: new Color("#ffffff"), camera: camera)
    {
        Main = new NodeDrawLayer(nodes, identifier: "main", priority: DrawLayerPriority.Normal);
    }

    protected override IEnumerable<DrawLayer> Compose()
    {
        yield return Main;
    }
}
