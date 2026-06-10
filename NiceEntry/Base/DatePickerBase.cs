#if IOS
using Microsoft.Maui.Handlers;
using UIKit;
#elif ANDROID
using Microsoft.Maui.Handlers;
#endif

namespace NiceEntry;

internal class DatePickerBase : DatePicker
{
#if ANDROID
    static DatePickerBase()
    {
        DatePickerHandler.Mapper.AppendToMapping("NiceEntryDatePicker", (handler, view) =>
        {
            if (view is DatePickerBase)
                handler.PlatformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        });
    }
#elif IOS
    static DatePickerBase()
    {
        DatePickerHandler.Mapper.AppendToMapping("NiceEntryDatePicker", (handler, view) =>
        {
            if (view is DatePickerBase)
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
