using NiceEntry.Drawing;

namespace NiceEntry;

public partial class LabelBase
{
    public static readonly double DefaultFontSize = DeviceInfo.Platform == DevicePlatform.iOS ? 12.0 : 16.0;

    // Mirrors the left value of LabelContainer.Margin in LabelBase.xaml — keep in lockstep.
    private const double LabelContainerLeftMargin = 14;
    // Horizontal padding between the label text and the surrounding stroke gap;
    // shared by UpdateLabelMaxWidth and UpdateNotchBounds.
    private const double NotchPadding = 4;

    private static readonly Color BorderStrokeLight = Color.FromArgb("#212121");
    private static readonly Color BorderStrokeDark = Color.FromArgb("#E1E1E1");

    private readonly Grid _contentGrid;
    private readonly Label _unitLabel;

    public LabelBase()
    {
        InitializeComponent();

        _unitLabel = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            IsVisible = false,
            InputTransparent = true,
            Opacity = 0.6,
            Margin = new Thickness(8, 0, 0, 0)
        };

        _contentGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        _contentGrid.Add(_unitLabel, 1, 0);

        UpdateContentPaddingView();
        UpdateUnitFontSizeView();
        ApplyDefaultStrokeColor();

        LabelContainer.SizeChanged += (_, _) => UpdateNotchBounds();
        SizeChanged += (_, _) => UpdateLabelMaxWidth();
    }

    // Existing properties
    public static readonly BindableProperty ViewProperty = BindableProperty.Create("View",
        typeof(View), typeof(LabelBase), defaultValue: null, defaultBindingMode: BindingMode.OneWay,
        propertyChanged: ElementChanged);

    public static readonly BindableProperty IsRequiredProperty = BindableProperty.Create(
        nameof(IsRequired), typeof(bool), typeof(LabelBase), defaultValue: false,
        propertyChanged: IsRequiredChanged);

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(LabelBase), propertyChanged: LabelChanged);

    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(
        nameof(Error), typeof(IReadOnlyCollection<string>), typeof(LabelBase),
        propertyChanged: ErrorChanged);

    // New properties
    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(LabelBase),
        defaultValue: DeviceInfo.Platform == DevicePlatform.iOS
            ? new Thickness(12, 12)
            : new Thickness(12, 10),
        propertyChanged: ContentPaddingChanged);

    public static readonly BindableProperty UnitProperty = BindableProperty.Create(
        nameof(Unit), typeof(string), typeof(LabelBase), propertyChanged: UnitChanged);

    public static readonly BindableProperty UnitFontFamilyProperty = BindableProperty.Create(
        nameof(UnitFontFamily), typeof(string), typeof(LabelBase),
        propertyChanged: UnitFontFamilyChanged);

    public static readonly BindableProperty UnitFontSizeProperty = BindableProperty.Create(
        nameof(UnitFontSize), typeof(double), typeof(LabelBase), defaultValue: DefaultFontSize,
        propertyChanged: UnitFontSizeChanged);

    public static readonly BindableProperty UnitColorProperty = BindableProperty.Create(
        nameof(UnitColor), typeof(Color), typeof(LabelBase),
        propertyChanged: UnitColorChanged);

    public static readonly BindableProperty ExampleProperty = BindableProperty.Create(
        nameof(Example), typeof(string), typeof(LabelBase), propertyChanged: ExampleChanged);

    // CLR properties
    public View View { get => (View)GetValue(ViewProperty); set => SetValue(ViewProperty, value); }
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public IReadOnlyCollection<string> Error { get => (IReadOnlyCollection<string>)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string UnitFontFamily { get => (string)GetValue(UnitFontFamilyProperty); set => SetValue(UnitFontFamilyProperty, value); }
    public double UnitFontSize { get => (double)GetValue(UnitFontSizeProperty); set => SetValue(UnitFontSizeProperty, value); }
    public Color UnitColor { get => (Color)GetValue(UnitColorProperty); set => SetValue(UnitColorProperty, value); }
    public string Example { get => (string)GetValue(ExampleProperty); set => SetValue(ExampleProperty, value); }

    // Change callbacks
    private static void ElementChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateElementView();
    private static void IsRequiredChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateIsRequiredView();
    private static void LabelChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateLabelView();
    private static void ErrorChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateErrorView();
    private static void ContentPaddingChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateContentPaddingView();
    private static void UnitChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateUnitView();
    private static void UnitFontFamilyChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateUnitFontFamilyView();
    private static void UnitFontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateUnitFontSizeView();
    private static void UnitColorChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateUnitColorView();
    private static void ExampleChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateExampleView();

    // Update methods
    private void UpdateElementView()
    {
        if (View is null)
        {
            BorderLabel.Content = null;
            return;
        }

        if (_contentGrid.Children.Count > 1)
            _contentGrid.RemoveAt(0);

        View.VerticalOptions = LayoutOptions.Center;
        _contentGrid.Insert(0, View);
        Grid.SetColumn((BindableObject)View, 0);
        BorderLabel.Content = _contentGrid;
        UpdateIsRequiredView();
    }

    private void UpdateIsRequiredView()
    {
        RequiredLabel.IsVisible = IsRequired;
        UpdateLabelMaxWidth();
    }

    private void UpdateLabelMaxWidth()
    {
        if (Width <= 0) return;

        var rightSafety = BorderLabel.CornerRadius + NotchPadding + 1;

        LabelContainer.MaximumWidthRequest = Math.Max(0, Width - LabelContainerLeftMargin - rightSafety);
    }

    private void UpdateLabelView()
    {
        LabelLabel.Text = Label;
        LabelLabel.IsVisible = !string.IsNullOrEmpty(Label);
        UpdateNotchBounds();
    }

    private void UpdateNotchBounds()
    {
        if (LabelContainer.Width <= 0 || !LabelLabel.IsVisible)
        {
            BorderLabel.NotchStart = 0;
            BorderLabel.NotchEnd = 0;
            return;
        }
        // The label sits over the top edge of the border. Translate its X-range
        // into BorderLabel-local coordinates and pad on each side so the text
        // doesn't kiss the stroke ends.
        BorderLabel.NotchStart = LabelContainer.X - NotchPadding;
        BorderLabel.NotchEnd = LabelContainer.X + LabelContainer.Width + NotchPadding;
    }

    private void UpdateErrorView()
    {
        var count = Error?.Count ?? 0;
        ErrorLabel.Text = count > 0 ? string.Join(',', Error!) : string.Empty;
        ErrorLabel.IsVisible = count > 0;
        ChangeBorderColor();
    }

    private void UpdateContentPaddingView()
    {
        BorderLabel.ContentPadding = ContentPadding;
    }

    private void UpdateUnitView()
    {
        _unitLabel.Text = Unit;
        _unitLabel.IsVisible = !string.IsNullOrEmpty(Unit);
    }

    private void UpdateUnitFontFamilyView() => _unitLabel.FontFamily = UnitFontFamily;
    private void UpdateUnitFontSizeView() => _unitLabel.FontSize = UnitFontSize;
    private void UpdateUnitColorView() => _unitLabel.TextColor = UnitColor;

    private void UpdateExampleView()
    {
        ExampleLabel.Text = Example;
        ExampleLabel.IsVisible = !string.IsNullOrEmpty(Example);
    }

    private void ChangeBorderColor()
    {
        if (Error is not null && Error.Count > 0)
        {
            BorderLabel.StrokeColor = Colors.Red;
        }
        else
        {
            // ClearValue on a property previously set to a local value drops any
            // existing AppThemeBinding, so re-establish the theme binding to keep
            // theme toggles working after an error-clear cycle.
            ApplyDefaultStrokeColor();
        }
    }

    private void ApplyDefaultStrokeColor() =>
        BorderLabel.SetAppThemeColor(NotchedBorder.StrokeColorProperty,
            BorderStrokeLight, BorderStrokeDark);
}
