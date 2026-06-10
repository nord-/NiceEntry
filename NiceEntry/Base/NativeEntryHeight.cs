#if IOS
using UIKit;

namespace NiceEntry;

/// <summary>
/// Measures the height of a borderless <see cref="UITextField"/> so the
/// picker-style inputs match the height of <c>EntryBase</c> at the same
/// font size. Results are cached per font size.
/// </summary>
internal static class NativeEntryHeight
{
    private static readonly Dictionary<double, double> Cache = new();

    public static double For(double fontSize)
    {
        var key = fontSize > 0 ? fontSize : 0;
        if (Cache.TryGetValue(key, out var height)) return height;

        using var reference = new UITextField { BorderStyle = UITextBorderStyle.None };
        if (key > 0)
            reference.Font = UIFont.SystemFontOfSize((nfloat)key);

        var size = reference.SizeThatFits(new CoreGraphics.CGSize(nfloat.MaxValue, nfloat.MaxValue));
        Cache[key] = size.Height;
        return size.Height;
    }
}
#endif
