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
    internal readonly Stopwatch _watch = new ();
    internal SkiaSharp.Skottie.Animation? _animation;
    internal readonly object _sync = new ();
    private DispatcherTimer? _timer;
    private readonly Uri _baseUri;
    private bool _enableCache;
    private Dictionary<string, SkiaSharp.Skottie.Animation>? _cache;

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
    /// Defines the <see cref="EnableCache"/> property.
    /// </summary>
    public static readonly DirectProperty<Lottie, bool> EnableCacheProperty =
        AvaloniaProperty.RegisterDirect<Lottie, bool>(nameof(EnableCache),
            o => o.EnableCache,
            (o, v) => o.EnableCache = v);

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
    /// Gets or sets a value controlling whether the loaded images are cached.
    /// </summary>
    public bool EnableCache
    {
        get { return _enableCache; }
        set { SetAndRaise(EnableCacheProperty, ref _enableCache, value); }
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
        if (_animation is null)
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

        if (change.Property == EnableCacheProperty)
        {
            var enableCache = change.NewValue.GetValueOrDefault<bool>();
            if (enableCache == false)
            {
                Stop();
                DisposeCache();
            }
            else
            {
                _cache = new Dictionary<string, SkiaSharp.Skottie.Animation>();
            }
        }
    }

    private static SkiaSharp.Skottie.Animation? Load(Stream stream)
    {
        using var managedStream = new SKManagedStream(stream);

        if (SkiaSharp.Skottie.Animation.TryCreate(managedStream, out var animation))
        {
            animation.Seek(0);
            Debug.WriteLine($"Version: {animation.Version} Duration: {animation.Duration} Fps:{animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
        }
        else
        {
            Debug.WriteLine($"Failed to load animation.");
        }
            
        return animation;
    }

    private static SkiaSharp.Skottie.Animation? Load(string path, Uri? baseUri)
    {
        var uri = path.StartsWith("/") ? new Uri(path, UriKind.Relative) : new Uri(path, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri && uri.IsFile)
        {
            using var fileStream = File.OpenRead(uri.LocalPath);
            return Load(fileStream);
        }
        else
        {
            var loader = AvaloniaLocator.Current.GetService<IAssetLoader>();
            using var assetStream = loader?.Open(uri, baseUri);
            if (assetStream is null)
            {
                return default;
            }

            return Load(assetStream);
        }
    }
    
    private void Load(string? path)
    {
        if (path is null)
        {
            lock (_sync)
            {
                Stop();
                _animation?.Dispose();
                _animation = null;
            }

            DisposeCache();
            return;
        }

        if (_enableCache && _cache is { } && _cache.TryGetValue(path, out var animation))
        {
            Stop();
            _animation = animation;
            Start();
            return;
        }

        if (!_enableCache)
        {
            lock (_sync)
            {
                Stop();
                _animation?.Dispose();
                _animation = null;
            }
        }

        try
        {
            Stop();
            _animation = Load(path, _baseUri);
            if (_animation is null)
            {
                return;
            }

            Start();

            if (_enableCache && _cache is { } && _animation is { })
            {
                _cache[path] = _animation;
            }
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "Failed to load animation: " + e);
            _animation = null;
        }
    }

    private void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _watch.Reset();
    }

    private void Start()
    {
        if (_animation is null)
        {
            return;
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(1 / 60.0, 1 / _animation.Fps))
        };

        _timer.Tick += (_, _) =>
        {
            _animation
            InvalidateVisual();
        };

        _timer.Start();
        _watch.Start();
    }

    private void DisposeCache()
    {
        if (_cache is null)
        {
            return;
        }

        lock (_sync)
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value != _animation)
                {
                    kvp.Value.Dispose();
                }
            }

            _cache = null;
        }
    }

    /// <summary>
    /// Raises the <see cref="Invalidated"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);
}
