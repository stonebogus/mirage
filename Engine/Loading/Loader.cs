using Mirage.Common;
using Mirage.Common.Collections;

namespace Mirage.Loading;

/// <summary>
/// Describes a resource loading operation.
/// </summary>
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

    /// <summary>
    /// Gets the registered resource decoders.
    /// </summary>
    public readonly ReactiveSet<Decoder> Decoders;

    /// <summary>
    /// Gets the absolute root directory used to locate resources.
    /// </summary>
    public readonly string Root;

    /// <summary>
    /// Initializes a new resource loader.
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
        Decoders = [.. decoders ?? []];
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedDecoders = Compose().ToArray();

        foreach (var decoder in composedDecoders)
        {
            ArgumentNullException.ThrowIfNull(decoder);
            Decoders.Add(decoder);
        }

        _composed = true;
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
    /// Composes the decoders managed by this loader.
    /// </summary>
    /// <returns>An enumerable sequence of decoders to register.</returns>
    /// <remarks>
    /// Composition occurs once when the loader starts or first loads a resource.
    /// Constructor-supplied decoders are registered before composed decoders.
    /// </remarks>
    protected virtual IEnumerable<Decoder> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
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
