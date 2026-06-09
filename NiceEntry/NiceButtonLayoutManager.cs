using Microsoft.Maui.Layouts;

namespace NiceEntry;

/// <summary>
/// Lays out the single child (the Border) of a <see cref="NiceButton"/>. When the button
/// is in Circle shape it returns a square desired size so the ellipse renders as a perfect
/// circle. This avoids both SizeChanged-driven resizing (layout loops on Android) and
/// ContentView.MeasureOverride (unreliable per dotnet/maui#19471).
/// </summary>
internal sealed class NiceButtonLayoutManager : ILayoutManager
{
    private readonly NiceButton _button;

    public NiceButtonLayoutManager(NiceButton button) => _button = button;

    public Size Measure(double widthConstraint, double heightConstraint)
    {
        var desired = new Size();
        foreach (var child in _button)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var size = child.Measure(widthConstraint, heightConstraint);
            desired = new Size(Math.Max(desired.Width, size.Width), Math.Max(desired.Height, size.Height));
        }

        if (_button.ForceSquare)
        {
            var side = Math.Max(desired.Width, desired.Height);
            desired = new Size(side, side);
        }

        return desired;
    }

    public Size ArrangeChildren(Rect bounds)
    {
        foreach (var child in _button)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            child.Arrange(bounds);
        }

        return bounds.Size;
    }
}
