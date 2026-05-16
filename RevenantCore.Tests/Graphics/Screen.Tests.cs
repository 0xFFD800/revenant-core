using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RevenantCore.Graphics;

namespace RevenantCore.Tests.Graphics;

file class FakeSpriteBuffer(Matrix? expMatrix, bool expDrawing) : ISpriteBuffer
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

[TestFixture]
public class Drawable_Test
{
    private class FakeDrawable(Vector2 size) : Drawable
    {
        public override void Draw(ISpriteBuffer buffer)
        {
            throw new NotImplementedException();
        }

        protected override Vector2 Size => size;
    }

    [TestCase(1, 1, 2, 2, 1.5F, 1F, TestName = "SetBase 1x1 -> (2,2)")]
    [TestCase(0, 0, 0, 0, 0, 0, TestName = "SetBase 0x0 -> (0,0)")]
    public void SetBase(float w, float h, float x, float y, float expX, float expY)
    {
        Drawable drawable = new FakeDrawable(new(w, h)).SetBase(new(x, y));
        Assert.AreEqual(new Vector2(expX, expY), drawable.Pos);
    }

    [TestCase(1, 1, 2, 2, 1.5F, 1.5F, TestName = "SetCenter 1x1 -> (2,2)")]
    [TestCase(0, 0, 0, 0, 0, 0, TestName = "SetCenter 0x0 -> (0,0)")]
    public void SetCenter(float w, float h, float x, float y, float expX, float expY)
    {
        Drawable drawable = new FakeDrawable(new(w, h)).SetCenter(new(x, y));
        Assert.AreEqual(new Vector2(expX, expY), drawable.Pos);
    }

    [TestCase(1, 1, 0.5F, 1, TestName = "RotateAroundBase 1x1")]
    [TestCase(0, 0, 0, 0, TestName = "RotateAroundBase 0x0")]
    public void RotateAroundBase(float w, float h, float expX, float expY)
    {
        Drawable drawable = new FakeDrawable(new(w, h)).RotateAroundBase();
        Assert.AreEqual(new Vector2(expX, expY), drawable.Origin);
    }

    [TestCase(1, 1, 0.5F, 0.5F, TestName = "RotateAroundCenter 1x1")]
    [TestCase(0, 0, 0, 0, TestName = "RotateAroundCenter 0x0")]
    public void RotateAroundCenter(float w, float h, float expX, float expY)
    {
        Drawable drawable = new FakeDrawable(new(w, h)).RotateAroundCenter();
        Assert.AreEqual(new Vector2(expX, expY), drawable.Origin);
    }

    [Test]
    public void TestBuilder()
    {
        Drawable drawable = new FakeDrawable(new(1, 1))
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
    [Test]
    public void TestInitial()
    {
        FakeSpriteBuffer buffer = new(null, false);
        Matrix initial = new();
        _ = new Screen(buffer, initial);
        buffer.Validate();
    }
}