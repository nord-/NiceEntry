namespace NiceEntry;

public partial class LabeledTimePicker
{
    public LabeledTimePicker()
    {
        InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(TimePicker.TimeProperty, nameof(Time), BindingMode.TwoWay);
        Element.BindingContext = this;

        UpdateFontSizeView();
    }
    
    public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(TimeSpan), typeof(LabeledTimePicker), propertyChanged: TimeChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledTimePicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);
    
    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }
    
    private static void TimeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateTimeView();
    
    private void UpdateTimeView() => Element.Time = Time;
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateFontSizeView();
    private void UpdateFontSizeView() => Element.FontSize = FontSize;
}
