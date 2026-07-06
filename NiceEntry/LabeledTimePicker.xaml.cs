namespace NiceEntry;

public partial class LabeledTimePicker
{
    public LabeledTimePicker()
    {
        InitializeComponent();

        Element.SetVisualElementBinding();
        Element.SetBinding(TimePicker.TimeProperty, nameof(Time), BindingMode.TwoWay);
        // LabelBase sätter semantikbeskrivningen på View, som numera är Grid-wrappern —
        // vidarebefordra etiketten till själva pickern så skärmläsare annonserar den där.
        Element.SetBinding(SemanticProperties.DescriptionProperty, nameof(Label));
        Element.BindingContext = this;

        // SetVisualElementBinding kopplar IsEnabled till Element, inte till ✕:et —
        // lyssna själv så clear-knappen döljs när kontrollen disablas.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsEnabled))
                UpdateClearButtonView();
        };

        UpdateFontSizeView();
        UpdateClearButtonView();
    }

    public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(TimeSpan?), typeof(LabeledTimePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: TimeChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledTimePicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);
    public static readonly BindableProperty ShowClearButtonProperty = BindableProperty.Create(nameof(ShowClearButton), typeof(bool), typeof(LabeledTimePicker), false, propertyChanged: ShowClearButtonChanged);

    public TimeSpan? Time
    {
        get => (TimeSpan?)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    private static void TimeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateClearButtonView();
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateFontSizeView();
    private static void ShowClearButtonChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateClearButtonView();

    private void UpdateFontSizeView()
    {
        Element.FontSize = FontSize;
        ClearButton.FontSize = FontSize;
    }
    private void UpdateClearButtonView() => ClearButton.IsVisible = ShowClearButton && Time is not null && IsEnabled;

    private void OnClearTapped(object? sender, TappedEventArgs e) => Time = null;
}
