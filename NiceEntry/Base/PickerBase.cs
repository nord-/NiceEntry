#if IOS
using Microsoft.Maui.Handlers;
using UIKit;
#elif ANDROID
using Microsoft.Maui.Handlers;
#endif

namespace NiceEntry;

internal class PickerBase : Picker
{
#if ANDROID
    static PickerBase()
    {
        PickerHandler.Mapper.AppendToMapping("NiceEntryPicker", (handler, view) =>
        {
            if (view is PickerBase)
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
    }
#elif IOS
    static PickerBase()
    {
        PickerHandler.Mapper.AppendToMapping("NiceEntryPicker", (handler, view) =>
        {
            if (view is PickerBase)
                handler.PlatformView.BorderStyle = UITextBorderStyle.None;
        });
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var size = base.MeasureOverride(widthConstraint, heightConstraint);
        return new Size(size.Width, NativeEntryHeight.For(FontSize));
    }
#endif
}
