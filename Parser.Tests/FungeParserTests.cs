using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Parser.Tests;

[TestClass]
public class FungeParserTests
{
    [TestMethod]
    public void ParseSingleChar_StoresCorrectly()
    {
        var space = FungeParser.Parse("@");
        Assert.AreEqual('@', space[new FungeVector(0, 0)]);
    }

    [TestMethod]
    public void ParseSpace_ReturnsDefaultCell()
    {
        var space = FungeParser.Parse(" @");
        Assert.AreEqual(' ', space[new FungeVector(0, 0)]);
        Assert.AreEqual('@', space[new FungeVector(1, 0)]);
    }

    [TestMethod]
    public void ParseMultiLine_CorrectCoordinates()
    {
        var source = "AB\nCD";
        var space = FungeParser.Parse(source);
        Assert.AreEqual('A', space[new FungeVector(0, 0)]);
        Assert.AreEqual('B', space[new FungeVector(1, 0)]);
        Assert.AreEqual('C', space[new FungeVector(0, 1)]);
        Assert.AreEqual('D', space[new FungeVector(1, 1)]);
    }

    [TestMethod]
    public void ParseCrLf_IgnoresCarriageReturn()
    {
        var space = FungeParser.Parse("A\r\nB");
        Assert.AreEqual('A', space[new FungeVector(0, 0)]);
        Assert.AreEqual('B', space[new FungeVector(0, 1)]);
    }

    [TestMethod]
    public void UnsetCell_ReturnsSpace()
    {
        var space = FungeParser.Parse("@");
        Assert.AreEqual(' ', space[new FungeVector(99, 99)]);
    }

    [TestMethod]
    public void BoundingBox_CorrectAfterParse()
    {
        var space = FungeParser.Parse("AB\nCD");
        Assert.AreEqual(0, space.MinX);
        Assert.AreEqual(0, space.MinY);
        Assert.AreEqual(1, space.MaxX);
        Assert.AreEqual(1, space.MaxY);
    }
}

[TestClass]
public class FungeVectorTests
{
    [TestMethod]
    public void RotateRight_EastBecomeSouth()
    {
        Assert.AreEqual(FungeVector.South, FungeVector.East.RotateRight());
    }

    [TestMethod]
    public void RotateRight_SouthBecomeWest()
    {
        Assert.AreEqual(FungeVector.West, FungeVector.South.RotateRight());
    }

    [TestMethod]
    public void RotateLeft_EastBecomeNorth()
    {
        Assert.AreEqual(FungeVector.North, FungeVector.East.RotateLeft());
    }

    [TestMethod]
    public void Reflect_EastBecomeWest()
    {
        Assert.AreEqual(FungeVector.West, FungeVector.East.Reflect());
    }

    [TestMethod]
    public void Addition()
    {
        Assert.AreEqual(new FungeVector(3, 5), new FungeVector(1, 2) + new FungeVector(2, 3));
    }
}

[TestClass]
public class FungeSpaceTests
{
    [TestMethod]
    public void Advance_WrapsEastBeyondMaxX()
    {
        var space = FungeParser.Parse("ABC");
        // MinX=0, MaxX=2, Width=3
        // Advance East from (2,0): next (3,0) -> wraps to (0,0)
        var next = space.Advance(new FungeVector(2, 0), FungeVector.East);
        Assert.AreEqual(new FungeVector(0, 0), next);
    }

    [TestMethod]
    public void Advance_WrapsWestBeyondMinX()
    {
        var space = FungeParser.Parse("ABC");
        var next = space.Advance(new FungeVector(0, 0), FungeVector.West);
        Assert.AreEqual(new FungeVector(2, 0), next);
    }

    [TestMethod]
    public void Advance_WrapsSouthBeyondMaxY()
    {
        var space = FungeParser.Parse("A\nB\nC");
        var next = space.Advance(new FungeVector(0, 2), FungeVector.South);
        Assert.AreEqual(new FungeVector(0, 0), next);
    }

    [TestMethod]
    public void SetCell_UpdatesBoundingBox()
    {
        var space = new FungeSpace();
        space[new FungeVector(5, 10)] = 'X';
        Assert.AreEqual(5, space.MinX);
        Assert.AreEqual(5, space.MaxX);
        Assert.AreEqual(10, space.MinY);
        Assert.AreEqual(10, space.MaxY);
    }
}
