using Microsoft.Maui.Handlers;

// ReSharper disable once CheckNamespace
namespace NiceEntry.Platforms.Android;

internal class EntryBaseNative : Entry
{
    static EntryBaseNative()
    {
        EntryHandler.Mapper.AppendToMapping("EntryBase", (handler, view) =>
        {
            if (view is EntryBaseNative)
                handler.PlatformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        });
    }
}
