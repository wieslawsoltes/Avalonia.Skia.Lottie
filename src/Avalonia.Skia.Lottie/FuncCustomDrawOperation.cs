using System;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using SkiaSharp;

namespace Avalonia.Skia.Lottie;

internal class FuncCustomDrawOperation : ICustomDrawOperation
{
    private readonly Action<SKCanvas> _draw;

    public FuncCustomDrawOperation(Rect bounds, Action<SKCanvas> draw)
    {
        _draw = draw;
        Bounds = bounds;
    }

    public void Dispose()
    {
    }

    public Rect Bounds { get; }

    public bool HitTest(Point p) => true;

    public bool Equals(ICustomDrawOperation? other) => false;

    public void Render(IDrawingContextImpl context)
    {
        var leaseFeature = context.GetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
        {
            return;
        }
        using var lease = leaseFeature.Lease();
        var canvas = lease?.SkCanvas;
        if (canvas is not null)
        {
            _draw(canvas);
        }
    }
}
