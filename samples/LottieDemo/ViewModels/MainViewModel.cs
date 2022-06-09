using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LottieDemo.ViewModels;

[ObservableObject]
public partial class MainViewModel
{
    private readonly ObservableCollection<AssetViewModel> _assets;

    [ObservableProperty] private AssetViewModel? _selectedAsset;

    public MainViewModel()
    {
        var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

        var assets = assetLoader?
            .GetAssets(new Uri("avares://LottieDemo/Assets"), new Uri("avares://LottieDemo/"))
            .Where(x => x.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(x=> new AssetViewModel(Path.GetFileName(x.AbsoluteUri), x.AbsoluteUri));

        _assets = assets is not null ? new ObservableCollection<AssetViewModel>(assets) : new();

        _selectedAsset = _assets.FirstOrDefault(x => x.Path.Contains("LottieLogo1.json"));
    }

    public IReadOnlyList<AssetViewModel> Assets => _assets;

    public void Add(string path)
    {
        _assets.Add(new AssetViewModel(Path.GetFileName(path), path));
    }
}
