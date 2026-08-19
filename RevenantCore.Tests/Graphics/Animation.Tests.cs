using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Graphics.Spec;
using RevenantCore.Scenes;
using RevenantCore.Util;
using static RevenantCore.Tests.Scenes.Scene_Test;

namespace RevenantCore.Tests.Graphics;

file class FakeDrawable : Drawable
{
    public override Vector2 Size => throw new NotImplementedException();

    public override void Draw(ISpriteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    protected override Drawable CopyData()
    {
        throw new NotImplementedException();
    }
}

[TestFixture]
public class RectangleSpec_Test
{
    [TestCase(0, TestName = "RectangleSpec.Data (All Zero)")]
    [TestCase(1, TestName = "RectangleSpec.Data (Non-Zero)")]
    public void Data_CreateFromCoords(int coords)
    {
        RectangleSpec spec = new()
        {
            X = coords,
            Y = coords,
            W = coords,
            H = coords
        };
        Assert.AreEqual(new Rectangle(coords, coords, coords, coords), spec.Data,
            "Data did not match expectation");
    }
}

[TestFixture]
public class Animation_Test
{
    [TestCase(1, 0, 0, TestName = "GetFrame (1 frame; time of 0)")]
    [TestCase(1, 200, 0, TestName = "GetFrame (1 frame; time of 200)")]
    [TestCase(2, 0, 0, TestName = "GetFrame (2 frames; time of 0)")]
    [TestCase(2, 100, 1, TestName = "GetFrame (2 frames; time of 100)")]
    [TestCase(2, 200, 0, TestName = "GetFrame (2 frames; time of 200)")]
    public void GetFrame_ModTime(int numFrames, int time, int expFrame)
    {
        List<Drawable> drawables = [];
        for (int i = 0; i < numFrames; i++)
            drawables.Add(new FakeDrawable());
        Animation animation = new([.. drawables], 100);
        FrameTime frameTime = new(new(new(0, 0, 0, 0, time), new()));
        Assert.AreSame(drawables[expFrame], animation.GetFrame(frameTime));
    }
}

[TestFixture]
public class AnimationCollection_Test
{
    private static FrozenDictionary<string, Animation> MakeDict((string, Drawable)[] values) => values
        .Select(v => new KeyValuePair<string, Animation>(v.Item1, new([v.Item2], 100)))
        .ToFrozenDictionary();

    [Test]
    public void GetFrame_NotFoundNoDef_Error()
    {
        AnimationCollection collection = new(MakeDict([]), null);
        Assert.Throws<ArgumentException>(() => collection.GetFrame("notFound", new(new())));
    }

    [Test]
    public void GetFrame_DefNotFound_Error()
    {
        AnimationCollection collection = new(MakeDict([]), "otherNotFound");
        Assert.Throws<ArgumentException>(() => collection.GetFrame("notFound", new(new())));
    }

    [Test]
    public void GetFrame_NotFoundDef_UseDef()
    {
        FakeDrawable drawable = new();
        AnimationCollection collection = new(MakeDict([("default", drawable)]), "default");
        Assert.AreSame(drawable, collection.GetFrame("notFound", new(new())));
    }

    [Test]
    public void GetFrame_Found_UseFound()
    {
        FakeDrawable drawable = new();
        FakeDrawable defDrawable = new();
        AnimationCollection collection = new(MakeDict([("found", drawable), ("default", defDrawable)]), "default");
        Assert.AreSame(drawable, collection.GetFrame("found", new(new())));
    }
}

[TestFixture]
public class FadeAnimation_Test
{
    [TestCase(false, 0, 0, false, TestName = "Apply_Initial_Clear")]
    [TestCase(true, 0, 255, false, TestName = "Apply_Reverse_Initial_Opaque")]
    [TestCase(false, 50, 127, false, TestName = "Apply_Half_Half")]
    [TestCase(true, 50, 127, false, TestName = "Apply_Reverse_Half_Half")]
    [TestCase(false, 100, 255, true, TestName = "Apply_Final_Opaque")]
    [TestCase(true, 100, 0, true, TestName = "Apply_Reverse_Final_Clear")]
    public void Apply_SetOpacity(bool reverse, int millis, float expAlpha, bool expDead)
    {
        FakeDrawable drawable = new();
        FadeAnimation fade = new(100, reverse);
        Scene scene = new FakeScene();
        fade.Create(scene, new(new()));
        TimeSpan mTime = new(0, 0, 0, 0, millis);
        fade.Apply(drawable, new(new(mTime, mTime)));
        Assert.AreEqual(expDead, fade.IsDead);
        fade.Glean(scene, new(new(mTime, mTime)));
        Assert.AreEqual(expAlpha, drawable.Mask.A);
    }
}

[TestFixture]
public class MoveAnimation_Test
{
    [TestCase(false, 0, 0, 0, false, TestName = "Apply_Initial_Initial")]
    [TestCase(false, 50, 3, 4, false, TestName = "Apply_Half_Half")]
    [TestCase(false, 100, 6, 8, true, TestName = "Apply_Final_Full")]
    public void Apply_SetOpacity(bool reverse, int millis, float expX, float expY, bool expDead)
    {
        FakeDrawable drawable = new();
        MoveAnimation fade = new(100, new(6, 8));
        Scene scene = new FakeScene();
        fade.Create(scene, new(new()));
        TimeSpan mTime = new(0, 0, 0, 0, millis);
        fade.Apply(drawable, new(new(mTime, mTime)));
        Assert.AreEqual(expDead, fade.IsDead);
        fade.Glean(scene, new(new(mTime, mTime)));
        Assert.AreEqual(expX, drawable.Pos.X);
        Assert.AreEqual(expY, drawable.Pos.Y);
    }
}