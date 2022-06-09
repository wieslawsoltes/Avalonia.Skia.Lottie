using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LottieDemo.ViewModels;

[ObservableObject]
public partial class MainViewModel
{
    private readonly ObservableCollection<string> _assets;

    [ObservableProperty] private string? _selectedAsset;

    public MainViewModel()
    {
        var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

        var assets = assetLoader?
            .GetAssets(new Uri("avares://LottieDemo/Assets"), new Uri("avares://LottieDemo/"))
            .Where(x => x.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(x=> x.AbsoluteUri);

        _assets = assets is not null ? new ObservableCollection<string>(assets) : new();

        _selectedAsset = _assets.FirstOrDefault(x => x.Contains("LottieLogo1.json"));
    }

    public IReadOnlyList<string> Assets => _assets;

    public void Add(string path)
    {
        _assets.Add(path);
    }
}
