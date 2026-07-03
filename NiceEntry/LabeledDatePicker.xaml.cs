namespace NiceEntry;

public partial class LabeledDatePicker
{
    public LabeledDatePicker()
    {
        InitializeComponent();

        Element.SetVisualElementBinding();
        Element.SetBinding(DatePicker.DateProperty, nameof(Date), BindingMode.TwoWay);
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

    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime?), typeof(LabeledDatePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: DateChanged);
    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(nameof(MinimumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MinValue, propertyChanged: MinimumDateChanged);
    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(nameof(MaximumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MaxValue, propertyChanged: MaximumDateChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledDatePicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);
    public static readonly BindableProperty ShowClearButtonProperty = BindableProperty.Create(nameof(ShowClearButton), typeof(bool), typeof(LabeledDatePicker), false, propertyChanged: ShowClearButtonChanged);

    public DateTime? Date
    {
        get => (DateTime?)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public DateTime MinimumDate
    {
        get => (DateTime)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public DateTime MaximumDate
    {
        get => (DateTime)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
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

    private static void DateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateClearButtonView();
    private static void MinimumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMinimumDateView();
    private static void MaximumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMaximumDateView();
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateFontSizeView();
    private static void ShowClearButtonChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateClearButtonView();

    private void UpdateMinimumDateView() => Element.MinimumDate = MinimumDate;
    private void UpdateMaximumDateView() => Element.MaximumDate = MaximumDate;
    private void UpdateFontSizeView() => Element.FontSize = FontSize;
    private void UpdateClearButtonView() => ClearButton.IsVisible = ShowClearButton && Date is not null && IsEnabled;

    private void OnClearTapped(object? sender, TappedEventArgs e) => Date = null;
}
