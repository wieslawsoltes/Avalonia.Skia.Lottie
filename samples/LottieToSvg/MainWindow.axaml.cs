using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        var paths = e.Data.GetFileNames()?.ToList();
        if (paths is null || paths.Count <= 0)
        {
            return;
        }

        var outputPath = OutputPath.Text;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                outputPath = "c:\\Temp\\";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                outputPath = "/Users/wieslawsoltes/Downloads/Temp";
            }
        }

        if (outputPath is null)
        {
            return;
        }
        
        await Task.Run(() =>
        {
            foreach (var path in paths)
            {
                Convert(path, outputPath);
            }
        });
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

    private void Convert(string inputFilePath, string outputPath)
    {
        using var inputStream = File.OpenRead(inputFilePath);
        using var inputManagedStream = new SKManagedStream(inputStream);

        if (!SkiaSharp.Skottie.Animation.TryCreate(inputManagedStream, out var animation))
        {
            return;
        }

        CreateSvg(animation, inputFilePath, outputPath);
    }
}
