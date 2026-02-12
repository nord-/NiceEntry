namespace NiceEntry;

public partial class LabelBase
{
    public LabelBase()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateLabelContainerBackground();
    }

    public static readonly BindableProperty ViewProperty = BindableProperty.Create("View", 
                                                                                   typeof(View), 
                                                                                   typeof(LabelBase),
                                                                                   defaultValue: null, 
                                                                                   defaultBindingMode: BindingMode.OneWay,
                                                                                   propertyChanged: ElementChanged
                                                                                   //validateValue: ViewHelper.ValidateCustomView
                                );
    public static readonly BindableProperty IsRequiredProperty = BindableProperty.Create(nameof(IsRequired), typeof(bool), typeof(LabelBase), defaultValue: false, propertyChanged: IsRequiredChanged);
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(LabelBase), propertyChanged: LabelChanged);
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(IReadOnlyCollection<string>), typeof(LabelBase), propertyChanged: ErrorChanged);
    public View View { get => (View)GetValue(ViewProperty); set => SetValue(ViewProperty, value); }
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public IReadOnlyCollection<string> Error { get => (IReadOnlyCollection<string>)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    private static void ElementChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateElementView();
    private static void IsRequiredChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateIsRequiredView();
    private static void LabelChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateLabelView();
    private static void ErrorChanged(BindableObject bindable, object oldValue, object newValue) => ((LabelBase)bindable).UpdateErrorView();
    
    private void UpdateElementView()
    {
        BorderLabel.Content = View;
        UpdateIsRequiredView();
    }

    private void UpdateIsRequiredView()
    {
        RequiredLabel.IsVisible = IsRequired;
    }

    private void UpdateLabelView()
    {
        LabelLabel.Text = Label;
        LabelLabel.IsVisible = !string.IsNullOrEmpty(Label);
    }
    
    private void UpdateErrorView()
    {
        ErrorLabel.Text = string.Join(',', Error);
        ErrorLabel.IsVisible = Error.Count > 0;
        ChangeBorderColor();
    }

    private void UpdateLabelContainerBackground()
    {
        LabelContainer.BackgroundColor = FindAncestorBackgroundColor() ??
            (Application.Current!.UserAppTheme == AppTheme.Light ? Colors.White : Colors.Black);
    }

    private Color? FindAncestorBackgroundColor()
    {
        Element? current = this;
        while (current != null)
        {
            if (current is VisualElement ve
                && ve.BackgroundColor is not null
                && ve.BackgroundColor != Colors.Transparent)
            {
                return ve.BackgroundColor;
            }
            current = current.Parent;
        }
        return null;
    }

    private void ChangeBorderColor()
    {
        if (Error.Count == 0)
        {
            BorderLabel.Stroke = Application.Current!.UserAppTheme == AppTheme.Light ? Color.FromArgb("#212121") : Color.FromArgb("#E1E1E1");
        }
        else
        {
            BorderLabel.Stroke = Colors.Red;
        }
    }
}