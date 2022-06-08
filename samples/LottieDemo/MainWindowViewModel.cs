using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace LottieDemo;

[ObservableObject]
public partial class MainWindowViewModel
{
    private readonly List<string>? _assets;

    [ObservableProperty] private string? _selectedAsset;

    public MainWindowViewModel()
    {
        var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

        _assets = assetLoader?
            .GetAssets(new Uri("avares://LottieDemo/Assets"), new Uri("avares://LottieDemo/"))
            .Select(x=>x.AbsoluteUri)
            .ToList();

        _selectedAsset = _assets?.FirstOrDefault(x => x.Contains("LottieLogo1.json"));
    }

    public IReadOnlyList<string>? Assets => _assets;
}
