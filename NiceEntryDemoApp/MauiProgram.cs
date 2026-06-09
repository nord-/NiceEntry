using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using NiceEntry;

namespace NiceEntryDemoApp;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .UseMauiCommunityToolkit()
               .UseNiceEntry()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
               });

#if DEBUG
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
