using Microsoft.Xna.Framework;
using RevenantCore.Graphics.Spec;

namespace RevenantCore.Tests.Graphics;

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