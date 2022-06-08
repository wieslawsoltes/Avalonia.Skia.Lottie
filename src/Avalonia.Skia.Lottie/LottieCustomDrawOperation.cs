using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using SkiaSharp;

namespace Avalonia.Skia.Lottie;

internal class LottieCustomDrawOperation : ICustomDrawOperation
{
    private readonly Lottie _lottie;

    public LottieCustomDrawOperation(Rect bounds, Lottie lottie)
    {
        _lottie = lottie;
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

        lock (_lottie._sync)
        {
            canvas.Save();

            var animation = _lottie._animation;
            if (animation is null)
            {
                return;
            }

            var watch = _lottie._watch;

            animation.SeekFrameTime((float)watch.Elapsed.TotalSeconds);

            if (watch.Elapsed.TotalSeconds > animation.Duration)
            {
                if (_lottie._isRunning)
                {
                    watch.Restart();
                    _lottie._count++;
                }
            }

            animation.Render(canvas, new SKRect(0, 0, animation.Size.Width, animation.Size.Height));

            canvas.Restore();
        }
    }
}
