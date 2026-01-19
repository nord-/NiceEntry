using System.Windows.Input;

namespace NiceEntry;

public partial class LabeledEntry
{
    public LabeledEntry()
    {
        InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(Entry.TextProperty, nameof(Text), BindingMode.TwoWay);
        Element.BindingContext = this;
    }
    
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(LabeledEntry), propertyChanged: TextChanged, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(LabeledEntry), propertyChanged: PlaceholderChanged);
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(int), int.MaxValue, propertyChanged: MaxLengthChanged);
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(LabeledEntry), propertyChanged: ReturnTypeChanged);
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(LabeledEntry), propertyChanged: KeyboardChanged);
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(LabeledEntry), false, propertyChanged: IsPasswordChanged);
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(LabeledEntry), false, propertyChanged: IsReadOnlyChanged);
    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(nameof(ReturnCommand), typeof(ICommand), typeof(LabeledEntry), propertyChanged: ReturnCommandChanged);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }
 
    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

	public new bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public ICommand ReturnCommand
    {
        get => (ICommand)GetValue(ReturnCommandProperty);
        set => SetValue(ReturnCommandProperty, value);
    }
   
    private static void TextChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateTextView();
    private static void PlaceholderChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdatePlaceholderView();
    private static void KeyboardChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateKeyboardView();
    private static void ReturnTypeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateReturnTypeView();
    private static void IsPasswordChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateIsPasswordView();
    private static void IsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateIsReadOnlyView();
    private static void MaxLengthChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateMaxLengthView();
    private static void ReturnCommandChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledEntry)bindable).UpdateReturnCommandView();    
    
    private void UpdateTextView() => Element.Text = Text;
    private void UpdatePlaceholderView() => Element.Placeholder = Placeholder;
    private void UpdateKeyboardView() => Element.Keyboard = Keyboard;
    private void UpdateReturnTypeView() => Element.ReturnType = ReturnType;
    private void UpdateIsPasswordView() => Element.IsPassword = IsPassword;
    private void UpdateIsReadOnlyView() => Element.IsReadOnly = IsReadOnly;
    private void UpdateMaxLengthView() => Element.MaxLength = MaxLength;
    private void UpdateReturnCommandView() => Element.ReturnCommand = ReturnCommand;
}