using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Tests.Util;

[TestFixture]
public class NumericsExtension_Test
{
    [TestCase(0, 0, 0, 0, 0, 0, TestName = "Abs (All Zero)")]
    [TestCase(-1, -2, -3, 1, 2, 3, TestName = "Abs (All Negative)")]
    [TestCase(1, 2, 3, 1, 2, 3, TestName = "Abs (All Positive)")]
    [TestCase(1, -2, 3, 1, 2, 3, TestName = "Abs (Mixed)")]
    public void VecAbs(float x, float y, float z, float expX, float expY, float expZ)
    {
        Assert.AreEqual(new Vector3(expX, expY, expZ), new Vector3(x, y, z).Abs());
    }

    [TestCase(0, 0, 0, 1, 2, 3, TestName = "Clamp (All Below)")]
    [TestCase(5, 5, 5, 2, 3, 4, TestName = "Clamp (All Above)")]
    [TestCase(1.5F, 2.5F, 3.5F, 1.5F, 2.5F, 3.5F, TestName = "Clamp (All Between)")]
    [TestCase(1.5F, 0, 5, 1.5F, 2, 4, TestName = "Clamp (Mixed)")]
    public void VecClamp(float x, float y, float z, float expX, float expY, float expZ)
    {
        Assert.AreEqual(new Vector3(expX, expY, expZ), new Vector3(x, y, z).Clamp(new(1, 2, 3), new(2, 3, 4)));
    }

    [TestCase(0, 0, 0, 0, 0, 0, TestName = "Sign (All Zero)")]
    [TestCase(-1, -2, -3, -1, -1, -1, TestName = "Sign (All Negative)")]
    [TestCase(1, 2, 3, 1, 1, 1, TestName = "Sign (All Positive)")]
    [TestCase(0, -2, 3, 0, -1, 1, TestName = "Sign (Mixed)")]
    public void VecSign(float x, float y, float z, float expX, float expY, float expZ)
    {
        Assert.AreEqual(new Vector3(expX, expY, expZ), new Vector3(x, y, z).Sign());
    }

    [TestCase(0, 1, 0, 0, 0, 0, 0, 0, TestName = "BB Add (All Zero)")]
    [TestCase(1, 2, -1, -2, -3, 0, -1, -2, TestName = "BB Add (All Negative)")]
    [TestCase(-1, 3, 1, 2, 3, 0, 1, 2, TestName = "BB Add (All Positive)")]
    [TestCase(0, 1, 0, 1, -1, 0, 1, -1, TestName = "BB Add (Mixed)")]
    public void BoundingBoxAdd(float coord, float size, float x, float y, float z, float expX, float expY, float expZ)
    {
        Vector3 min = new(coord, coord, coord);
        BoundingBox b = new(min, min + new Vector3(size, size, size));
        Vector3 expMin = new(expX, expY, expZ);
        Assert.AreEqual(expMin, (b + new Vector3(x, y, z)).Min);
    }
}