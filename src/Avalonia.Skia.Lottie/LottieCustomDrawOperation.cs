using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Skia.Lottie;

internal class LottieCustomDrawOperation : ICustomDrawOperation
{
    private readonly SkiaSharp.Skottie.Animation _animation;
    private readonly Stopwatch _watch;

    public LottieCustomDrawOperation(Rect bounds, SkiaSharp.Skottie.Animation animation, Stopwatch watch)
    {
        _animation = animation;
        _watch = watch;
        Bounds = bounds;
    }

    public void Dispose()
    {
    }

    public Rect Bounds { get; }

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) => false;

    public void Render(IDrawingContextImpl context)
    {
        var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
        if (canvas is null)
        {
            return;
        }

        canvas.Save();

        _animation.SeekFrameTime((float)_watch.Elapsed.TotalSeconds);

        if (_watch.Elapsed.TotalSeconds > _animation.Duration)
        {
            _watch.Restart();
        }

        _animation.Render(canvas, new SKRect(0, 0, _animation.Size.Width, _animation.Size.Height));

        canvas.Restore();
    }
}
