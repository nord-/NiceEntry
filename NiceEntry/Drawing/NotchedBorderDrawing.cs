using Microsoft.Maui.Graphics;

namespace NiceEntry.Drawing;

internal static class NotchedBorderDrawing
{
    /// <summary>
    /// Builds a rounded-rectangle stroke path with a gap on the top edge
    /// between <paramref name="notchStart"/> and <paramref name="notchEnd"/>
    /// (both in canvas-local logical units). If the notch span is zero or
    /// would land inside the corner arcs, returns a plain rounded rectangle.
    /// </summary>
    public static PathF BuildPath(
        float width,
        float height,
        float cornerRadius,
        float strokeThickness,
        float notchStart,
        float notchEnd)
    {
        var inset = strokeThickness / 2f;
        var left = inset;
        var top = inset;
        var right = width - inset;
        var bottom = height - inset;
        var r = Math.Max(0, cornerRadius - inset);

        var path = new PathF();

        var notchActive = notchEnd > notchStart
            && notchStart > left + r
            && notchEnd < right - r;

        path.MoveTo(left, top + r);
        path.QuadTo(left, top, left + r, top);

        if (notchActive)
        {
            path.LineTo(notchStart, top);
            path.MoveTo(notchEnd, top);
            path.LineTo(right - r, top);
        }
        else
        {
            path.LineTo(right - r, top);
        }

        path.QuadTo(right, top, right, top + r);
        path.LineTo(right, bottom - r);
        path.QuadTo(right, bottom, right - r, bottom);
        path.LineTo(left + r, bottom);
        path.QuadTo(left, bottom, left, bottom - r);
        path.LineTo(left, top + r);

        return path;
    }
}
