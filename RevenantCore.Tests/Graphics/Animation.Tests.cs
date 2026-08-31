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

file class MockAnimation(bool isDead, bool expCreated, bool expGleaned, bool expApplied) : IAnimationHook
{
    private bool created = false, gleaned = false, applied = false;

    public bool IsDead => isDead;

    public void Apply(Drawable drawable, FrameTime time)
    {
        applied = true;
    }

    public void Create(Scene scene, FrameTime time)
    {
        created = true;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        gleaned = true;
    }

    internal void Validate()
    {
        Assert.AreEqual(expCreated, created);
        Assert.AreEqual(expGleaned, gleaned);
        Assert.AreEqual(expApplied, applied);
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
    [TestCase(0, false, 0, 0, false, TestName = "Apply_Initial_Initial")]
    [TestCase(50, false, 3, 4, false, TestName = "Apply_Half_Half")]
    [TestCase(100, false, 6, 8, true, TestName = "Apply_Final_Full")]
    [TestCase(0, true, 6, 8, false, TestName = "ApplyReverse_Final_Initial")]
    [TestCase(50, true, 3, 4, false, TestName = "ApplyReverse_Half_Half")]
    [TestCase(100, true, 0, 0, true, TestName = "ApplyReverse_Initial_Full")]
    public void Apply_SetOpacity(int millis, bool reverse, float expX, float expY, bool expDead)
    {
        FakeDrawable drawable = new();
        MoveAnimation move = new(100, new(6, 8), reverse);
        Scene scene = new FakeScene();
        move.Create(scene, new(new()));
        TimeSpan mTime = new(0, 0, 0, 0, millis);
        move.Apply(drawable, new(new(mTime, mTime)));
        Assert.AreEqual(expDead, move.IsDead);
        move.Glean(scene, new(new(mTime, mTime)));
        Assert.AreEqual(expX, drawable.Pos.X);
        Assert.AreEqual(expY, drawable.Pos.Y);
    }
}

[TestFixture]
public class RotateAnimation_Test
{
    [TestCase(0, 0, false, TestName = "Apply_Initial_Initial")]
    [TestCase(50, 1, false, TestName = "Apply_Half_Half")]
    [TestCase(100, 2, true, TestName = "Apply_Final_Full")]
    public void Apply_SetOpacity(int millis, float expRotation, bool expDead)
    {
        FakeDrawable drawable = new();
        RotateAnimation rotate = new(100, 2);
        Scene scene = new FakeScene();
        rotate.Create(scene, new(new()));
        TimeSpan mTime = new(0, 0, 0, 0, millis);
        rotate.Apply(drawable, new(new(mTime, mTime)));
        Assert.AreEqual(expDead, rotate.IsDead);
        rotate.Glean(scene, new(new(mTime, mTime)));
        Assert.AreEqual(expRotation, drawable.Rotation);
    }
}

[TestFixture]
public class AnimationLoop_Test
{
    [TestCase(false, TestName = "Create_OneLiving_AdvanceToLiving")]
    [TestCase(true, TestName = "Create_NoneLiving_Dead")]
    public void Create_TryAdvance(bool secondDead)
    {
        MockAnimation[] animations = [
            new(true, true, true, false), 
            new(secondDead, true, secondDead, false), 
            new(true, secondDead, secondDead, false)];
        AnimationLoop loop = new(animations);
        loop.Create(new FakeScene(), new(new()));
        foreach (MockAnimation animation in animations)
            animation.Validate();
        Assert.AreEqual(secondDead, loop.IsDead);
    }

    [Test]
    public void Glean_GleanCurrent()
    {
        MockAnimation[] animations = [new(false, false, true, false), new(false, false, false, false)];
        AnimationLoop loop = new(animations);
        loop.Glean(new FakeScene(), new(new()));
        foreach (MockAnimation animation in animations)
            animation.Validate();
    }

    [Test]
    public void Apply_Living_Apply()
    {
        MockAnimation[] animations = [new(false, true, false, true), new(false, false, false, false)];
        AnimationLoop loop = new(animations);
        loop.Create(new FakeScene(), new(new()));
        loop.Apply(new FakeDrawable(), new(new()));
        foreach (MockAnimation animation in animations)
            animation.Validate();
        Assert.IsFalse(loop.IsDead);
    }

    [Test]
    public void Apply_OneDead_Advance()
    {
        MockAnimation[] animations = [new(true, true, true, false), new(false, true, false, true)];
        AnimationLoop loop = new(animations);
        loop.Create(new FakeScene(), new(new()));
        loop.Apply(new FakeDrawable(), new(new()));
        foreach (MockAnimation animation in animations)
            animation.Validate();
        Assert.IsFalse(loop.IsDead);
    }
 
    [Test]
    public void Apply_AllDead_Die()
    {
        MockAnimation[] animations = [new(true, true, true, false), new(true, true, true, false)];
        AnimationLoop loop = new(animations);
        loop.Create(new FakeScene(), new(new()));
        loop.Apply(new FakeDrawable(), new(new()));
        foreach (MockAnimation animation in animations)
            animation.Validate();
        Assert.IsTrue(loop.IsDead);
    }
}