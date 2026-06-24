using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Graphics.Spec;
using RevenantCore.Util;

namespace RevenantCore.Tests.Graphics;

file class FakeDrawable : Drawable
{
    protected override Vector2 Size => throw new NotImplementedException();

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
        Assert.AreSame(drawables[expFrame], animation.GetFrame(frameTime.Millis));
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
        Assert.Throws<ArgumentException>(() => collection.GetFrame("notFound", 0));
    }

    [Test]
    public void GetFrame_DefNotFound_Error()
    {
        AnimationCollection collection = new(MakeDict([]), "otherNotFound");
        Assert.Throws<ArgumentException>(() => collection.GetFrame("notFound", 0));
    }

    [Test]
    public void GetFrame_NotFoundDef_UseDef()
    {
        FakeDrawable drawable = new();
        AnimationCollection collection = new(MakeDict([("default", drawable)]), "default");
        Assert.AreSame(drawable, collection.GetFrame("notFound", 0));
    }

    [Test]
    public void GetFrame_Found_UseFound()
    {
        FakeDrawable drawable = new();
        FakeDrawable defDrawable = new();
        AnimationCollection collection = new(MakeDict([("found", drawable), ("default", defDrawable)]), "default");
        Assert.AreSame(drawable, collection.GetFrame("found", 0));
    }
}