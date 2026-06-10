using Microsoft.Maui.Graphics;
using NiceEntry.Drawing;
using Xunit;

namespace NiceEntry.Tests;

public class NotchedBorderDrawingTests
{
    // A plain rounded rectangle is drawn as a single continuous subpath, so it
    // contains exactly one Move operation. An active notch introduces a gap on
    // the top edge via a second Move, so two Move operations means the notch is
    // rendered.
    private static int CountMoves(PathF path)
    {
        var moves = 0;
        for (var i = 0; i < path.OperationCount; i++)
        {
            if (path.GetSegmentType(i) == PathOperation.Move)
                moves++;
        }
        return moves;
    }

    [Fact]
    public void BuildPath_NotchWithinTopEdge_RendersGap()
    {
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 8, strokeThickness: 1,
            notchStart: 20, notchEnd: 80);

        Assert.Equal(2, CountMoves(path));
    }

    [Fact]
    public void BuildPath_ZeroSpan_RendersPlainRectangle()
    {
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 8, strokeThickness: 1,
            notchStart: 0, notchEnd: 0);

        Assert.Equal(1, CountMoves(path));
    }

    [Fact]
    public void BuildPath_InvertedSpan_RendersPlainRectangle()
    {
        // notchEnd <= notchStart must not activate the notch.
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 8, strokeThickness: 1,
            notchStart: 80, notchEnd: 20);

        Assert.Equal(1, CountMoves(path));
    }

    [Fact]
    public void BuildPath_NotchStartInsideLeftCornerArc_RendersPlainRectangle()
    {
        // notchStart must clear left + cornerRadius; here it lands inside the arc.
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 20, strokeThickness: 1,
            notchStart: 5, notchEnd: 120);

        Assert.Equal(1, CountMoves(path));
    }

    [Fact]
    public void BuildPath_NotchEndInsideRightCornerArc_RendersPlainRectangle()
    {
        // notchEnd must stay left of right - cornerRadius; here it overruns it.
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 20, strokeThickness: 1,
            notchStart: 40, notchEnd: 195);

        Assert.Equal(1, CountMoves(path));
    }

    [Fact]
    public void BuildPath_ZeroWidth_DoesNotThrowAndRendersPlainRectangle()
    {
        var path = NotchedBorderDrawing.BuildPath(
            width: 0, height: 56, cornerRadius: 8, strokeThickness: 1,
            notchStart: 20, notchEnd: 80);

        // No valid top edge to notch into, so it degrades to a plain (degenerate) path.
        Assert.Equal(1, CountMoves(path));
    }

    [Fact]
    public void BuildPath_CornerRadiusSmallerThanInset_ClampsRadiusWithoutThrowing()
    {
        // strokeThickness/2 exceeds cornerRadius, so the effective radius clamps to 0.
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 1, strokeThickness: 10,
            notchStart: 20, notchEnd: 80);

        Assert.True(path.OperationCount > 0);
        Assert.Equal(2, CountMoves(path));
    }

    [Theory]
    [InlineData(20, 80, 2)]   // valid notch -> gap
    [InlineData(0, 0, 1)]     // zero span -> plain
    [InlineData(80, 80, 1)]   // equal endpoints -> plain
    public void BuildPath_MoveCount_MatchesNotchActivation(float start, float end, int expectedMoves)
    {
        var path = NotchedBorderDrawing.BuildPath(
            width: 200, height: 56, cornerRadius: 8, strokeThickness: 1,
            notchStart: start, notchEnd: end);

        Assert.Equal(expectedMoves, CountMoves(path));
    }
}
