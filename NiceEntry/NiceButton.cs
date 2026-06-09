using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace NiceEntry;

public class NiceButton : Layout
{
    /// <summary>Default font size for button text (not the field-tuned LabelBase value).</summary>
    public static readonly double DefaultFontSize = 14.0;

    private static readonly Thickness DefaultContentPadding =
        DeviceInfo.Platform == DevicePlatform.iOS ? new Thickness(12, 12) : new Thickness(12, 10);

    private readonly Border _border;
    private readonly Grid _contentHost;
    private readonly Label _iconLabel;
    private readonly Label _textLabel;

    public NiceButton()
    {
        // The Layout root must never paint a fill: a non-transparent Background BRUSH wins
        // over BackgroundColor in rendering, so a transparent brush keeps the root invisible
        // while the inner Border (correctly clipped to the shape) carries all fill. Without
        // this, a set BackgroundColor would paint a rectangle behind rounded/circle corners.
        Background = Brush.Transparent;

        _iconLabel = new Label
        {
            FontFamily = AppHostBuilderExtensions.MaterialDesignIconsFontFamily,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = IconSize
        };

        _textLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            FontSize = FontSize
        };

        _contentHost = new Grid
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _border = new Border
        {
            StrokeThickness = 0,
            Padding = ContentPadding,
            Content = _contentHost,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        Add(_border);
        RebuildContent();
        UpdateShapeView();
    }

    /// <summary>True when the button must be measured square (Circle shape).</summary>
    internal bool ForceSquare => ButtonShape == ButtonShape.Circle;

    protected override ILayoutManager CreateLayoutManager() => new NiceButtonLayoutManager(this);

    // --- Content & text-style bindable properties ---

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NiceButton), string.Empty, propertyChanged: TextChanged);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(MaterialIcon?), typeof(NiceButton), null, propertyChanged: IconChanged);

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(ButtonContentOrientation), typeof(NiceButton),
        ButtonContentOrientation.Horizontal, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty IconPlacementProperty = BindableProperty.Create(
        nameof(IconPlacement), typeof(IconPlacement), typeof(NiceButton),
        IconPlacement.Start, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(NiceButton), 6.0, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(NiceButton), DefaultContentPadding,
        propertyChanged: ContentPaddingChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(NiceButton), DefaultFontSize, propertyChanged: FontSizeChanged);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(NiceButton), null, propertyChanged: FontFamilyChanged);

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(NiceButton),
        FontAttributes.None, propertyChanged: FontAttributesChanged);

    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize), typeof(double), typeof(NiceButton), 20.0, propertyChanged: IconSizeChanged);

    public static readonly BindableProperty ButtonShapeProperty = BindableProperty.Create(
        nameof(ButtonShape), typeof(ButtonShape), typeof(NiceButton),
        ButtonShape.Rounded, propertyChanged: ShapeChanged);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(NiceButton), 8.0, propertyChanged: ShapeChanged);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public MaterialIcon? Icon { get => (MaterialIcon?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public ButtonContentOrientation Orientation { get => (ButtonContentOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public IconPlacement IconPlacement { get => (IconPlacement)GetValue(IconPlacementProperty); set => SetValue(IconPlacementProperty, value); }
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public string FontFamily { get => (string)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public ButtonShape ButtonShape { get => (ButtonShape)GetValue(ButtonShapeProperty); set => SetValue(ButtonShapeProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    private static void TextChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateTextView();
    private static void IconChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconView();
    private static void LayoutAffectingChanged(BindableObject b, object o, object n) => ((NiceButton)b).RebuildContent();
    private static void ContentPaddingChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateContentPaddingView();
    private static void FontSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontSizeView();
    private static void FontFamilyChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontFamilyView();
    private static void FontAttributesChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontAttributesView();
    private static void IconSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconSizeView();
    private static void ShapeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateShapeView();

    private void UpdateTextView()
    {
        _textLabel.Text = Text;
        RebuildContent();
    }

    private void UpdateIconView()
    {
        _iconLabel.Text = Icon.HasValue ? char.ConvertFromUtf32((int)Icon.Value) : null;
        RebuildContent();
    }

    private void UpdateContentPaddingView() => _border.Padding = ContentPadding;
    private void UpdateFontSizeView() => _textLabel.FontSize = FontSize;
    private void UpdateFontFamilyView() => _textLabel.FontFamily = FontFamily;
    private void UpdateFontAttributesView() => _textLabel.FontAttributes = FontAttributes;
    private void UpdateIconSizeView() => _iconLabel.FontSize = IconSize;

    private void UpdateShapeView()
    {
        _border.StrokeShape = ButtonShape switch
        {
            ButtonShape.Rectangle => new Rectangle(),
            ButtonShape.Circle => new Ellipse(),
            _ => new RoundRectangle { CornerRadius = new Microsoft.Maui.CornerRadius(CornerRadius) }
        };

        InvalidateMeasure();
    }

    /// <summary>
    /// Rebuilds the icon/text container based on what is set (icon-only, label-only, or both)
    /// and the chosen orientation/placement.
    /// </summary>
    private void RebuildContent()
    {
        _contentHost.Children.Clear();
        _contentHost.RowDefinitions.Clear();
        _contentHost.ColumnDefinitions.Clear();

        var hasIcon = Icon.HasValue;
        var hasText = !string.IsNullOrEmpty(Text);

        _iconLabel.IsVisible = hasIcon;
        _textLabel.IsVisible = hasText;

        if (!hasIcon && !hasText) return;

        if (hasIcon && hasText)
        {
            var iconFirst = IconPlacement == IconPlacement.Start;
            var first = iconFirst ? (View)_iconLabel : _textLabel;
            var second = iconFirst ? (View)_textLabel : _iconLabel;

            if (Orientation == ButtonContentOrientation.Horizontal)
            {
                _contentHost.ColumnSpacing = Spacing;
                _contentHost.RowSpacing = 0;
                _contentHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _contentHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _contentHost.Add(first, 0, 0);
                _contentHost.Add(second, 1, 0);
            }
            else
            {
                _contentHost.RowSpacing = Spacing;
                _contentHost.ColumnSpacing = 0;
                _contentHost.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _contentHost.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _contentHost.Add(first, 0, 0);
                _contentHost.Add(second, 0, 1);
            }
        }
        else
        {
            var only = hasIcon ? (View)_iconLabel : _textLabel;
            _contentHost.Add(only, 0, 0);
        }

        InvalidateMeasure();
    }
}
