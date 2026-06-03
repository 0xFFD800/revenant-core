using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RevenantCore.Graphics;

/// <summary>
/// Wraps any used methods of SpriteBatch to allow client code to be tested.
/// </summary>
public interface ISpriteBuffer
{
    void Begin(Matrix transform);
    void End();
    void Draw(Texture2D texture, Vector2 pos, Rectangle? source, Color mask, float rotation, Vector2 origin, SpriteEffects effects);
    void DrawString(SpriteFont font, string text, Vector2 pos, Color mask, float rotation, Vector2 origin, SpriteEffects effects);
}

[ExcludeFromCodeCoverage]
internal class SpriteBuffer(SpriteBatch batch) : ISpriteBuffer
{
    public void Begin(Matrix transform)
    {
        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
    }

    public void End()
    {
        batch.End();
    }

    public void Draw(Texture2D texture, Vector2 pos, Rectangle? source, Color mask, float rotation, Vector2 origin, SpriteEffects effects)
    {
        batch.Draw(texture, pos, source, mask, rotation, origin, 1, effects, 0);
    }

    public void DrawString(SpriteFont font, string text, Vector2 pos, Color mask, float rotation, Vector2 origin, SpriteEffects effects)
    {
        batch.DrawString(font, text, pos, mask, rotation, origin, 1, effects, 0);
    }
}

/// <summary>
/// A wrapper around ISpriteBuffer allowing drawing code to utilize a stack of transformation matrices.
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
    /// Draws the provided drawable object to the active buffer.
    /// </summary>
    /// <param name="drawable">The drawable object to draw to the active buffer.</param>
    void Draw(Drawable drawable);
}

public class Screen(ISpriteBuffer buffer)
{
    /// <summary>
    /// Whether the buffer is currently drawing.
    /// </summary>
    private bool drawing = false;
    private readonly Stack<Matrix> stack = [];
    private Matrix Current
    {
        get;
        set
        {
            if (!drawing)
                drawing = true;
            else
                buffer.End();

            field = value;
            buffer.Begin(value);
        }
    } = Matrix.Identity;

    public void Push(Matrix transform)
    {
        stack.Push(Current);
        Current *= transform;
    }

    public void Pop()
    {
        if (!stack.TryPop(out Matrix toSet) || !drawing)
            throw new InvalidOperationException("Unbalanced calls to Push and Pop!");
        drawing = stack.Count > 0;
        if (drawing)
            Current = toSet;
        else
            buffer.End();
    }

    public void Draw(Drawable drawable)
    {
        drawable.Draw(buffer);
    }
}

/// <summary>
/// An item which can be drawn to a Screen.
/// </summary>
/// <param name="texture">The texture this sprite should draw.</param>
public abstract class Drawable
{
    /// <summary>
    /// Draws this object to the buffer.
    /// </summary>
    /// <param name="buffer"></param>
    public abstract void Draw(ISpriteBuffer buffer);

    /// <summary>
    /// Gets the size of this drawable item.
    /// </summary>
    internal protected abstract Vector2 Size { get; }

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

    private Vector2 Base => new(Size.X / 2, Size.Y);
    private Vector2 Center => new(Size.X / 2, Size.Y / 2);

    public Drawable SetPos(Vector2 pos)
    {
        Pos = pos;
        return this;
    }

    /// <summary>
    /// Sets the position of this drawable so its base (the midpoint of the bottom edge) is at the provided position.
    /// </summary>
    /// <param name="basePos">The position to set the base of this sprite to.</param>
    public Drawable SetBase(Vector2 basePos) => SetPos(basePos - Base);

    /// <summary>
    /// Sets the position of this drawable so its center is at the provided position.
    /// </summary>
    /// <param name="center">The position to set the center of this sprite to.</param>
    public Drawable SetCenter(Vector2 center) => SetPos(center - Center);

    public Drawable SetRotation(float radians)
    {
        Rotation = radians;
        return this;
    }

    public Drawable SetOrigin(Vector2 origin)
    {
        Origin = origin;
        return this;
    }

    /// <summary>
    /// Sets the origin of this drawable to its base.
    /// </summary>
    public Drawable RotateAroundBase() => SetOrigin(Base);

    /// <summary>
    /// Sets the origin of this drawable to its center.
    /// </summary>
    public Drawable RotateAroundCenter() => SetOrigin(Center);

    public Drawable SetSource(Rectangle source)
    {
        Source = source;
        return this;
    }

    public Drawable SetMask(Color mask)
    {
        Mask = mask;
        return this;
    }

    /// <summary>
    /// Sets the opacity of this drawable to be the provided value.
    /// </summary>
    /// <param name="opacity">The opacity at which to draw this drawable.</param>
    public Drawable SetOpacity(float opacity) => SetMask(Mask * opacity);

    public Drawable SetEffects(SpriteEffects effects)
    {
        Effects = effects;
        return this;
    }

    public Drawable AddEffects(SpriteEffects effects) => SetEffects(Effects | effects);
}

/// <summary>
/// Represents a sprite which will be drawn to the screen.
/// </summary>
/// <param name="texture">The texture which this Sprite should be drawn as.</param>
[ExcludeFromCodeCoverage]
internal class Sprite(Texture2D texture) : Drawable
{
    public override void Draw(ISpriteBuffer buffer)
    {
        buffer.Draw(texture, Pos, Source, Mask, Rotation, Origin, Effects);
    }

    protected internal override Vector2 Size => new(texture.Width, texture.Height);
}

/// <summary>
/// Represents text which will be drawn to the screen.
/// </summary>
/// <param name="text">The text to be drawn.</param>
/// <param name="font">The font which should be used to render <paramref name="text"/>.</param>
[ExcludeFromCodeCoverage]
internal class DrawableText(string text, SpriteFont font) : Drawable
{
    public override void Draw(ISpriteBuffer buffer)
    {
        buffer.DrawString(font, text, Pos, Mask, Rotation, Origin, Effects);
    }

    protected internal override Vector2 Size => font.MeasureString(text);
}
