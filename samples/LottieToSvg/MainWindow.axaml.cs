using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace LottieToSvg;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

        if (!e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.FileNames))
        {
            return;
        }

        var paths = e.Data.GetFileNames()?.ToList();
        if (paths is null || paths.Count <= 0)
        {
            return;
        }

        await Task.Run(() =>
        {
            foreach (var path in paths)
            {
                var outputPath = "/Users/wieslawsoltes/Downloads/Temp";
                // var outputPath = "c:\\Temp\\";
                Convert(path, outputPath);
            }
        });
    }

    private void CreatePdf(SkiaSharp.Skottie.Animation animation, string inputFilePath, string outputPath)
    {
        var inPoint = animation.InPoint;
        var outPoint = animation.OutPoint;
        var dst = new SKRect(0, 0, animation.Size.Width, animation.Size.Height);

        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}.pdf";
        var outputFilePath = Path.Combine(outputPath, outputFileName);
        using var outputStream = new SKFileWStream(outputFilePath);
        using var document = SKDocument.CreatePdf(outputStream, SKDocument.DefaultRasterDpi);
        if (document is null)
        {
            return;
        }

        for (var t = inPoint; t <= outPoint; t += 1d)
        {
            using var canvas = document.BeginPage(animation.Size.Width, animation.Size.Height);
            animation.SeekFrame(t);
            animation.Render(canvas, dst);
        }

        document.Close();
    }
    
    private void CreateXps(SkiaSharp.Skottie.Animation animation, string inputFilePath, string outputPath)
    {
        var inPoint = animation.InPoint;
        var outPoint = animation.OutPoint;
        var dst = new SKRect(0, 0, animation.Size.Width, animation.Size.Height);
        
        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}.xps";
        var outputFilePath = Path.Combine(outputPath, outputFileName);
        using var outputStream = new SKFileWStream(outputFilePath);
        using var document = SKDocument.CreateXps(outputStream, SKDocument.DefaultRasterDpi);
        if (document is null)
        {
            return;
        }

        for (var t = inPoint; t <= outPoint; t += 1d)
        {
            using var canvas = document.BeginPage(animation.Size.Width, animation.Size.Height);
            animation.SeekFrame(t);
            animation.Render(canvas, dst);
        }

        document.Close();
    }

    private void CreateSvg(SkiaSharp.Skottie.Animation animation, string inputFilePath, string outputPath)
    {
        var inPoint = animation.InPoint;
        var outPoint = animation.OutPoint;
        var dst = new SKRect(0, 0, animation.Size.Width, animation.Size.Height);

        for (var t = inPoint; t <= outPoint; t += 1d)
        {
            var outputFileExtension = "svg";
            var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}.{t:0000}.{outputFileExtension}";
            var outputFilePath = Path.Combine(outputPath, outputFileName);

            using var outputStream = File.Create(outputFilePath);
            using var outputManagedStream = new SKManagedWStream(outputStream);

            var bounds = SKRect.Create(animation.Size);
            using var canvas = SKSvgCanvas.Create(bounds, outputManagedStream);

            animation.SeekFrame(t);
            animation.Render(canvas, dst);
        } 
    }
    
    private void CreatePng(SkiaSharp.Skottie.Animation animation, string inputFilePath, string outputPath)
    {
        var inPoint = animation.InPoint;
        var outPoint = animation.OutPoint;
        var dst = new SKRect(0, 0, animation.Size.Width, animation.Size.Height);

        // var bs = new List<Bitmap>();

        for (var t = inPoint; t <= outPoint; t += 1d)
        {
            var outputFileExtension = "png";
            var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}.{t:0000}.{outputFileExtension}";
            var outputFilePath = Path.Combine(outputPath, outputFileName);

            var imageInfo = new SKImageInfo((int)animation.Size.Width, (int)animation.Size.Height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using var bitmap = new SKBitmap(imageInfo);
            using var canvas = new SKCanvas(bitmap);

            animation.SeekFrame(t);
            animation.Render(canvas, dst);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var outputStream = File.Create(outputFilePath);
            data.SaveTo(outputStream);

            // var b = new Bitmap(data.AsStream());
            // bs.Add(b);
        }
    }

    private void Convert(string inputFilePath, string outputPath)
    {
        using var inputStream = File.OpenRead(inputFilePath);
        using var inputManagedStream = new SKManagedStream(inputStream);

        if (!SkiaSharp.Skottie.Animation.TryCreate(inputManagedStream, out var animation))
        {
            return;
        }

        CreatePdf(animation, inputFilePath, outputPath);

        CreateXps(animation, inputFilePath, outputPath);
        
        CreateSvg(animation, inputFilePath, outputPath);
        
        CreatePng(animation, inputFilePath, outputPath);
    }
}
