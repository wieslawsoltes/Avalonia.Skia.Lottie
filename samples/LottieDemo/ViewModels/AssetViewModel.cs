using CommunityToolkit.Mvvm.ComponentModel;

namespace LottieDemo.ViewModels;

[ObservableObject]
public partial class AssetViewModel
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _path;

    public AssetViewModel(string name, string path)
    {
        _name = name;
        _path = path;
    }

    public override string ToString() => _name;
}
