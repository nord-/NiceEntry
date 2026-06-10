using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Layouts;
using System.Windows.Input;

namespace NiceEntry;

public class NiceButton : Layout
{
    static NiceButton()
    {
        // Announce the control as a button to screen readers. The Layout root renders as a
        // plain container by default, so without this TalkBack/VoiceOver wouldn't expose a
        // button role. The accessible name comes from SemanticProperties.Description (set from
        // Text or SemanticDescription). Filtered on the NiceButton type so other layouts are
        // unaffected.
#if ANDROID
        LayoutHandler.Mapper.AppendToMapping("NiceButtonAccessibility", (handler, view) =>
        {
            if (view is NiceButton)
            {
                handler.PlatformView.ImportantForAccessibility = Android.Views.ImportantForAccessibility.Yes;
                handler.PlatformView.SetAccessibilityDelegate(new ButtonAccessibilityDelegate());
            }
        });
#elif IOS
        LayoutHandler.Mapper.AppendToMapping("NiceButtonAccessibility", (handler, view) =>
        {
            if (view is NiceButton)
            {
                handler.PlatformView.IsAccessibilityElement = true;
                handler.PlatformView.AccessibilityTraits = UIKit.UIAccessibilityTrait.Button;
            }
        });
#endif
    }

#if ANDROID
    private sealed class ButtonAccessibilityDelegate : Android.Views.View.AccessibilityDelegate
    {
        public override void OnInitializeAccessibilityNodeInfo(Android.Views.View host, Android.Views.Accessibility.AccessibilityNodeInfo info)
        {
            base.OnInitializeAccessibilityNodeInfo(host, info);
            info.ClassName = "android.widget.Button";
        }
    }
#endif

    /// <summary>Default font size for button text (not the field-tuned LabelBase value).</summary>
    public const double DefaultFontSize = 14.0;

    private static readonly Thickness DefaultContentPadding =
        DeviceInfo.Platform == DevicePlatform.iOS ? new Thickness(12, 12) : new Thickness(12, 10);

    private static readonly Color DefaultBackgroundLight = Color.FromArgb("#3B49DF");
    private static readonly Color DefaultBackgroundDark = Color.FromArgb("#5965F2");
    private static readonly Color DefaultForegroundLight = Colors.White;
    private static readonly Color DefaultForegroundDark = Colors.White;
    private static readonly Color DisabledBackgroundLight = Color.FromArgb("#E0E0E0");
    private static readonly Color DisabledBackgroundDark = Color.FromArgb("#3A3A3A");
    // #757575 on #E0E0E0 ≈ 3.5:1; #AAAAAA on #3A3A3A ≈ 5.0:1 — legible while still looking disabled.
    private static readonly Color DisabledForegroundLight = Color.FromArgb("#757575");
    private static readonly Color DisabledForegroundDark = Color.FromArgb("#AAAAAA");

    // Background-brush neutralization (see OnPropertyChanged): _userBackgroundBrush holds the
    // consumer's intended Background brush; the Layout's own Background is forced transparent.
    private bool _suppressBackground;
    private Brush? _userBackgroundBrush;
    private bool _commandEnabled = true;
    private bool _tapInFlight;

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

        NeutralizeInheritedVisualStates(_iconLabel);
        NeutralizeInheritedVisualStates(_textLabel);

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

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        _border.GestureRecognizers.Add(tap);

        // The layout root is the single accessibility element (see the handler mapper), and
        // screen-reader activation targets that element rather than the inner border. Recognize
        // taps on the root too so the button is operable via TalkBack/VoiceOver; the _tapInFlight
        // guard in OnTapped keeps a single sighted tap from firing the command twice.
        var rootTap = new TapGestureRecognizer();
        rootTap.Tapped += OnTapped;
        GestureRecognizers.Add(rootTap);

