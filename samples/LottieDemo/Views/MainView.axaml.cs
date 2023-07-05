﻿using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using LottieDemo.ViewModels;

namespace LottieDemo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var paths = e.Data.GetFileNames()?.ToList();
        if (paths is null)
        {
            return;
        }

        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            vm.Add(path);

            if (i == 0)
            {
                vm.SelectedAsset = vm.Assets[^1];
            }
        }
    }
}
