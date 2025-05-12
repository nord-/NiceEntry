using System.Collections;

namespace NiceEntry;

public partial class LabeledPicker
{
	public LabeledPicker()
	{
		InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(Picker.ItemsSourceProperty, nameof(ItemsSource), BindingMode.TwoWay);
        Element.SetBinding(Picker.SelectedIndexProperty, nameof(SelectedIndex), BindingMode.TwoWay);
        Element.SetBinding(Picker.SelectedItemProperty, nameof(SelectedItem), BindingMode.TwoWay);
        Element.BindingContext = this;
    }
    
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(LabeledPicker), propertyChanged: ItemSourceChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(LabeledPicker), propertyChanged: SelectedIndexChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(LabeledPicker), propertyChanged: SelectedItemChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(LabeledPicker), propertyChanged: PlaceholderChanged);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public BindingBase ItemDisplayBinding
    {
        get => Element?.ItemDisplayBinding!;
        set => Element.ItemDisplayBinding = value;
    }

    private static void ItemSourceChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdateItemSourceView();
    private static void SelectedIndexChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdateSelectedIndex();
    private static void SelectedItemChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdateSelectedItem();
    private static void PlaceholderChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdatePlaceholder();
    
    private void UpdateItemSourceView() => Element.ItemsSource = ItemsSource;
    private void UpdateSelectedIndex() => Element.SelectedIndex = SelectedIndex;
    private void UpdateSelectedItem() => Element.SelectedItem = SelectedItem;
    private void UpdatePlaceholder() => Element.Title = Placeholder;
}