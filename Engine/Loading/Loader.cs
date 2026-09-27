using Mirage.Common;
using Mirage.Common.Collections;

namespace Mirage.Loading;

/// <summary>
/// Initializes a new instance of the <see cref="LoadContext"/> class.
/// </summary>
/// <remarks>
/// Describes a resource loading operation.
/// </remarks>
/// <param name="Identifier">
/// The normalized resource identifier relative to the loader root.
/// </param>
/// <param name="Path">
/// The absolute path of the resource.
/// </param>
public sealed record LoadContext(string Identifier, string Path);

/// <summary>
/// Loads, caches and unloads resources.
/// </summary>
/// <remarks>The loader owns successfully loaded resources and destroys them when it is destroyed.</remarks>
public class Loader : Module
{
    private readonly Dictionary<ResourceKey, Resource> _resources = [];
    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the registered resource decoders.
    /// </summary>
    public readonly ReactiveSet<Decoder> Decoders;

    /// <summary>
    /// Gets the absolute root directory used to locate resources.
    /// </summary>
    public readonly string Root;

    /// <summary>
    /// Initializes a new instance of the <see cref="Loader"/> class.
    /// </summary>
    /// <param name="root">
    /// The directory from which resources are loaded.
    /// </param>
    /// <param name="decoders">
    /// The initial decoders, or <see langword="null"/> for none.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="root"/> is empty or whitespace.
    /// </exception>
    public Loader(string root, IEnumerable<Decoder>? decoders = null)
        : base("Loader")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        Decoders = [];
        foreach (var decoder in decoders ?? [])
        {
            Decoders.Add(decoder);
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<Decoder> objects = [];

        foreach (var decoder in composedObjects)
        {
            if (Decoders.Contains(decoder) || !objects.Add(decoder))
                throw new InvalidOperationException("Duplicate composed object found.");
        }

        foreach (var decoder in composedObjects)
            Decoders.Add(decoder);

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

    private string ResolvePath(string path)
    {
        var absolutePath = Path.GetFullPath(path, Root);

        var relativePath = Path.GetRelativePath(Root, absolutePath);

        if (
            relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath)
        )
        {
            throw new ArgumentException(
                $"Resource path '{path}' is outside the loader root.",
                nameof(path)
            );
        }

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                $"Resource '{relativePath}' was not found.",
                absolutePath
            );
        }

        return absolutePath;
    }

    /// <summary>
    /// Composes the decoders managed by this object.
    /// </summary>
    /// <returns>The decoders to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the loader first starts or first loads a resource.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The loader references decoders without owning or destroying them.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<Decoder> Compose()
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

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var resource in _resources.Values.Where(resource => !resource.Destroyed))
        {
            resource.Destroy();
        }

        _resources.Clear();

        Decoders.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
        EnsureConfigured();
    }

    /// <summary>
    /// Loads or retrieves a cached resource.
    /// </summary>
    /// <typeparam name="TResource">
    /// The type of resource to load.
    /// </typeparam>
    /// <param name="path">
    /// The resource path relative to <see cref="Root"/>.
    /// </param>
    /// <returns>
    /// The loaded resource.
    /// </returns>
    /// <remarks>
    /// Resources are cached by requested type and normalized relative path.
    /// The loader owns the returned resource.
    /// </remarks>
    /// <exception cref="Mirage.Common.Lifecycle.DestroyedObjectException">
    /// Thrown when the loader has been destroyed.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is empty or resolves outside <see cref="Root"/>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the resource file does not exist.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the path has no extension or no decoder supports the requested resource type and extension.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple decoders match or a decoder returns the wrong resource type.
    /// </exception>
    public TResource Load<TResource>(string path)
        where TResource : Resource
    {
        ThrowIfDestroyed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        EnsureComposed();
        EnsureConfigured();

        var absolutePath = ResolvePath(path);

        var identifier = Path.GetRelativePath(Root, absolutePath);

        var key = new ResourceKey(typeof(TResource), identifier);

        if (_resources.TryGetValue(key, out var cached))
        {
            if (cached.Destroyed)
            {
                _resources.Remove(key);
            }
            else
            {
                return (TResource)cached;
            }
        }

        var extension = Path.GetExtension(absolutePath);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new NotSupportedException($"Resource '{identifier}' has no filename extension.");
        }

        var candidates = Decoders
            .Where(decoder =>
                decoder.ResourceType == typeof(TResource) && decoder.Supports(extension)
            )
            .Take(2)
            .ToArray();

        switch (candidates.Length)
        {
            case 0:
                throw new NotSupportedException(
                    $"No decoder is registered for resource type "
                        + $"'{typeof(TResource).Name}' and extension "
                        + $"'{extension}'."
                );

            case > 1:
                throw new InvalidOperationException(
                    $"Multiple decoders are registered for resource type "
                        + $"'{typeof(TResource).Name}' and extension "
                        + $"'{extension}'."
                );
        }

        using var stream = File.OpenRead(absolutePath);

        var context = new LoadContext(identifier, absolutePath);

        var decoded = candidates[0].Decode(context, stream);

        if (decoded is not TResource resource)
        {
            decoded.Destroy();

            throw new InvalidOperationException(
                $"Decoder '{candidates[0].GetType().Name}' returned "
                    + $"'{decoded.GetType().Name}' instead of "
                    + $"'{typeof(TResource).Name}'."
            );
        }

        _resources.Add(key, resource);

        return resource;
    }

    private readonly record struct ResourceKey(Type Type, string Identifier);
}
