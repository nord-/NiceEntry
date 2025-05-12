using Microsoft.Maui.Handlers;

namespace NiceEntry.Platforms.iOS;

internal class EntryBaseNative : Entry
{
    static EntryBaseNative()
    {
        EntryHandler.Mapper.AppendToMapping("EntryBase", (handler, _) =>
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
        });
    }
}
