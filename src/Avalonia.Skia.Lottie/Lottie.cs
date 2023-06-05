using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using SkiaSharp;

namespace Avalonia.Skia.Lottie;

/// <summary>
/// Lottie animation player control.
/// </summary>
public class Lottie : Control
{
    private SkiaSharp.Skottie.Animation? _animation;
    private static int _repeatCount;
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

    private CompositionCustomVisual? _customVisual;

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

    protected override void OnLoaded()
    {
        var elemVisual = ElementComposition.GetElementVisual(this);
        var compositor = elemVisual?.Compositor;
        if (compositor is null) return;
        _customVisual = compositor.CreateCustomVisual(new SkottieCustomVisualHandler());
        ElementComposition.SetElementChildVisual(this, _customVisual);
        LayoutUpdated += OnLayoutUpdated;
        
        base.OnLoaded();
        
        if (preloadPath is not null)
        {
            DisposeImpl();
            Load(preloadPath);
            _customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);
            _customVisual.SendHandlerMessage(new SkottieCustomVisualHandler.Payload(SkottieCustomVisualHandler.Command.Update,
                _animation, Stretch, StretchDirection));
            Start();
            preloadPath = null;
        }
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        if (_customVisual == null) return;
        _customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);
        _customVisual.SendHandlerMessage(new SkottieCustomVisualHandler.Payload(SkottieCustomVisualHandler.Command.Update,
            _animation, Stretch, StretchDirection));
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

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        switch (change.Property.Name)
        {
            case nameof(Path):
                var path = change.GetNewValue<string?>();

                if (preloadPath is null && _customVisual is null)
                {
                    preloadPath = path;
                    return;
                }
                
                Load(path); 
                break;
            case nameof(RepeatCount):
                _repeatCount = change.GetNewValue<int>();
                Stop();
                Start();
                break; 
            
        }
    }

    private void Stop()
    {
        _customVisual?.SendHandlerMessage(new SkottieCustomVisualHandler.Payload(SkottieCustomVisualHandler.Command.Stop));
    }

    private SkiaSharp.Skottie.Animation? Load(Stream stream)
    {
        // var data = new StreamReader(stream).ReadToEnd();
        // if (SkiaSharp.Skottie.Animation.TryParse(data, out var animation)) 
        using var managedStream = new SKManagedStream(stream);
        if (SkiaSharp.Skottie.Animation.TryCreate(managedStream, out var animation))
        {
            animation.Seek(0);

            Logger
                .TryGet(LogEventLevel.Information, LogArea.Control)?
                .Log(this,
                    $"Version: {animation.Version} Duration: {animation.Duration} Fps:{animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
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

        using var assetStream = AssetLoader.Open(uri, baseUri);
        if (assetStream is null)
        {
            return default;
        }

        return Load(assetStream);
    }

    private string? preloadPath;
    
    private void Load(string? path)
    {
        Stop();
        
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

            InvalidateArrange();
            InvalidateMeasure();
            
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
        _customVisual?.SendHandlerMessage(new SkottieCustomVisualHandler.Payload(SkottieCustomVisualHandler.Command.Dispose));
    }

    private void Start()
    {
        _customVisual?.SendHandlerMessage(new SkottieCustomVisualHandler.Payload(SkottieCustomVisualHandler.Command.Start, _animation,
            Stretch, StretchDirection, _repeatCount));
    }
}
