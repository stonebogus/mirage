using Mirage.Common.Lifecycle;
using Mirage.Graphics;
using Mirage.Graphics.Interfaces;

namespace Mirage.Renderer.Backends;

/// <summary>
/// Represents a platform-specific rendering backend.
/// </summary>
/// <remarks>
/// A backend owns the complete lifetime of a rendering frame. Renderable
/// entries are evaluated and their resources are prepared before the
/// platform-specific frame begins.
/// </remarks>
public abstract class RendererBackend : Destroyable
{
    /// <summary>
    /// Gets whether the backend is currently started.
    /// </summary>
    public bool Started { get; private set; }

    private void EnsureStarted()
    {
        if (!Started)
        {
            throw new InvalidOperationException("The rendering backend is not started.");
        }
    }

    /// <summary>
    /// Begins a platform-specific rendering frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when rendering can continue; otherwise,
    /// <see langword="false"/>. A backend may return
    /// <see langword="false"/> when its target is temporarily unavailable,
    /// such as when a window is minimized.
    /// </returns>
    protected virtual bool OnBeginFrame(RenderContext context)
    {
        return true;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (!Started)
            return;

        OnStop();
        Started = false;
    }

    /// <summary>
    /// Ends and presents a platform-specific rendering frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    protected virtual void OnEndFrame(RenderContext context) { }

    /// <summary>
    /// Prepares the resources referenced by rendering data.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <param name="data">
    /// The rendering data that will be executed during the frame.
    /// </param>
    /// <remarks>
    /// This method runs before <see cref="OnBeginFrame"/>. Backends should
    /// create and upload buffers, textures, shaders and pipelines here
    /// instead of creating them while a render pass is active.
    /// </remarks>
    protected virtual void OnPrepare(RenderContext context, IReadOnlyList<RenderData> data) { }

    /// <summary>
    /// Executes backend-agnostic rendering data.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <param name="data">
    /// The backend-agnostic rendering data to execute.
    /// </param>
    protected abstract void OnRender(RenderContext context, RenderData data);

    /// <summary>
    /// Starts platform-specific rendering resources.
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// Stops platform-specific rendering resources.
    /// </summary>
    protected virtual void OnStop() { }

    /// <summary>
    /// Renders and presents one complete frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the frame.
    /// </param>
    /// <param name="entries">
    /// The renderable entries in execution order.
    /// </param>
    /// <remarks>
    /// Rendering data is collected and prepared before the platform-specific
    /// frame begins. Once a frame begins, its end operation is guaranteed to
    /// run even when command execution throws an exception.
    /// </remarks>
    public void RenderFrame(RenderContext context, IReadOnlyList<IRenderable> entries)
    {
        ThrowIfDestroyed();

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entries);

        EnsureStarted();

        var renderingData = new List<RenderData>(entries.Count);

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var data = entry.Render(context);

            if (data is null)
            {
                throw new InvalidOperationException(
                    $"Renderable '{entry.GetType().Name}' returned null rendering data."
                );
            }

            renderingData.Add(data);
        }

        OnPrepare(context, renderingData);

        if (!OnBeginFrame(context))
            return;

        try
        {
            foreach (var data in renderingData)
                OnRender(context, data);
        }
        finally
        {
            OnEndFrame(context);
        }
    }

    /// <summary>
    /// Starts the rendering backend.
    /// </summary>
    /// <remarks>
    /// Repeated calls have no effect while the backend is already started.
    /// </remarks>
    public void Start()
    {
        ThrowIfDestroyed();

        if (Started)
            return;

        OnStart();
        Started = true;
    }

    /// <summary>
    /// Stops the rendering backend.
    /// </summary>
    /// <remarks>
    /// Repeated calls have no effect while the backend is stopped.
    /// </remarks>
    public void Stop()
    {
        ThrowIfDestroyed();

        if (!Started)
            return;

        OnStop();
        Started = false;
    }
}
