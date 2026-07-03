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

        UpdateFontSizeView();
        UpdateTitleColorView();
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(LabeledPicker), defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(LabeledPicker), -1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(LabeledPicker), defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(LabeledPicker), propertyChanged: PlaceholderChanged);
    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(nameof(TitleColor), typeof(Color), typeof(LabeledPicker), Color.FromArgb("#808080"), propertyChanged: TitleColorChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledPicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);

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

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    // Deliberately a plain CLR property, mirroring MAUI's Picker.ItemDisplayBinding: as a
    // BindableProperty, XAML applies "{Binding X}" against the BindingContext instead of
    // assigning the binding itself, so the inner picker never receives it (#37).
    public BindingBase? ItemDisplayBinding
    {
        get => Element.ItemDisplayBinding;
        set => Element.ItemDisplayBinding = value;
    }

    private static void PlaceholderChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdatePlaceholder();
    private static void TitleColorChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdateTitleColorView();
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledPicker)bindable).UpdateFontSizeView();

    private void UpdatePlaceholder() => Element.Title = Placeholder;
    private void UpdateFontSizeView() => Element.FontSize = FontSize;
    private void UpdateTitleColorView() => Element.TitleColor = TitleColor;
}
