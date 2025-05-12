using Microsoft.Maui.Handlers;

namespace NiceEntry.Platforms.Android;

internal class EntryBaseNative : Entry
{
    static EntryBaseNative()
    {
        EntryHandler.Mapper.AppendToMapping("EntryBase", (handler, _) =>
        {
            handler.PlatformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        });
    }
}
