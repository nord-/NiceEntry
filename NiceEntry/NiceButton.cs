using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace NiceEntry;

public class NiceButton : Layout
{
    /// <summary>Default font size for button text (not the field-tuned LabelBase value).</summary>
    public static readonly double DefaultFontSize = 14.0;

    private static readonly Thickness DefaultContentPadding =
        DeviceInfo.Platform == DevicePlatform.iOS ? new Thickness(12, 12) : new Thickness(12, 10);

    private static readonly Color DefaultBackgroundLight = Color.FromArgb("#3B49DF");
    private static readonly Color DefaultBackgroundDark = Color.FromArgb("#5965F2");
    private static readonly Color DefaultForegroundLight = Colors.White;
    private static readonly Color DefaultForegroundDark = Colors.White;
    private static readonly Color DisabledBackgroundLight = Color.FromArgb("#E0E0E0");
    private static readonly Color DisabledBackgroundDark = Color.FromArgb("#3A3A3A");
    private static readonly Color DisabledForegroundLight = Color.FromArgb("#9E9E9E");
    private static readonly Color DisabledForegroundDark = Color.FromArgb("#6E6E6E");

    // Background-brush neutralization (see OnPropertyChanged): _userBackgroundBrush holds the
    // consumer's intended Background brush; the Layout's own Background is forced transparent.
    private bool _suppressBackground;
    private Brush? _userBackgroundBrush;

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
        UpdateBorderStrokeView();
        ApplyColors();
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

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(NiceButton), null, propertyChanged: ColorChanged);

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(NiceButton), null, propertyChanged: BorderStrokeChanged);

    public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
        nameof(BorderWidth), typeof(double), typeof(NiceButton), 0.0, propertyChanged: BorderStrokeChanged);

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
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color BorderColor { get => (Color)GetValue(BorderColorProperty); set => SetValue(BorderColorProperty, value); }
    public double BorderWidth { get => (double)GetValue(BorderWidthProperty); set => SetValue(BorderWidthProperty, value); }

    private static void TextChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateTextView();
    private static void IconChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconView();
    private static void LayoutAffectingChanged(BindableObject b, object o, object n) => ((NiceButton)b).RebuildContent();
    private static void ContentPaddingChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateContentPaddingView();
    private static void FontSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontSizeView();
    private static void FontFamilyChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontFamilyView();
    private static void FontAttributesChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontAttributesView();
    private static void IconSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconSizeView();
    private static void ShapeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateShapeView();
    private static void ColorChanged(BindableObject b, object o, object n) => ((NiceButton)b).ApplyColors();
    private static void BorderStrokeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateBorderStrokeView();

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

    private void ApplyColors()
    {
        // OnPropertyChanged can fire (e.g. for Background) before the constructor has built
        // the inner views; bail out until they exist.
        if (_border is null) return;

        if (!IsEnabled)
        {
            _border.ClearValue(BackgroundProperty);
            _border.SetAppThemeColor(BackgroundColorProperty, DisabledBackgroundLight, DisabledBackgroundDark);
            SetForeground(DisabledForegroundLight, DisabledForegroundDark, themed: true);
            return;
        }

        if (_userBackgroundBrush is not null)
        {
            _border.Background = _userBackgroundBrush;
        }
        else if (BackgroundColor is not null)
        {
            _border.ClearValue(BackgroundProperty);
            _border.ClearValue(BackgroundColorProperty);
            _border.BackgroundColor = BackgroundColor;
        }
        else
        {
            _border.ClearValue(BackgroundProperty);
            _border.SetAppThemeColor(BackgroundColorProperty, DefaultBackgroundLight, DefaultBackgroundDark);
        }

        if (TextColor is not null)
            SetForeground(TextColor, TextColor, themed: false);
        else
            SetForeground(DefaultForegroundLight, DefaultForegroundDark, themed: true);
    }

    private void SetForeground(Color light, Color dark, bool themed)
    {
        if (themed)
        {
            _iconLabel.SetAppThemeColor(Label.TextColorProperty, light, dark);
            _textLabel.SetAppThemeColor(Label.TextColorProperty, light, dark);
        }
        else
        {
            _iconLabel.ClearValue(Label.TextColorProperty);
            _textLabel.ClearValue(Label.TextColorProperty);
            _iconLabel.TextColor = light;
            _textLabel.TextColor = light;
        }
    }

    private void UpdateBorderStrokeView()
    {
        _border.Stroke = BorderColor is null ? null : new SolidColorBrush(BorderColor);
        _border.StrokeThickness = BorderWidth;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (_suppressBackground) return;

        if (propertyName == BackgroundColorProperty.PropertyName)
        {
            if (BackgroundColor is not null)
                _userBackgroundBrush = null;
            ApplyColors();
        }
        else if (propertyName == IsEnabledProperty.PropertyName)
        {
            ApplyColors();
        }
        else if (propertyName == BackgroundProperty.PropertyName)
        {
            // Consumer set a Background brush (e.g. gradient). Capture it for the inner Border,
            // then force the Layout root back to transparent so it never paints a rectangle.
            _userBackgroundBrush = ReferenceEquals(Background, Brush.Transparent) ? null : Background;
            _suppressBackground = true;
            Background = Brush.Transparent;
            _suppressBackground = false;
            ApplyColors();
        }
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
