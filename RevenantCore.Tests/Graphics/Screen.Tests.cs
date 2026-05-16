using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RevenantCore.Graphics;

namespace RevenantCore.Tests.Graphics;

file class MockSpriteBuffer(Matrix? expMatrix, bool expDrawing) : ISpriteBuffer
{
    private bool drawing = false;
    private Matrix? actMatrix;

    public void Begin(Matrix transform)
    {
        Assert.False(drawing, "Begin called while already drawing");
        actMatrix = transform;
        drawing = true;
    }

    public void End()
    {
        Assert.True(drawing, "End called while not drawing");
        actMatrix = null;
        drawing = false;
    }

    public void Draw(Texture2D texture, Vector2 pos, Rectangle? source, Color mask, float rotation, Vector2 origin, SpriteEffects effects)
    {
        throw new NotImplementedException();
    }

    public void DrawString(SpriteFont font, string text, Vector2 pos, Color mask, float rotation, Vector2 origin, SpriteEffects effects)
    {
        throw new NotImplementedException();
    }

    public void Validate()
    {
        Assert.AreEqual(expMatrix, actMatrix);
        Assert.AreEqual(expDrawing, drawing);
    }
}

file class MockDrawable(Vector2 size, bool expDrawn) : Drawable
{
    bool drawn = false;
    internal ISpriteBuffer? Buffer { get; private set; } = null;

    public override void Draw(ISpriteBuffer buffer)
    {
        drawn = true;
        Buffer = buffer;
    }

    protected override Vector2 Size => size;

    public void Validate()
    {
        Assert.AreEqual(expDrawn, drawn);
    }
}

[TestFixture]
public class Drawable_Test
{
    [TestCase(1, 1, 2, 2, 1.5F, 1F, TestName = "SetBase 1x1 -> (2,2)")]
    [TestCase(0, 0, 0, 0, 0, 0, TestName = "SetBase 0x0 -> (0,0)")]
    public void SetBase(float w, float h, float x, float y, float expX, float expY)
    {
        Drawable drawable = new MockDrawable(new(w, h), false).SetBase(new(x, y));
        Assert.AreEqual(new Vector2(expX, expY), drawable.Pos);
    }

    [TestCase(1, 1, 2, 2, 1.5F, 1.5F, TestName = "SetCenter 1x1 -> (2,2)")]
    [TestCase(0, 0, 0, 0, 0, 0, TestName = "SetCenter 0x0 -> (0,0)")]
    public void SetCenter(float w, float h, float x, float y, float expX, float expY)
    {
        Drawable drawable = new MockDrawable(new(w, h), false).SetCenter(new(x, y));
        Assert.AreEqual(new Vector2(expX, expY), drawable.Pos);
    }

    [TestCase(1, 1, 0.5F, 1, TestName = "RotateAroundBase 1x1")]
    [TestCase(0, 0, 0, 0, TestName = "RotateAroundBase 0x0")]
    public void RotateAroundBase(float w, float h, float expX, float expY)
    {
        Drawable drawable = new MockDrawable(new(w, h), false).RotateAroundBase();
        Assert.AreEqual(new Vector2(expX, expY), drawable.Origin);
    }

    [TestCase(1, 1, 0.5F, 0.5F, TestName = "RotateAroundCenter 1x1")]
    [TestCase(0, 0, 0, 0, TestName = "RotateAroundCenter 0x0")]
    public void RotateAroundCenter(float w, float h, float expX, float expY)
    {
        Drawable drawable = new MockDrawable(new(w, h), false).RotateAroundCenter();
        Assert.AreEqual(new Vector2(expX, expY), drawable.Origin);
    }

    [Test]
    public void TestBuilder()
    {
        Drawable drawable = new MockDrawable(new(1, 1), false)
            .SetPos(new(1, 1))
            .SetRotation(1)
            .SetOrigin(new(1, 1))
            .SetMask(Color.Black)
            .SetOpacity(0.5F)
            .SetEffects(SpriteEffects.FlipHorizontally)
            .AddEffects(SpriteEffects.FlipVertically);
        Assert.AreEqual(new Vector2(1, 1), drawable.Pos);
        Assert.AreEqual(1, drawable.Rotation);
        Assert.AreEqual(new Vector2(1, 1), drawable.Origin);
        Assert.AreEqual(null, drawable.Source);
        drawable.SetSource(new(0, 0, 1, 1));
        Assert.AreEqual(new Rectangle(0, 0, 1, 1), drawable.Source);
        Assert.AreEqual(Color.Black * 0.5F, drawable.Mask);
        Assert.AreEqual(SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, drawable.Effects);
    }
}

[TestFixture]
public class Screen_Test
{
    [Test(Description = "Screen should not begin drawing with no push or pop operations")]
    public void NoPush_NoBegin()
    {
        MockSpriteBuffer buffer = new(null, false);
        _ = new Screen(buffer);
        buffer.Validate();
    }

    [Test(Description = "Pop with no corresponding Push should raise an exception")]
    public void Pop_NoPush_Raises()
    {
        Screen screen = new(new MockSpriteBuffer(null, false));
        Assert.Throws<InvalidOperationException>(screen.Pop);
    }

    [Test(Description = "A single push with no pop should continue the draw cycle")]
    public void Push_NoPop_Drawing()
    {
        MockSpriteBuffer buffer = new(Matrix.CreateTranslation(0, 0, 1), true);
        Screen screen = new(buffer);
        screen.Push(Matrix.CreateTranslation(0, 0, 1));
        buffer.Validate();
    }

    [Test(Description = "Balanced pushes and pops should end the draw cycle")]
    public void Push_Pop_NotDrawing()
    {
        MockSpriteBuffer buffer = new(null, false);
        Screen screen = new(buffer);
        screen.Push(Matrix.CreateTranslation(0, 0, 1));
        screen.Pop();
        buffer.Validate();
    }
    
    [Test(Description = "Multiple pushes with no corresponding pop should combine their matrices")]
    public void PushTwo_NoPop_Combine()
    {
        MockSpriteBuffer buffer = new(Matrix.CreateTranslation(1, 0, 1), true);
        Screen screen = new(buffer);
        screen.Push(Matrix.CreateTranslation(1, 0, 0));
        screen.Push(Matrix.CreateTranslation(0, 0, 1));
        buffer.Validate();
    }

    [Test(Description = "One pop after two pushes should restore the matrix after the first push")]
    public void PushTwo_PopOne_Restore()
    {
        Matrix firstPush = Matrix.CreateTranslation(1, 0, 0);
        MockSpriteBuffer buffer = new(firstPush, true);
        Screen screen = new(buffer);
        screen.Push(firstPush);
        screen.Push(Matrix.CreateTranslation(0, 0, 1));
        screen.Pop();
        buffer.Validate();
    }

    [Test(Description = "Calling \"Draw\" on a drawable should pass its buffer on")]
    public void Draw_PassBuffer()
    {
        MockSpriteBuffer buffer = new(null, false);
        Screen screen = new(buffer);
        screen.Push(Matrix.Identity);
        MockDrawable drawable = new(Vector2.One, true);
        screen.Draw(drawable);
        screen.Pop();
        drawable.Validate();
        buffer.Validate();
        Assert.AreSame(buffer, drawable.Buffer);
    }
}