using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LottieDemo.ViewModels;

[ObservableObject]
public partial class MainViewModel
{
    private readonly ObservableCollection<AssetViewModel> _assets;

    [ObservableProperty] private AssetViewModel? _selectedAsset;
    [ObservableProperty] private bool _enableCheckerboard;

    public MainViewModel()
    {
        var assets = AssetLoader
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

    [RelayCommand]
    private void Previous()
    {
        if (_selectedAsset is { } && _assets.Count > 1)
        {
            var index = _assets.IndexOf(_selectedAsset);
            if (index == 0)
            {
                SelectedAsset = _assets[_assets.Count - 1];
            }
            else if (index > 0)
            {
                SelectedAsset = _assets[index - 1];
            }
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (_selectedAsset is { } && _assets.Count > 1)
        {
            var index = _assets.IndexOf(_selectedAsset);
            if (index == _assets.Count - 1)
            {
                SelectedAsset = _assets[0];
            }
            else if (index >= 0 && index < _assets.Count - 1)
            {
                SelectedAsset = _assets[index + 1];
            }
        }
    }
}
