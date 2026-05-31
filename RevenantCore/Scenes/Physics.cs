using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// An internal class which represents a single physical object during a single run of the Tick loop.
/// </summary>
/// <param name="collideable">The collideable object which is having its physics calculated.</param>
/// <param name="millis">The number of milliseconds elapsed since the last run of the Tick loop.</param>
internal sealed class PhysicalObject(ICollideable collideable, double millis)
{
    private double remMillis = millis;
    private float friction = 1;

    private ICollideable Collideable => collideable;

    private Vector3 GetNextPos(double millis)
    {
        Vector3 totalVel = collideable.Velocity * (float)millis;
        Vector3 totalAcc = collideable.Acceleration * (float)(millis * millis);
        return collideable.Position + totalVel + (0.5F * totalAcc);
    }
    
    private void HandleReflection(Vector3 v, Vector3 a, float? massRatio, float? absorption, BoundingBox intersection)
    {
        Vector3 b = intersection.Max - intersection.Min;
        Vector3 normal;
        if (b.X <= b.Y && b.X <= b.Z)
            normal = new(-1, 1, 1);
        else if (b.Y <= b.X && b.Y <= b.Z)
            normal = new(1, -1, 1);
        else
            normal = new(1, 1, -1);
        if (massRatio.HasValue)
        {
            collideable.Velocity = v * massRatio.Value;
            collideable.Acceleration = a * massRatio.Value;
        }
        else
        {
            collideable.Velocity *= normal;
            collideable.Acceleration *= normal;
        }
        Vector3 absp = -(normal - Vector3.One);
        absp.Normalize();
        if (absorption.HasValue)
        {
            absp *= 1 - absorption.Value;
            absp = Vector3.One - absp;
            collideable.Velocity /= absp;
            collideable.Acceleration /= absp;
        }
        else
        {
            absp = Vector3.One - absp;
            collideable.Velocity *= absp;
            collideable.Acceleration *= absp;
        }
    }

    private void HandleSlide(BoundingBox intersection, PhysicalObject other)
    {
        // Determine the direction of the occlusion
        Vector3 b = intersection.Max - intersection.Min;
        float shift;
        Vector3 dir;
        if (b.X <= b.Y && b.X <= b.Z)
        {
            shift = b.X;
            dir = Vector3.UnitX;
        }
        else if (b.Y <= b.X && b.Y <= b.Z)
        {
            shift = b.Y;
            dir = Vector3.UnitY;
        }
        else
        {
            shift = b.Z;
            dir = Vector3.UnitZ;
        }

        // Eliminate occlusion by shifting according to mass ratio
        float? m1 = collideable.Material.Mass;
        float? m2 = other.Collideable.Material.Mass;
        Vector3 sign = -(dir * (other.Collideable.Position - collideable.Position)).Sign();
        if (sign == Vector3.Zero)
            sign = Vector3.One;
        if (m1.HasValue && !m2.HasValue)
            collideable.Position += shift * dir * -sign;
        else if (m2.HasValue && !m1.HasValue)
            other.Collideable.Position += shift * dir * sign;
        else if (m1.HasValue && m2.HasValue)
        {
            collideable.Position += shift * dir * (m1.Value / (m1.Value + m2.Value)) * -sign;
            other.Collideable.Position += shift * dir * (m2.Value / (m1.Value + m2.Value)) * sign;
        }

        // Eliminate velocity and acceleration in the direction in which the occlusion had been
        dir = Vector3.One - dir;
        collideable.Velocity *= dir;
        collideable.Acceleration *= dir;
        other.Collideable.Velocity *= dir;
        other.Collideable.Acceleration *= dir;
    }

    private void HandleCollide(BoundingBox intersection, PhysicalObject other, double millis1, double millis2)
    {   
        collideable.Position = GetNextPos(millis1);
        other.Collideable.Position = other.GetNextPos(millis2);

        float? m1 = collideable.Material.Mass;
        float? m2 = other.Collideable.Material.Mass;
        float? massRatio1 = m1.HasValue && m2.HasValue ? m2.Value / m1.Value : m1.HasValue ? null : 0;
        float? massRatio2 = m1.HasValue && m2.HasValue ? m1.Value / m2.Value : m2.HasValue ? null : 0;
        float? absorption = collideable.Material.MaterialAbsorption * other.Collideable.Material.MaterialAbsorption;
        Vector3 v1 = collideable.Velocity;
        Vector3 a1 = collideable.Acceleration;
        HandleReflection(other.Collideable.Velocity, other.Collideable.Acceleration, massRatio1, absorption, intersection);
        other.HandleReflection(v1, a1, massRatio2, absorption, intersection);
    }

    private Vector3 ApplyFrictionTo(Vector3 v) =>
        v.Sign() * (v.Abs() - (v.Abs() * (1 - friction)).Clamp(Vector3.Zero, v.Abs()));

    /// <summary>
    /// Applies a certain value of gravitational acceleration to this object, dependent on its physical parameters.
    /// </summary>
    /// <param name="gravity">The gravitational acceleration of the enclosing scene.</param>
    internal void ApplyGravity(float gravity)
    {
        // Only collideables with a defined mass should be affected by gravity.
        if (collideable.Material.Mass.HasValue)
            collideable.Acceleration -= Vector3.UnitY * gravity;
    }

    /// <summary>
    /// Test for collisions with the provided object and apply any effects to both.
    /// </summary>
    /// <param name="other">The object to test for collisions with.</param>
    internal void ApplyCollisions(PhysicalObject other)
    {
        BoundingBox? currInt = collideable.CollisionBox.FindIntersection(other.Collideable.CollisionBox);
        BoundingBox? futureInt = null;
        if (currInt.HasValue)
            HandleSlide(currInt.Value, other);

        const int steps = 10;

        if (!currInt.HasValue)
            for (int i = 1; i < steps + 1; i++)
            {
                double millis1 = i * (remMillis / steps);
                double millis2 = i * (other.remMillis / steps);
                Vector3 np1 = GetNextPos(millis1) - collideable.Position;
                Vector3 np2 = other.GetNextPos(millis2) - other.Collideable.Position;
                futureInt = (collideable.CollisionBox + np1).FindIntersection(other.Collideable.CollisionBox + np2);
                if (futureInt.HasValue)
                {
                    HandleCollide(futureInt.Value, other, millis1, millis2);
                    remMillis -= millis1;
                    other.remMillis -= millis2;
                    break;
                }
            }
        
        if (futureInt.HasValue || currInt.HasValue)
        {
            other.friction *= 1 - collideable.Material.Friction;
            friction *= 1 - other.Collideable.Material.Friction;
        }
    }

    /// <summary>
    /// Applies the accrued information about velocity, acceleration, friction, etc. to this object. 
    /// This should be the last function called on this object during a run of the Tick loop.
    /// </summary>
    internal void Move()
    {
        collideable.Velocity = ApplyFrictionTo(collideable.Velocity);
        collideable.Acceleration = ApplyFrictionTo(collideable.Acceleration);

        // If this object's velocity after this tick will be less than its static friction, set its velocity and acceleration to zero now as it should not be moved
        if ((collideable.Velocity + collideable.Acceleration * (float)remMillis).Length() <= collideable.Material.StaticFriction)
            collideable.Velocity = collideable.Acceleration = Vector3.Zero;

        collideable.Position = GetNextPos(remMillis);
        collideable.Velocity += collideable.Acceleration * (float)remMillis;
        collideable.Acceleration = Vector3.Zero;
    }
}