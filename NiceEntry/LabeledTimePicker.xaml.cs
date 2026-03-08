namespace NiceEntry;

public partial class LabeledTimePicker
{
	public LabeledTimePicker()
	{
		InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(TimePicker.TimeProperty, nameof(Time), BindingMode.TwoWay);
        Element.BindingContext = this;
	}
    
    public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(TimeSpan), typeof(LabeledTimePicker), propertyChanged: TimeChanged, defaultBindingMode: BindingMode.TwoWay);
    
    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }
    
    private static void TimeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateTimeView();
    
    private void UpdateTimeView() => Element.Time = Time;

}