        Add(_border);
        RebuildContent();
        UpdateShapeView();
        UpdateBorderStrokeView();
        ApplyColors();
        UpdateShadowView();
        UpdateSemanticDescription();
    }

    // Stock MAUI app templates ship an implicit Label style whose CommonStates/Disabled visual
    // state sets a grey TextColor. When the button is disabled, IsEnabled propagates to the inner
    // labels and that inherited style attaches its visual states to them — and visual-state setters
    // outrank locally-set values, so they override the colors ApplyColors assigns (defeating both the
    // disabled-contrast handling and DisabledTextColor). Giving each label its own element-level
    // CommonStates group with empty states suppresses the inherited style's states (an element-level
    // group outranks a style-level one), leaving ApplyColors with the final say.
    private static void NeutralizeInheritedVisualStates(VisualElement label)
    {
        var common = new VisualStateGroup { Name = "CommonStates" };
        common.States.Add(new VisualState { Name = "Normal" });
        common.States.Add(new VisualState { Name = "Disabled" });
        VisualStateManager.SetVisualStateGroups(label, new VisualStateGroupList { common });
    }

    /// <summary>True when the button must be measured square (Circle shape).</summary>
    internal bool ForceSquare => ButtonShape == ButtonShape.Circle;

    protected override ILayoutManager CreateLayoutManager() => new NiceButtonLayoutManager(this);

    // --- Content & text-style bindable properties ---

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NiceButton), string.Empty, propertyChanged: TextChanged);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(MaterialIcon?), typeof(NiceButton), null, propertyChanged: IconChanged);

    public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
        nameof(SemanticDescription), typeof(string), typeof(NiceButton), null, propertyChanged: SemanticDescriptionChanged);

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

    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow), typeof(bool), typeof(NiceButton), false, propertyChanged: ShadowChanged);

    public static readonly BindableProperty CustomShadowProperty = BindableProperty.Create(
        nameof(CustomShadow), typeof(Shadow), typeof(NiceButton), null, propertyChanged: ShadowChanged);

    public static readonly BindableProperty DisabledBackgroundColorProperty = BindableProperty.Create(
        nameof(DisabledBackgroundColor), typeof(Color), typeof(NiceButton), null, propertyChanged: ColorChanged);

    public static readonly BindableProperty DisabledTextColorProperty = BindableProperty.Create(
        nameof(DisabledTextColor), typeof(Color), typeof(NiceButton), null, propertyChanged: ColorChanged);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(NiceButton), null, propertyChanged: CommandChanged);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(NiceButton), null, propertyChanged: CommandParameterChanged);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public MaterialIcon? Icon { get => (MaterialIcon?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>
    /// Accessible name announced by screen readers. Defaults to <see cref="Text"/> when not set;
    /// set this explicitly for icon-only buttons, which otherwise have no accessible name.
    /// </summary>
    public string SemanticDescription { get => (string)GetValue(SemanticDescriptionProperty); set => SetValue(SemanticDescriptionProperty, value); }
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
    public bool HasShadow { get => (bool)GetValue(HasShadowProperty); set => SetValue(HasShadowProperty, value); }

    /// <summary>
    /// A fully custom shadow applied to the button. Takes precedence over <see cref="HasShadow"/>.
    /// Note: set <see cref="CustomShadow"/>, not the inherited <c>VisualElement.Shadow</c> — the
    /// Layout root is transparent, so a shadow set on it would not be visible; only the inner
    /// border (set via this property) renders a shadow.
    /// </summary>
    public Shadow CustomShadow { get => (Shadow)GetValue(CustomShadowProperty); set => SetValue(CustomShadowProperty, value); }

    public Color DisabledBackgroundColor { get => (Color)GetValue(DisabledBackgroundColorProperty); set => SetValue(DisabledBackgroundColorProperty, value); }
    public Color DisabledTextColor { get => (Color)GetValue(DisabledTextColorProperty); set => SetValue(DisabledTextColorProperty, value); }

    public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private static void CommandChanged(BindableObject b, object oldValue, object newValue)
    {
        var btn = (NiceButton)b;
        if (oldValue is ICommand oldCmd) oldCmd.CanExecuteChanged -= btn.OnCanExecuteChanged;
        if (newValue is ICommand newCmd) newCmd.CanExecuteChanged += btn.OnCanExecuteChanged;
        btn.RefreshEnabledFromCommand();
    }

    private static void CommandParameterChanged(BindableObject b, object o, object n)
        => ((NiceButton)b).RefreshEnabledFromCommand();

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Command is ICommand cmd)
        {
            cmd.CanExecuteChanged -= OnCanExecuteChanged;
            if (Handler is not null)
            {
                cmd.CanExecuteChanged += OnCanExecuteChanged;
                RefreshEnabledFromCommand();
            }
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e) => RefreshEnabledFromCommand();

    private void RefreshEnabledFromCommand()
    {
        var enabled = Command?.CanExecute(CommandParameter) ?? true;
        if (_commandEnabled == enabled) return;
        _commandEnabled = enabled;
        ApplyColors();
    }

    private static void TextChanged(BindableObject b, object o, object n)
    {
        var btn = (NiceButton)b;
        if (string.IsNullOrEmpty(o as string) == string.IsNullOrEmpty(n as string))
        {
            btn._textLabel.Text = (string?)n ?? string.Empty;
            btn.InvalidateMeasure();
        }
        else
            btn.RebuildContent();

        btn.UpdateSemanticDescription();
    }

    private static void SemanticDescriptionChanged(BindableObject b, object o, object n)
        => ((NiceButton)b).UpdateSemanticDescription();

    // Prefer the explicit SemanticDescription; fall back to Text so text buttons are announced
    // without extra configuration. Icon-only buttons have no Text, so SemanticDescription is the
    // only way to give them an accessible name.
    private void UpdateSemanticDescription()
        => SemanticProperties.SetDescription(this, string.IsNullOrEmpty(SemanticDescription) ? Text : SemanticDescription);

    private static void IconChanged(BindableObject b, object o, object n)
    {
        var btn = (NiceButton)b;
        if ((o is MaterialIcon) == (n is MaterialIcon))
        {
            btn._iconLabel.Text = n is MaterialIcon icon ? char.ConvertFromUtf32((int)icon) : null;
            btn.InvalidateMeasure();
        }
        else
            btn.RebuildContent();
    }
    private static void LayoutAffectingChanged(BindableObject b, object o, object n) => ((NiceButton)b).RebuildContent();
    private static void ContentPaddingChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateContentPaddingView();
    private static void FontSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontSizeView();
    private static void FontFamilyChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontFamilyView();
    private static void FontAttributesChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontAttributesView();
    private static void IconSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconSizeView();
    private static void ShapeChanged(BindableObject b, object o, object n)
    {
        var btn = (NiceButton)b;
        btn.UpdateShapeView();
        btn.InvalidateMeasure();
    }
    private static void ColorChanged(BindableObject b, object o, object n) => ((NiceButton)b).ApplyColors();
    private static void BorderStrokeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateBorderStrokeView();
    private static void ShadowChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateShadowView();

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
    }

    private void ApplyColors()
    {
        // Guard: Background = Brush.Transparent in the constructor fires OnPropertyChanged
        // synchronously before _border is assigned.
        if (_border is null) return;

        if (!IsEnabled || !_commandEnabled)
        {
            _border.ClearValue(BackgroundProperty);
            if (DisabledBackgroundColor is not null)
                _border.BackgroundColor = DisabledBackgroundColor;
            else
                _border.SetAppThemeColor(BackgroundColorProperty, DisabledBackgroundLight, DisabledBackgroundDark);

            if (DisabledTextColor is not null)
                SetForeground(DisabledTextColor, DisabledTextColor, themed: false);
            else
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
            // Removes active SetAppThemeColor binding before direct assignment; without this
            // the binding overwrites the explicit color on the next OS theme change.
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

    private void UpdateShadowView()
    {
        if (CustomShadow is not null)
            _border.Shadow = CustomShadow;
        else if (HasShadow)
            _border.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.3f, Radius = 8, Offset = new Point(0, 2) };
        else
            _border.ClearValue(Border.ShadowProperty);
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled || !_commandEnabled) return;
        if (_tapInFlight) return;
        _tapInFlight = true;
        try
        {
            var cmd = Command;
            var param = CommandParameter;
            if (cmd is null || !cmd.CanExecute(param)) return;

            await _contentHost.FadeToAsync(0.3, 100);
            await _contentHost.FadeToAsync(1, 100);

            if (cmd.CanExecute(param))
                cmd.Execute(param);
        }
        finally
        {
            _tapInFlight = false;
        }
    }

    // BackgroundColor and Background are inherited VisualElement properties and cannot
    // be re-declared with a custom propertyChanged callback; OnPropertyChanged is the
    // only way to intercept them.
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

        _iconLabel.Text = Icon.HasValue ? char.ConvertFromUtf32((int)Icon.Value) : null;
        _textLabel.Text = Text;

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
