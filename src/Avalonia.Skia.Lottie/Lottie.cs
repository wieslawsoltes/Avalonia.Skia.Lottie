using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;

namespace Avalonia.Skia.Lottie;

/// <summary>
/// Lottie animation player control.
/// </summary>
public class Lottie : Control, IAffectsRender
{
    private readonly Stopwatch _watch = new ();
    internal SkiaSharp.Skottie.Animation? _animation;
    internal readonly object _sync = new ();
    private DispatcherTimer? _timer;
    private int _repeatCount;
    private int _count;
    internal bool _isRunning;
    private readonly Uri _baseUri;

    /// <summary>
    /// Infinite number of repeats.
    /// </summary>
    public const int Infinity = -1;

    /// <summary>
    /// Defines the <see cref="Path"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<Lottie, string?>(nameof(Path));

    /// <summary>
    /// Defines the <see cref="Stretch"/> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<Lottie, Stretch>(nameof(Stretch), Stretch.Uniform);

    /// <summary>Lottie
    /// Defines the <see cref="StretchDirection"/> property.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<Lottie, StretchDirection>(
            nameof(StretchDirection),
            StretchDirection.Both);

    /// <summary>
    /// Defines the <see cref="RepeatCount"/> property.
    /// </summary>
    public static readonly StyledProperty<int> RepeatCountProperty = 
        AvaloniaProperty.Register<Lottie, int>(nameof(RepeatCount), Infinity);

    /// <inheritdoc/>
    public event EventHandler? Invalidated;

    /// <summary>
    /// Gets or sets the Lottie animation path.
    /// </summary>
    [Content]
    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling how the image will be stretched.
    /// </summary>
    public Stretch Stretch
    {
        get { return GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    /// <summary>
    /// Gets or sets a value controlling in what direction the image will be stretched.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get { return GetValue(StretchDirectionProperty); }
        set { SetValue(StretchDirectionProperty, value); }
    }

    /// <summary>
    ///  Sets how many times the animation should be repeated.
    /// </summary>
    public int RepeatCount
    {
        get => GetValue(RepeatCountProperty);
        set => SetValue(RepeatCountProperty, value);
    }

    static Lottie()
    {
        AffectsRender<Lottie>(PathProperty, StretchProperty, StretchDirectionProperty);
        AffectsMeasure<Lottie>(PathProperty, StretchProperty, StretchDirectionProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Lottie"/> class.
    /// </summary>
    /// <param name="baseUri">The base URL for the XAML context.</param>
    public Lottie(Uri baseUri)
    {
        _baseUri = baseUri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Lottie"/> class.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider.</param>
    public Lottie(IServiceProvider serviceProvider)
    {
        _baseUri = serviceProvider.GetContextBaseUri();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_animation == null)
        {
            return new Size();
        }
        
        var sourceSize = _animation is { }
            ? new Size(_animation.Size.Width, _animation.Size.Height)
            : default;

        return Stretch.CalculateSize(availableSize, sourceSize, StretchDirection);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_animation == null)
        {
            return new Size();
        }

        var sourceSize = _animation is { }
            ? new Size(_animation.Size.Width, _animation.Size.Height)
            : default;

        return Stretch.CalculateSize(finalSize, sourceSize);
    }

    public override void Render(DrawingContext context)
    {
        if (_animation is null || !_isRunning)
        {
            return;
        }

        var viewPort = new Rect(Bounds.Size);
        var sourceSize = new Size(_animation.Size.Width, _animation.Size.Height);
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
        {
            return;
        }

        var scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort
            .CenterRect(new Rect(scaledSize))
            .Intersect(viewPort);
        var sourceRect = new Rect(sourceSize)
            .CenterRect(new Rect(destRect.Size / scale));

        var bounds = SKRect.Create(new SKPoint(), _animation.Size);
        var scaleMatrix = Matrix.CreateScale(
            destRect.Width / sourceRect.Width,
            destRect.Height / sourceRect.Height);
        var translateMatrix = Matrix.CreateTranslation(
            -sourceRect.X + destRect.X - bounds.Top,
            -sourceRect.Y + destRect.Y - bounds.Left);

        using (context.PushClip(destRect))
        using (context.PushPreTransform(translateMatrix * scaleMatrix))
        {
            context.Custom(
                new LottieCustomDrawOperation(
                    new Rect(0, 0, bounds.Width, bounds.Height),
                    this));
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PathProperty)
        {
            var path = change.NewValue.GetValueOrDefault<string?>();
            Load(path);
            RaiseInvalidated(EventArgs.Empty);
        }

        if (change.Property == RepeatCountProperty)
        {
            _repeatCount = change.NewValue.GetValueOrDefault<int>();
            Stop();
            Start();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Raises the <see cref="Invalidated"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);

    private SkiaSharp.Skottie.Animation? Load(Stream stream)
    {
        using var managedStream = new SKManagedStream(stream);

        if (SkiaSharp.Skottie.Animation.TryCreate(managedStream, out var animation))
        {
            animation.Seek(0);

            Logger
                .TryGet(LogEventLevel.Information, LogArea.Control)?
                .Log(this, $"Version: {animation.Version} Duration: {animation.Duration} Fps:{animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
        }
        else
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "Failed to load animation.");
        }

        return animation;
    }

    private SkiaSharp.Skottie.Animation? Load(string path, Uri? baseUri)
    {
        var uri = path.StartsWith("/") ? new Uri(path, UriKind.Relative) : new Uri(path, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri && uri.IsFile)
        {
            using var fileStream = File.OpenRead(uri.LocalPath);
            return Load(fileStream);
        }

        var loader = AvaloniaLocator.Current.GetService<IAssetLoader>();
        using var assetStream = loader?.Open(uri, baseUri);
        if (assetStream is null)
        {
            return default;
        }

        return Load(assetStream);
    }

    private void Load(string? path)
    {
        if (path is null)
        {
            DisposeImpl();

            return;
        }

        DisposeImpl();

        try
        {
            _repeatCount = RepeatCount;
            _animation = Load(path, _baseUri);
            if (_animation is null)
            {
                return;
            }

            Start();
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "Failed to load animation: " + e);
            _animation = null;
        }
    }

    private void DisposeImpl()
    {
        lock (_sync)
        {
            Stop();
            _animation?.Dispose();
            _animation = null;
        }
    }

    private void Start()
    {
        if (_animation is null)
        {
            return;
        }

        if (_repeatCount == 0)
        {
            return;
        }

        _count = 0;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(1 / 60.0, 1 / _animation.Fps))
        };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        _watch.Start();

        _isRunning = true;
    }

    private void Tick()
    {
        if (_timer is null)
        {
            return;
        }

        if (_repeatCount == 0 || (_repeatCount > 0 && _count >= _repeatCount))
        {
            _isRunning = false;
            _timer.Stop();
            _watch.Stop();
            InvalidateVisual();
        }

        if (_isRunning)
        {
            InvalidateVisual();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void Stop()
    {
        _isRunning = false;

        _timer?.Stop();
        _timer = null;

        _watch.Reset();

        _count = 0;
    }

    internal float GetFrameTime()
    {
        if (_animation is null || _timer is null)
        {
            return 0f;
        }

        var frameTime = (float)_watch.Elapsed.TotalSeconds;
 
        if (_watch.Elapsed.TotalSeconds > _animation.Duration)
        {
            _watch.Restart();
            _count++;
        }

        return frameTime;
    }
}
