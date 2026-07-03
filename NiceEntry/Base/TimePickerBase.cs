#if IOS
using Microsoft.Maui.Handlers;
using UIKit;
#elif ANDROID
using Microsoft.Maui.Handlers;
#endif

namespace NiceEntry;

internal class TimePickerBase : TimePicker
{
#if ANDROID
    static TimePickerBase()
    {
        TimePickerHandler.Mapper.AppendToMapping("NiceEntryTimePicker", (handler, view) =>
        {
            if (view is TimePickerBase)
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
    }
#elif IOS
    static TimePickerBase()
    {
        TimePickerHandler.Mapper.AppendToMapping("NiceEntryTimePicker", (handler, view) =>
        {
            if (view is TimePickerBase)
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
