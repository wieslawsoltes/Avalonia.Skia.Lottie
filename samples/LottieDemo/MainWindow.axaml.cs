using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace LottieDemo;

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
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.FileNames))
        {
            if (DataContext is MainWindowViewModel vm)
            {
                var paths = e.Data.GetFileNames()?.ToList();
                if (paths is not null)
                {
                    for (var i = 0; i < paths.Count; i++)
                    {
                        var path = paths[i];
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            vm.Add(path);

                            if (i == 0)
                            {
                                vm.SelectedAsset = vm.Assets[^1];
                            }
                        }
                    }
                }
            }
        }
    }
}
