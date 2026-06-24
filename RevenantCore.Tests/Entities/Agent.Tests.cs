using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Scenes;

namespace RevenantCore.Tests.Entities;

file class FakeAnimationCollection() : AnimationCollection(new List<KeyValuePair<string, Animation>>().ToFrozenDictionary(), null);

file class FakeLoader() : ILoader
{
    public Drawable LoadSprite(string path)
    {
        throw new NotImplementedException();
    }
}

file class FakeScene() : Scene(new(new(new FakeLoader(), []), new([])), new(), "default");

[TestFixture]
public class NullAgent_Test
{
    [Test]
    public void Apply_DoNothing()
    {
        IAgent agent = new NullAgent();
        Entity entity = new(agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene);
        entity.Tick(new FakeScene(), new(new()));
        Assert.AreEqual(Vector3.Zero, entity.Position);
        Assert.AreEqual(Vector3.Zero, entity.Velocity);
        Assert.AreEqual(Vector3.Zero, entity.Acceleration);
    }
}