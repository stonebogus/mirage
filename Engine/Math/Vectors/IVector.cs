using System.Numerics;

namespace Mirage.Math.Vectors;

/// <summary>
/// Represents a vector whose components support common vector operations.
/// </summary>
public interface IVector<TVector>
    : IEquatable<TVector>,
        IAdditionOperators<TVector, TVector, TVector>,
        ISubtractionOperators<TVector, TVector, TVector>,
        IUnaryNegationOperators<TVector, TVector>,
        IMultiplyOperators<TVector, float, TVector>,
        IDivisionOperators<TVector, float, TVector>,
        IEqualityOperators<TVector, TVector, bool>
    where TVector : IVector<TVector>
{
    /// <summary>
    /// Gets the length of the vector.
    /// </summary>
    float Length { get; }

    /// <summary>
    /// Gets the squared length of the vector.
    /// </summary>
    float LengthSquared { get; }

    /// <summary>
    /// Gets a normalized copy of the vector.
    /// </summary>
    TVector Normalized { get; }

    /// <summary>
    /// Clamps the components of a vector between minimum and maximum values.
    /// </summary>
    static abstract TVector Clamp(TVector value, TVector minimum, TVector maximum);

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    static abstract float Distance(TVector left, TVector right);

    /// <summary>
    /// Calculates the squared distance between two vectors.
    /// </summary>
    static abstract float DistanceSquared(TVector left, TVector right);

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    static abstract float Dot(TVector left, TVector right);

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    static abstract TVector Lerp(TVector start, TVector end, float amount);

    /// <summary>
    /// Returns a vector containing the largest components of the two vectors.
    /// </summary>
    static abstract TVector Max(TVector left, TVector right);

    /// <summary>
    /// Returns a vector containing the smallest components of the two vectors.
    /// </summary>
    static abstract TVector Min(TVector left, TVector right);
}
