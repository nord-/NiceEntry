namespace NiceEntry;

public static class AppHostBuilderExtensions
{
    /// <summary>Font family alias for the bundled Material Design Icons webfont.</summary>
    public const string MaterialDesignIconsFontFamily = "MaterialDesignIcons";

    /// <summary>
    /// Registers NiceEntry's bundled fonts (Material Design Icons). Call exactly once during
    /// startup. Do NOT also register the <c>MaterialDesignIcons</c> alias manually:
    /// <c>ConfigureFonts</c> appends to a font list, so a duplicate alias adds a duplicate
    /// descriptor and can break font resolution.
    /// </summary>
    public static MauiAppBuilder UseNiceEntry(this MauiAppBuilder builder)
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddEmbeddedResourceFont(
                typeof(AppHostBuilderExtensions).Assembly,
                "materialdesignicons-webfont.ttf",
                MaterialDesignIconsFontFamily);
        });

        return builder;
    }
}
