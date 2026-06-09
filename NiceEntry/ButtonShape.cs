namespace NiceEntry;

/// <summary>Visual shape of a <see cref="NiceButton"/>'s border.</summary>
public enum ButtonShape
{
    /// <summary>Straight corners. <c>CornerRadius</c> is ignored.</summary>
    Rectangle,

    /// <summary>Rounded corners controlled by <c>CornerRadius</c>.</summary>
    Rounded,

    /// <summary>Ellipse; the button is measured square so it renders a perfect circle. <c>CornerRadius</c> is ignored.</summary>
    Circle
}
