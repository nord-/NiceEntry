using Microsoft.Maui.Layouts;

namespace NiceEntry;

/// <summary>
/// Lays out the single child (the Border) of a <see cref="NiceButton"/>. When the button
/// is in Circle shape it measures a square desired size AND inscribes a centered square at
/// arrange time, so the ellipse renders as a perfect circle even when a parent stretches the
/// control to non-square bounds (e.g. the default <c>HorizontalOptions.Fill</c> in a stack).
/// This avoids both SizeChanged-driven resizing (layout loops on Android) and
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

            if (_button.HasTextContent)
            {
                // The icon/text grid uses Auto columns and stays centered, so icon and text hug
                // as a unit even when a parent stretches the button — but an Auto column measures
                // the label at infinite width, so wrap/ellipsis would never engage on its own.
                // Cap the label at the width left over after the non-text parts (border padding,
                // icon, spacing); the label then wraps (WordWrap) or ellipsizes (TailTruncation)
                // per EffectiveLineBreakMode instead of overflowing the border. The overhead is
                // additive, so it is valid even when this pass measured with a stale cap.
                var textLabel = _button.TextLabel;
                var cap = double.IsPositiveInfinity(widthConstraint)
                    ? double.PositiveInfinity
                    : Math.Max(0, widthConstraint - (size.Width - textLabel.DesiredSize.Width));

                // Only touch the property on real changes: assignment invalidates measure, and a
                // steady-state no-op write would otherwise re-trigger layout forever.
                if (Math.Abs(textLabel.MaximumWidthRequest - cap) > 0.5)
                {
                    textLabel.MaximumWidthRequest = cap;
                    size = child.Measure(widthConstraint, heightConstraint);
                }
            }

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

            if (_button.ForceSquare)
            {
                // Measure squares the desired size, but a parent that stretches this control
                // (e.g. a VerticalStackLayout with the default HorizontalOptions=Fill) hands us
                // non-square bounds. Inscribe a centered square so the ellipse renders as a
                // perfect circle regardless of how we were stretched.
                var side = Math.Min(bounds.Width, bounds.Height);
                var x = bounds.X + (bounds.Width - side) / 2;
                var y = bounds.Y + (bounds.Height - side) / 2;
                child.Arrange(new Rect(x, y, side, side));
            }
            else
            {
                child.Arrange(bounds);
            }
        }

        return bounds.Size;
    }
}
