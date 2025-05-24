using Avalonia.Media;

namespace Avalonia.Skia.Lottie;

internal record struct LottiePayload(
    LottieCommand LottieCommand,
    SkiaSharp.Skottie.Animation? Animation = null,
    Stretch? Stretch = null,
    StretchDirection? StretchDirection = null,
    int? RepeatCount = null);
