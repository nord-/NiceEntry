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
        TimePickerHandler.Mapper.AppendToMapping("NiceEntryTimePicker", (handler, _) =>
        {
            handler.PlatformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        });
    }
#elif IOS
    private static nfloat? _entryHeight;

    static TimePickerBase()
    {
        TimePickerHandler.Mapper.AppendToMapping("NiceEntryTimePicker", (handler, _) =>
        {
            handler.PlatformView.BorderStyle = UITextBorderStyle.None;
        });
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var size = base.MeasureOverride(widthConstraint, heightConstraint);

        if (_entryHeight is null)
        {
            using var reference = new UITextField { BorderStyle = UITextBorderStyle.None };
            var refSize = reference.SizeThatFits(new CoreGraphics.CGSize(nfloat.MaxValue, nfloat.MaxValue));
            _entryHeight = refSize.Height;
        }

        return new Size(size.Width, (double)_entryHeight.Value);
    }
#endif
}
