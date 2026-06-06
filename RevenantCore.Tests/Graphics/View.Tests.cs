using Microsoft.Xna.Framework;
using NUnit.Framework.Internal;
using RevenantCore.Graphics;

namespace RevenantCore.Tests.Graphics;

[TestFixture]
public class Camera_Test
{
    private static void Project(Vector3 pos3, Vector2 expPos2, Vector2 pos, Vector2 size)
    {
        Camera camera = new(size, size * 2);
        camera.MoveTo(pos);
        Vector2 pos2 = camera.Project(pos3);
        Assert.AreEqual(expPos2, pos2);
    }

    [TestCase(0, 0, 0, 0, 160, TestName = "Project 320x80 Ground Near Left Corner")]
    [TestCase(0, 0, 80, 20, 120, TestName = "Project 320x80 Ground Far Left Corner")]
    [TestCase(320, 0, 80, 300, 120, TestName = "Project 320x80 Ground Far Right Corner")]
    [TestCase(320, 0, 0, 320, 160, TestName = "Project 320x80 Ground Near Right Corner")]
    [TestCase(160, 0, 40, 160, 140, TestName = "Project 320x80 Ground Center")]
    [TestCase(0, 40, 0, 0, 120, TestName = "Project 320x80 Air Near Left Corner")]
    [TestCase(0, 40, 80, 20, 100, TestName = "Project 320x80 Air Far Left Corner")]
    [TestCase(320, 40, 80, 300, 100, TestName = "Project 320x80 Air Far Right Corner")]
    [TestCase(320, 40, 0, 320, 120, TestName = "Project 320x80 Air Near Right Corner")]
    [TestCase(160, 40, 40, 160, 110, TestName = "Project 320x80 Air Center")]
    public void Project320x80(float x, float y, float z, float expX, float expY)
    {
        Project(new(x, y, z), new(expX, expY), new(0, 0), new(320, 80));
    }

    [TestCase(0, 0, 0, 0, 240, TestName = "Project 640x120 Ground Near Left Corner")]
    [TestCase(0, 0, 120, 30, 180, TestName = "Project 640x120 Ground Far Left Corner")]
    [TestCase(640, 0, 120, 610, 180, TestName = "Project 640x120 Ground Far Right Corner")]
    [TestCase(640, 0, 0, 640, 240, TestName = "Project 640x120 Ground Near Right Corner")]
    [TestCase(320, 0, 60, 320, 210, TestName = "Project 640x120 Ground Center")]
    [TestCase(0, 60, 0, 0, 180, TestName = "Project 640x120 Air Near Left Corner")]
    [TestCase(0, 60, 120, 30, 150, TestName = "Project 640x120 Air Far Left Corner")]
    [TestCase(640, 60, 120, 610, 150, TestName = "Project 640x120 Air Far Right Corner")]
    [TestCase(640, 60, 0, 640, 180, TestName = "Project 640x120 Air Near Right Corner")]
    [TestCase(320, 60, 60, 320, 165, TestName = "Project 640x120 Air Center")]
    public void Project640x120(float x, float y, float z, float expX, float expY)
    {
        Project(new(x, y, z), new(expX, expY), new(0, 0), new(640, 120));
    }

    [Test(Description = "The position of the camera should not affect the vector projection")]
    public void ProjectNonzeroPos()
    {
        Project(new(320, 0, 80), new(320, 120), new(160, 40), new(320, 80));
    }

    [Test(Description = "The position of the camera should not drop below 0, 0")]
    public void MoveTo_Negative_Zero()
    {
        Camera camera = new(new(10), new(100));
        camera.MoveTo(new(5));
        Assert.AreEqual(new Vector3(5, 5, 0), camera.Transform.Translation);
        camera.MoveTo(new(-5));
        Assert.AreEqual(Vector3.Zero, camera.Transform.Translation);
    }

    [Test(Description = "The bounds of the camera should not be able to go beyond the total size")]
    public void MoveTo_TotalSize_TotalMinusBounds()
    {
        Camera camera = new(new(10), new(100));
        Assert.AreEqual(Vector3.Zero, camera.Transform.Translation);
        camera.MoveTo(new(100));
        Assert.AreEqual(new Vector3(90, 90, 0), camera.Transform.Translation);
    }
}    

[TestFixture]
public class CameraCollection_Test
{
    [TestCase(0, 0, 0, 0, 180, TestName = "MoveAllTo_SameSize_SnDoesntMove (Near Bottom Left)")]
    [TestCase(160, 90, 0, 160, 90, TestName = "MoveAllTo_SameSize_SnDoesntMove (Near Center)")]
    [TestCase(320, 180, 0, 320, 0, TestName = "MoveAllTo_SameSize_SnDoesntMove (Near Upper Right)")]
    public void MoveAllTo_SameSize_SnDoesntMove(float scenePosX, float scenePosY, float scenePosZ, float expFgX, float expFgY)
    {
        CameraCollection collection = new(new(320, 180), new(320, 180));
        collection.MoveAllTo(new(scenePosX, scenePosY, scenePosZ));
        Assert.AreEqual(Vector3.Zero, collection.Get(DrawLayer.Base).Transform.Translation);
        Assert.AreEqual(Vector3.Zero, collection.Get(DrawLayer.Background).Transform.Translation);
        Assert.AreEqual(Vector3.Zero, collection.Get(DrawLayer.Scene).Transform.Translation);
        Assert.AreEqual(new Vector3(expFgX, expFgY, 0), collection.Get(DrawLayer.Foreground).Transform.Translation);
        Assert.AreEqual(Vector3.Zero, collection.Get(DrawLayer.UI).Transform.Translation);
    }
}