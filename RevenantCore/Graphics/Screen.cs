using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RevenantCore.Graphics;

/// <summary>
/// A wrapper around the default SpriteBatch allowing drawing code to utilize a stack of transformation matrices.
/// Calls to Push and Pop <em>must</em> be balanced on any given run of the Draw loop.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Pushes a new transformation matrix.
    /// If there is no active sprite buffer, this will create a new one with the provided matrix.
    /// Otherwise, this will flush the existing sprite buffer and create a new one with the multiple of the current two.
    /// </summary>
    /// <param name="transform">The transformation matrix to apply to the new buffer.</param>
    void Push(Matrix transform);

    /// <summary>
    /// Pop the active transformation matrix off the stack.
    /// </summary>
    void Pop();

    /// <summary>
    /// Draws the provided sprite to the active buffer.
    /// </summary>
    /// <param name="sprite">The sprite to draw to the active buffer.</param>
    void Draw(Sprite sprite);
}

public class Screen(SpriteBatch buffer, Matrix initial) : IScreen
{
    private bool firstPush = true;
    private readonly Stack<Matrix> stack = [];
    private Matrix Current
    {
        get;
        set
        {
            if (firstPush)
                firstPush = false;
            else
                buffer.End();

            field = value;
            buffer.Begin(samplerState: SamplerState.PointClamp, transformMatrix: value);
        }
    } = initial;

    public void Push(Matrix transform)
    {
        stack.Push(Current);
        Current *= transform;
    }

    public void Pop()
    {
        Debug.Assert(stack.TryPop(out Matrix toSet) && !firstPush, "Unbalanced calls to Push and Pop!");
        Current = toSet;
    }

    public void Draw(Sprite sprite)
    {
        buffer.Draw(sprite.Texture, sprite.Pos, sprite.Source, sprite.Mask, sprite.Rotation, sprite.Origin, 1F, sprite.Effects, 0);
    }
}

/// <summary>
/// A helper class which provides properties to be used by the Screen's Draw method.
/// </summary>
/// <param name="texture">The texture this sprite should draw.</param>
public class Sprite(Texture2D texture)
{
    /// <summary>
    /// The texture which will be drawn by this sprite.
    /// </summary>
    public Texture2D Texture => texture;

    /// <summary>
    /// The position at which this sprite will be drawn.
    /// The final position on screen will depend on the state of the camera.
    /// </summary>
    public Vector2 Pos { get; set; } = Vector2.Zero;

    /// <summary>
    /// The angle at which to rotate the sprite, in radians.
    /// </summary>
    public float Rotation { get; set; } = 0;

    /// <summary>
    /// The position around which to rotate the sprite, relative to the top left corner of the sprite.
    /// </summary>
    public Vector2 Origin { get; set; } = Vector2.Zero;

    /// <summary>
    /// The area of the sprite to draw, measured in pixels on the original sprite.
    /// </summary>
    public Rectangle? Source { get; set; }

    /// <summary>
    /// The color mask to apply to the image when drawing it.
    /// White will draw the original image without changes.
    /// Multiplying the mask by a scalar will change the opacity.
    /// </summary>
    public Color Mask { get; set; } = Color.White;

    /// <summary>
    /// The effects with which to display the image.
    /// </summary>
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    private Vector2 Base => new(Texture.Width / 2, Texture.Height);
    private Vector2 Center => new(Texture.Width / 2, Texture.Height / 2);

    public Sprite SetPos(Vector2 pos)
    {
        Pos = pos;
        return this;
    }

    /// <summary>
    /// Sets the position of this sprite so its base (the midpoint of the bottom edge) is at the provided position.
    /// </summary>
    /// <param name="basePos">The position to set the base of this sprite to.</param>
    public Sprite SetBase(Vector2 basePos) => SetPos(basePos - Base);

    /// <summary>
    /// Sets the position of this sprite so its center is at the provided position.
    /// </summary>
    /// <param name="center">The position to set the center of this sprite to.</param>
    public Sprite SetCenter(Vector2 center) => SetPos(center - Center);

    public Sprite SetRotation(float radians)
    {
        Rotation = radians;
        return this;
    }

    public Sprite SetOrigin(Vector2 origin)
    {
        Origin = origin;
        return this;
    }

    /// <summary>
    /// Sets the origin of this sprite to its base.
    /// </summary>
    public Sprite RotateAroundBase() => SetOrigin(Base);

    /// <summary>
    /// Sets the origin of this sprite to its center.
    /// </summary>
    public Sprite RotateAroundCenter() => SetOrigin(Center);

    public Sprite SetSource(Rectangle source)
    {
        Source = source;
        return this;
    }

    public Sprite SetMask(Color mask)
    {
        Mask = mask;
        return this;
    }

    public Sprite CombineMask(Color mask) => SetMask(Mask * mask);

    /// <summary>
    /// Sets the opacity of this sprite to be the provided value.
    /// </summary>
    /// <param name="opacity">The opacity at which to draw this sprite.</param>
    public Sprite SetOpacity(float opacity) => SetMask(Mask * opacity);

    public Sprite SetEffects(SpriteEffects effects)
    {
        Effects = effects;
        return this;
    }

    public Sprite AddEffects(SpriteEffects effects) => SetEffects(Effects | effects);
}