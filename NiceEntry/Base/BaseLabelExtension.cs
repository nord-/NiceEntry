// ReSharper disable once CheckNamespace
namespace NiceEntry;

internal static class BaseLabelExtension
{
    public static void SetVisualElementBinding(this VisualElement visualElement)
    {
        visualElement.SetBinding(VisualElement.IsEnabledProperty, nameof(visualElement.IsEnabled), BindingMode.TwoWay);
        visualElement.SetBinding(VisualElement.IsVisibleProperty, nameof(visualElement.IsVisible), BindingMode.TwoWay);
    }
}