using Mirage.Math.Vectors;

namespace Tests.Math;

public class VectorTest
{
    [Fact]
    public void Vector_StoresTwoComponents()
    {
        var vector = new Vector(3, 4);

        Assert.Equal(3, vector.X);
        Assert.Equal(4, vector.Y);
        Assert.Equal(5, vector.Length);
    }

    [Fact]
    public void Vector_SupportsTwoDimensionalOperations()
    {
        var left = new Vector(1, 2);
        var right = new Vector(3, 4);

        Assert.Equal(new Vector(4, 6), left + right);
        Assert.Equal(new Vector(-2, -2), left - right);
        Assert.Equal(11, Vector.Dot(left, right));
        Assert.Equal(-2, Vector.Cross(left, right));
    }
}
