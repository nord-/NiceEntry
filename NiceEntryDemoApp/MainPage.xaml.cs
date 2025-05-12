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
    [ObservableProperty,NotifyDataErrorInfo,Required(ErrorMessage = $"{nameof(MyProperty)} is required"),MinLength(2, ErrorMessage = "Minimum 3 chars")] 
    private string _myProperty = "";

    [RelayCommand]
    private async Task OnEnterPressed()
    {
        Validate();

        if (ValidationErrors.IsValid)
        {
            // show a toast with value of myProperty
            var toast = Toast.Make("MyProperty: " + MyProperty);
            await toast.Show();
        }
    }
}