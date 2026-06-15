namespace Esolang.Funge.Parser.Tests;

public class FungeParserTests
{
    [Test]
    public async Task ParseSingleChar_StoresCorrectly()
    {
        var space = FungeParser.Parse("@");
        await Assert.That(space[new FungeVector(0, 0)]).IsEqualTo('@');
    }

    [Test]
    public async Task ParseSpace_ReturnsDefaultCell()
    {
        var space = FungeParser.Parse(" @");
        await Assert.That(space[new FungeVector(0, 0)]).IsEqualTo(' ');
        await Assert.That(space[new FungeVector(1, 0)]).IsEqualTo('@');
    }

    [Test]
    public async Task ParseMultiLine_CorrectCoordinates()
    {
        var source = "AB\nCD";
        var space = FungeParser.Parse(source);
        await Assert.That(space[new FungeVector(0, 0)]).IsEqualTo('A');
        await Assert.That(space[new FungeVector(1, 0)]).IsEqualTo('B');
        await Assert.That(space[new FungeVector(0, 1)]).IsEqualTo('C');
        await Assert.That(space[new FungeVector(1, 1)]).IsEqualTo('D');
    }

    [Test]
    public async Task ParseCrLf_IgnoresCarriageReturn()
    {
        var space = FungeParser.Parse("A\r\nB");
        await Assert.That(space[new FungeVector(0, 0)]).IsEqualTo('A');
        await Assert.That(space[new FungeVector(0, 1)]).IsEqualTo('B');
    }

    [Test]
    public async Task UnsetCell_ReturnsSpace()
    {
        var space = FungeParser.Parse("@");
        await Assert.That(space[new FungeVector(99, 99)]).IsEqualTo(' ');
    }

    [Test]
    public async Task BoundingBox_CorrectAfterParse()
    {
        var space = FungeParser.Parse("AB\nCD");
        await Assert.That(space.MinX).IsEqualTo(0);
        await Assert.That(space.MinY).IsEqualTo(0);
        await Assert.That(space.MinZ).IsEqualTo(0);
        await Assert.That(space.MaxX).IsEqualTo(1);
        await Assert.That(space.MaxY).IsEqualTo(1);
        await Assert.That(space.MaxZ).IsEqualTo(0);
    }

    [Test]
    public async Task BoundingBox_IncludesSpacesInSource()
    {
        var space = FungeParser.Parse("A  ");
        await Assert.That(space.MinX).IsEqualTo(0);
        await Assert.That(space.MaxX).IsEqualTo(2);
        await Assert.That(space.MinY).IsEqualTo(0);
        await Assert.That(space.MaxY).IsEqualTo(0);
        await Assert.That(space.MinZ).IsEqualTo(0);
        await Assert.That(space.MaxZ).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SgmlSpaces_AreTreatedAsSpaceCells()
    {
        var space = FungeParser.Parse("A\t\vB");
        await Assert.That(space[new FungeVector(0, 0)]).IsEqualTo('A');
        await Assert.That(space[new FungeVector(1, 0)]).IsEqualTo(' ');
        await Assert.That(space[new FungeVector(2, 0)]).IsEqualTo(' ');
        await Assert.That(space[new FungeVector(3, 0)]).IsEqualTo('B');
    }

    [Test]
    public async Task Parse_FormFeed_StartsNewLayer()
    {
        var space = FungeParser.Parse("A\fB");
        await Assert.That(space[new FungeVector(0, 0, 0)]).IsEqualTo('A');
        await Assert.That(space[new FungeVector(0, 0, 1)]).IsEqualTo('B');
        await Assert.That(space.MinZ).IsEqualTo(0);
        await Assert.That(space.MaxZ).IsEqualTo(1);
    }
}

public class FungeVectorTests
{
    [Test]
    public async Task RotateRight_EastBecomeSouth()
        => await Assert.That(FungeVector.East.RotateRight()).IsEqualTo(FungeVector.South);

    [Test]
    public async Task RotateRight_SouthBecomeWest()
        => await Assert.That(FungeVector.South.RotateRight()).IsEqualTo(FungeVector.West);

    [Test]
    public async Task RotateLeft_EastBecomeNorth()
        => await Assert.That(FungeVector.East.RotateLeft()).IsEqualTo(FungeVector.North);

    [Test]
    public async Task Reflect_EastBecomeWest()
        => await Assert.That(FungeVector.East.Reflect()).IsEqualTo(FungeVector.West);

    [Test]
    public async Task Addition()
        => await Assert.That(new FungeVector(1, 2, 3) + new FungeVector(2, 3, 4)).IsEqualTo(new FungeVector(3, 5, 7));
}

public class FungeSpaceTests
{
    [Test]
    public async Task Advance_WrapsEastBeyondMaxX()
    {
        var space = FungeParser.Parse("ABC");
        // MinX=0, MaxX=2, Width=3
        // Advance East from (2,0): next (3,0) -> wraps to (0,0)
        var next = space.Advance(new FungeVector(2, 0), FungeVector.East);
        await Assert.That(next).IsEqualTo(new FungeVector(0, 0));
    }

    [Test]
    public async Task Advance_WrapsWestBeyondMinX()
    {
        var space = FungeParser.Parse("ABC");
        var next = space.Advance(new FungeVector(0, 0), FungeVector.West);
        await Assert.That(next).IsEqualTo(new FungeVector(2, 0));
    }

    [Test]
    public async Task Advance_WrapsSouthBeyondMaxY()
    {
        var space = FungeParser.Parse("A\nB\nC");
        var next = space.Advance(new FungeVector(0, 2), FungeVector.South);
        await Assert.That(next).IsEqualTo(new FungeVector(0, 0));
    }

    [Test]
    public async Task SetCell_UpdatesBoundingBox()
    {
        var space = new FungeSpace();
        space[new FungeVector(5, 10, 15)] = 'X';
        await Assert.That(space.MinX).IsEqualTo(5);
        await Assert.That(space.MaxX).IsEqualTo(5);
        await Assert.That(space.MinY).IsEqualTo(10);
        await Assert.That(space.MaxY).IsEqualTo(10);
        await Assert.That(space.MinZ).IsEqualTo(15);
        await Assert.That(space.MaxZ).IsEqualTo(15);
    }

    [Test]
    public async Task Advance_WrapsLowBeyondMaxZ()
    {
        var space = FungeParser.Parse("A\fB");
        var next = space.Advance(new FungeVector(0, 0, 1), FungeVector.Low);
        await Assert.That(next).IsEqualTo(new FungeVector(0, 0, 0));
    }
}
