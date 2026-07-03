using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NiceEntryDemoApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        var viewModel = new MainViewModel();
        BindingContext = viewModel;

        
        InitializeComponent();
    }
}


public partial class MainViewModel : ValidatableViewModel
{
    [ObservableProperty,NotifyDataErrorInfo,Required(ErrorMessage = $"{nameof(MyProperty)} is required"),MinLength(3, ErrorMessage = "Minimum 3 chars")] 
    private string _myProperty = "";

    [ObservableProperty] private DateTime? _dateSelected;
    [ObservableProperty,NotifyDataErrorInfo,Required(ErrorMessage = "You have to pick a time")] private TimeSpan? _timeSelected;
    [ObservableProperty,Required(ErrorMessage = "You have to pick an item")] private PickerItem? _pickedItem;

    [ObservableProperty] private string _icaoText = "";

    public ObservableCollection<string> IcaoSuggestions { get; } = new();

    private static readonly string[] IcaoCandidates =
    [
        "ESGG", "ESSA", "ESMS", "ESSB", "ESNQ", "ESPA", "ESNU",
        "EKCH", "ENGM", "EFHK", "EDDF", "EDDM", "EHAM", "EGLL"
    ];

    partial void OnIcaoTextChanged(string value)
    {
        IcaoSuggestions.Clear();
        if (string.IsNullOrWhiteSpace(value)) return;

        foreach (var c in IcaoCandidates)
        {
            if (c.StartsWith(value, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
            {
                IcaoSuggestions.Add(c);
            }
        }
    }

    public List<PickerItem> Items { get; set; } =
    [
        new PickerItem("1", "One"),
        new PickerItem("2", "Two"),
        new PickerItem("3", "Three")
    ];

    [RelayCommand]
    private async Task OnEnterPressed()
    {
        Validate();

        if (ValidationErrors.IsValid)
        {
            // show a toast with value of myProperty
            var toast = Toast.Make($"MyProperty: {MyProperty}, Date: {DateSelected:D}, Time: {TimeSelected}\nPicked Item: {PickedItem?.PickerValue ?? "null"} ({PickedItem?.PickerKey ?? "null"})");
            await toast.Show();
        }
    }
}

public record PickerItem(string PickerKey, string PickerValue);