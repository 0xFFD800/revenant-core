using Microsoft.Xna.Framework;
using System;

namespace RevenantCore.Util;

public static class NumericsExtension
{
    extension(Vector3 vec)
    {
        public Vector3 Abs() => new(Math.Abs(vec.X), Math.Abs(vec.Y), Math.Abs(vec.Z));

        public Vector3 Clamp(Vector3 min, Vector3 max) => new(Math.Clamp(vec.X, min.X, max.X), Math.Clamp(vec.Y, min.Y, max.Y), Math.Clamp(vec.Z, min.Z, max.Z));

        public Vector3 Sign() => new(Math.Sign(vec.X), Math.Sign(vec.Y), Math.Sign(vec.Z));
    }

    extension(BoundingBox box)
    {
        public BoundingBox Add(Vector3 vec) => new(box.Min + vec, box.Max + vec);

        public BoundingBox? FindIntersection(BoundingBox b2) =>
            !box.Intersects(b2) ? null : new(VectorMath.Max(box.Min, b2.Min), VectorMath.Min(box.Max, b2.Max));

        public static BoundingBox operator +(BoundingBox b, Vector3 vec) => Add(b, vec);
    }
}

public static class VectorMath
{
    public static Vector3 Max(Vector3 v1, Vector3 v2) => new(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y), Math.Max(v1.Z, v2.Z));
    public static Vector2 Max(Vector2 v1, Vector2 v2) => new(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y));
    public static Vector3 Min(Vector3 v1, Vector3 v2) => new(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y), Math.Min(v1.Z, v2.Z));
}
