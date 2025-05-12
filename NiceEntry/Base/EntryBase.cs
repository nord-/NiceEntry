
#if ANDROID
using NiceEntry.Platforms.Android;
#elif IOS 
using NiceEntry.Platforms.iOS;
#endif

namespace NiceEntry;

internal class EntryBase : EntryBaseNative;
