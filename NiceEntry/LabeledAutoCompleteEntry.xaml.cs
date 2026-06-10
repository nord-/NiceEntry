using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;

namespace NiceEntry;

public partial class LabeledAutoCompleteEntry
{
    private static readonly DataTemplate DefaultSuggestionTemplate = new(() =>
    {
        var label = new Microsoft.Maui.Controls.Label
        {
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Center
        };
        label.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding("."));
        return label;
    });

    private INotifyCollectionChanged? _observedSuggestions;

    public LabeledAutoCompleteEntry()
    {
        InitializeComponent();

        Entry.SetBinding(LabeledEntry.TextProperty, nameof(Text), BindingMode.TwoWay);
        Entry.BindingContext = this;

        Entry.Element.Focused += OnEntryFocused;
        Entry.Element.Unfocused += OnEntryUnfocused;

        // Detach the CollectionChanged subscription while unloaded so a
        // long-lived Suggestions collection doesn't keep this control (and
        // its page) alive after navigation.
        Loaded += (_, _) =>
        {
            AttachObservedSuggestions();
            RebuildVisibleSuggestions();
        };
        Unloaded += (_, _) => DetachObservedSuggestions();

        SuggestionsView.ItemTemplate = DefaultSuggestionTemplate;
    }

    // Forwarded to inner LabeledEntry
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.Label = (string)n);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(LabeledAutoCompleteEntry),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: TextChanged);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.Placeholder = (string)n);

    public static readonly BindableProperty IsRequiredProperty = BindableProperty.Create(
        nameof(IsRequired), typeof(bool), typeof(LabeledAutoCompleteEntry), defaultValue: false,
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.IsRequired = (bool)n);

    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(
        nameof(Error), typeof(IReadOnlyCollection<string>), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.Error = (IReadOnlyCollection<string>)n);

    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
        nameof(ReturnType), typeof(ReturnType), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.ReturnType = (ReturnType)n);

    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
        nameof(ReturnCommand), typeof(ICommand), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.ReturnCommand = (ICommand)n);

    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
        nameof(Keyboard), typeof(Keyboard), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.Keyboard = (Keyboard)n);

    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
        nameof(MaxLength), typeof(int), typeof(LabeledAutoCompleteEntry), defaultValue: int.MaxValue,
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.MaxLength = (int)n);

    public static readonly BindableProperty HorizontalTextAlignmentProperty = BindableProperty.Create(
        nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(LabeledAutoCompleteEntry),
        defaultValue: TextAlignment.Start,
        propertyChanged: (b, _, n) => ((LabeledAutoCompleteEntry)b).Entry.HorizontalTextAlignment = (TextAlignment)n);

    // Local to the autocomplete control
    public static readonly BindableProperty SuggestionsProperty = BindableProperty.Create(
        nameof(Suggestions), typeof(IEnumerable), typeof(LabeledAutoCompleteEntry),
        propertyChanged: SuggestionsChanged);

    public static readonly BindableProperty MaxSuggestionsProperty = BindableProperty.Create(
        nameof(MaxSuggestions), typeof(int), typeof(LabeledAutoCompleteEntry), defaultValue: 8,
        propertyChanged: (b, _, _) => ((LabeledAutoCompleteEntry)b).RebuildVisibleSuggestions());

    public static readonly BindableProperty SuggestionTemplateProperty = BindableProperty.Create(
        nameof(SuggestionTemplate), typeof(DataTemplate), typeof(LabeledAutoCompleteEntry),
        propertyChanged: (b, _, _) => ((LabeledAutoCompleteEntry)b).UpdateSuggestionTemplate());

    public static readonly BindableProperty CommitOnUpperCaseProperty = BindableProperty.Create(
        nameof(CommitOnUpperCase), typeof(bool), typeof(LabeledAutoCompleteEntry), defaultValue: false);

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public IReadOnlyCollection<string> Error { get => (IReadOnlyCollection<string>)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public ReturnType ReturnType { get => (ReturnType)GetValue(ReturnTypeProperty); set => SetValue(ReturnTypeProperty, value); }
    public ICommand ReturnCommand { get => (ICommand)GetValue(ReturnCommandProperty); set => SetValue(ReturnCommandProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public TextAlignment HorizontalTextAlignment { get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty); set => SetValue(HorizontalTextAlignmentProperty, value); }
    public IEnumerable Suggestions { get => (IEnumerable)GetValue(SuggestionsProperty); set => SetValue(SuggestionsProperty, value); }
    public int MaxSuggestions { get => (int)GetValue(MaxSuggestionsProperty); set => SetValue(MaxSuggestionsProperty, value); }
    public DataTemplate SuggestionTemplate { get => (DataTemplate)GetValue(SuggestionTemplateProperty); set => SetValue(SuggestionTemplateProperty, value); }
    public bool CommitOnUpperCase { get => (bool)GetValue(CommitOnUpperCaseProperty); set => SetValue(CommitOnUpperCaseProperty, value); }

    private static void TextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var self = (LabeledAutoCompleteEntry)bindable;
        if (self.CommitOnUpperCase && newValue is string s && s.Length > 0)
        {
            var upper = s.ToUpperInvariant();
            if (!string.Equals(s, upper, StringComparison.Ordinal))
                self.Text = upper;
        }
    }

    private static void SuggestionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var self = (LabeledAutoCompleteEntry)bindable;

        self.DetachObservedSuggestions();
        self.AttachObservedSuggestions();
        self.RebuildVisibleSuggestions();
    }

    private void AttachObservedSuggestions()
    {
        if (_observedSuggestions is not null) return;
        if (!IsLoaded) return;
        if (Suggestions is not INotifyCollectionChanged notify) return;

        _observedSuggestions = notify;
        notify.CollectionChanged += OnSuggestionsCollectionChanged;
    }

    private void DetachObservedSuggestions()
    {
        if (_observedSuggestions is null) return;

        _observedSuggestions.CollectionChanged -= OnSuggestionsCollectionChanged;
        _observedSuggestions = null;
    }

    private void OnSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildVisibleSuggestions();

    private void RebuildVisibleSuggestions()
    {
        if (Suggestions is null)
        {
            SuggestionsView.ItemsSource = null;
            UpdateDropdownVisibility();
            return;
        }

        var cap = MaxSuggestions < 0 ? int.MaxValue : MaxSuggestions;
        var list = new List<object>();
        foreach (var item in Suggestions)
        {
            if (list.Count >= cap) break;
            if (item is not null) list.Add(item);
        }

        SuggestionsView.ItemsSource = list;
        UpdateDropdownVisibility();
    }

    private void UpdateSuggestionTemplate()
        => SuggestionsView.ItemTemplate = SuggestionTemplate ?? DefaultSuggestionTemplate;

    private void UpdateDropdownVisibility()
    {
        var hasItems = SuggestionsView.ItemsSource is ICollection c && c.Count > 0;
        DropdownBorder.IsVisible = hasItems && Entry.Element.IsFocused;
    }

    private void OnEntryFocused(object? sender, FocusEventArgs e)
        => UpdateDropdownVisibility();

    private void OnEntryUnfocused(object? sender, FocusEventArgs e)
        => DropdownBorder.IsVisible = false;

    private void OnSuggestionTapped(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0) return;

        var picked = e.CurrentSelection[0]?.ToString();
        SuggestionsView.SelectedItem = null;

        if (picked is null) return;

        if (CommitOnUpperCase)
            picked = picked.ToUpperInvariant();

        Text = picked;

        DropdownBorder.IsVisible = false;
        Entry.Element.CursorPosition = picked.Length;
    }
}
