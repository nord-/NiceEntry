using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NiceEntryDemoApp;

public partial class ButtonsPage : ContentPage
{
    public ButtonsPage()
    {
        BindingContext = new ButtonsViewModel();
        InitializeComponent();
    }
}

public partial class ButtonsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isButtonEnabled = true;

    [RelayCommand]
    private async Task NiceButtonTapped(string? which)
    {
        var toast = Toast.Make($"NiceButton tapped: {which ?? "(no parameter)"}");
        await toast.Show();
    }
}
