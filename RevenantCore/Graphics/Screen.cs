using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
public class SpriteBuffer(SpriteBatch batch) : ISpriteBuffer
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
/// Wraps the necessary methods of SpriteFont to allow client code to be tested.
/// </summary>
public interface IFont
{
    /// <summary>
    /// Creates a new drawable object encapsulating some text.
    /// </summary>
    /// <param name="text">The text to draw as part of this drawable.</param>
    /// <returns>A drawable object which shows some text when drawn on screen.</returns>
    Drawable CreateDrawable(string text);

    /// <summary>
    /// Measures what size this text will be if drawn on-screen in this font.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The size of this text when drawn in this font, returned as X and Y vector coordinates.</returns>
    Vector2 MeasureText(string text);
}

[ExcludeFromCodeCoverage]
public class Font(SpriteFont font) : IFont
{
    public Drawable CreateDrawable(string text) => new DrawableText(text, font);
    public Vector2 MeasureText(string text) => font.MeasureString(text);
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

public class Screen(ISpriteBuffer buffer) : IScreen
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
    /// <param name="buffer">The buffer to draw this object to.</param>
    public abstract void Draw(ISpriteBuffer buffer);

    /// <summary>
    /// Gets the size of this drawable item.
    /// </summary>
    public abstract Vector2 Size { get; }

    /// <summary>
    /// The position at which this sprite will be drawn.
    /// The final position on screen will depend on the state of the camera.
    /// </summary>
    public Vector2 Pos { get; set; } = Vector2.Zero;

    /// <summary>
    /// The offset from the top left corner where it should start being drawn.
    /// Use this to avoid changes to the base and center overriding earlier position offsets.
    /// </summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

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

    /// <summary>
    /// The data array of pixel colors within this drawable.
    /// Currently only supported for sprites.
    /// </summary>
    public abstract Color[] Data { get; set; }

    private Vector2 Base => new(Size.X / 2, Size.Y);
    private Vector2 Center => new(Size.X / 2, Size.Y / 2);
    public Vector2 DrawPos => Pos + Offset;

    public Drawable SetPos(Vector2 pos)
    {
        Pos = pos;
        return this;
    }

    public Drawable SetOffset(Vector2 offset)
    {
        Offset = offset;
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

    public Drawable SetSource(Rectangle? source)
    {
        Source = source;
        return this;
    }

    public Drawable SetMask(Color mask)
    {
        Mask = mask;
        return this;
    }

    public Drawable ApplyMask(Color mask)
    {
        Mask *= mask;
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

    /// <summary>
    /// Overlays this onto another drawable, only drawing the pixels which intersect with the base drawable.
    /// Currently only supported for sprites.
    /// </summary>
    /// <param name="drawable">The base drawable onto which this is being overlaid.</param>
    public abstract Drawable OverlayOnto(Drawable drawable);

    /// <summary>
    /// Merges this image onto layer, treating the parameter as the bottom layer.
    /// </summary>
    /// <param name="layer">The layer to treat as the bottom layer.</param>
    /// <returns>A merged image, with the pixels of this image merged on top of the one provided.</returns>
    public abstract Drawable MergeDown(Drawable layer);

    /// <summary>
    /// Clears the overlay, restoring the original drawn data.
    /// </summary>
    public abstract Drawable ClearOverlay();

    protected abstract Drawable CopyData();

    /// <summary>
    /// Creates a shallow copy of this drawable.
    /// Will reuse the same underlying data (sprite or string), but create a new instance.
    /// </summary>
    /// <returns></returns>
    public Drawable ShallowCopy() => CopyData()
        .SetPos(Pos)
        .SetOffset(Offset)
        .SetRotation(Rotation)
        .SetSource(Source)
        .SetMask(Mask);
}

/// <summary>
/// Represents a sprite which will be drawn to the screen.
/// </summary>
/// <param name="texture">The texture which this Sprite should be drawn as.</param>
[ExcludeFromCodeCoverage]
public class Sprite : Drawable
{
    private readonly Texture2D original, toDraw;

    public Sprite(Texture2D texture)
    {
        original = texture;
        toDraw = new(texture.GraphicsDevice, original.Width, original.Height);
        Color[] data = new Color[texture.Width * texture.Height];
        original.GetData(data);
        toDraw.SetData(data);
    }

    public override void Draw(ISpriteBuffer buffer)
    {
        buffer.Draw(toDraw, DrawPos, Source, Mask, Rotation, Origin, Effects);
    }

    public override Vector2 Size => new(toDraw.Width, toDraw.Height);

    public override Color[] Data
    {
        get
        {
            Color[] data = new Color[toDraw.Width * toDraw.Height];
            toDraw.GetData(data);
            return data;
        }
        set { toDraw.SetData(value); }
    }

    public override Drawable OverlayOnto(Drawable drawable) => MergeWith(drawable, (p, op) => p * op.A, p => p * 0);

    public override Drawable MergeDown(Drawable layer)
    {
        // First, apply the masks since we can't mask the separate portions separately.
        Data = [..Data.Select(d => d * Mask/*Color.Lerp(d, Mask, 0.5F)*/)];
        layer.Data = [..layer.Data.Select(d => d * layer.Mask/*Color.Lerp(d, layer.Mask, 0.5F)*/)];
        Mask = Color.White;
        layer.Mask = Color.White;
        return MergeWith(layer, (p, op) => Color.Lerp(op, p, p.A), p => p);
    }

    private Sprite MergeWith(Drawable other, Func<Color, Color, Color> mergePixel, Func<Color, Color> noMatch)
    {
        Point relativePos = (DrawPos - other.DrawPos).ToPoint();
        Color[] data = Data, otherData = other.Data;
        for (int x = 0; x < toDraw.Width; x++)
            for (int y = 0; y < toDraw.Height; y++)
                if (x + relativePos.X >= 0 && x + relativePos.X < other.Size.X && y + relativePos.Y >= 0 && y + relativePos.Y < other.Size.Y)
                    data[x + (y * toDraw.Width)] = mergePixel(data[x + (y * toDraw.Width)], otherData[x + relativePos.X + ((y + relativePos.Y) * (int)other.Size.X)]);
                else
                    data[x + (y * toDraw.Width)] = noMatch(data[x + (y * toDraw.Width)]);
        toDraw.SetData(data);
        return this;
    }

    public override Drawable ClearOverlay()
    {
        Color[] data = new Color[original.Width * original.Height];
        original.GetData(data);
        toDraw.SetData(data);
        return this;
    }

    protected override Drawable CopyData() => new Sprite(toDraw);
}

/// <summary>
/// Represents text which will be drawn to the screen.
/// </summary>
/// <param name="text">The text to be drawn.</param>
/// <param name="font">The font which should be used to render <paramref name="text"/>.</param>
[ExcludeFromCodeCoverage]
public class DrawableText(string text, SpriteFont font) : Drawable
{
    public override void Draw(ISpriteBuffer buffer)
    {
        buffer.DrawString(font, text, DrawPos, Mask, Rotation, Origin, Effects);
    }

    public override Vector2 Size => font.MeasureString(text);

    public override Color[] Data { get => []; set { } }

    public override Drawable OverlayOnto(Drawable drawable) => this;

    public override Drawable MergeDown(Drawable layer) => this;

    public override Drawable ClearOverlay() => this;

    protected override Drawable CopyData() => new DrawableText(text, font);
}
