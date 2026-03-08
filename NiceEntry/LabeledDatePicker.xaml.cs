namespace NiceEntry;

public partial class LabeledDatePicker
{
	public LabeledDatePicker()
	{
		InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(DatePicker.DateProperty, nameof(Date), BindingMode.TwoWay);
        Element.BindingContext = this;
	}
    
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime), typeof(LabeledDatePicker), propertyChanged: DateChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(nameof(MinimumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MinValue, propertyChanged: MinimumDateChanged);
    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(nameof(MaximumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MaxValue, propertyChanged: MaximumDateChanged);
    
    public DateTime Date
    {
        get => (DateTime)GetValue(DateProperty);
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
    
    private static void DateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateDateView();
    private static void MinimumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMinimumDateView();
    private static void MaximumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMaximumDateView();
    
    private void UpdateDateView() => Element.Date = Date;
    private void UpdateMinimumDateView() => Element.MinimumDate = MinimumDate;
    private void UpdateMaximumDateView() => Element.MaximumDate = MaximumDate;

}