using Microsoft.Maui.Handlers;

// ReSharper disable once CheckNamespace
namespace NiceEntry.Platforms.iOS;

internal class EntryBaseNative : Entry
{
    static EntryBaseNative()
    {
        EntryHandler.Mapper.AppendToMapping("EntryBase", (handler, view) =>
        {
            if (view is EntryBaseNative)
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
        });
    }
}
