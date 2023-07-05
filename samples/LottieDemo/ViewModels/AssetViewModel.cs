using CommunityToolkit.Mvvm.ComponentModel;

namespace LottieDemo.ViewModels;

public partial class AssetViewModel : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _path;

    public AssetViewModel(string name, string path)
    {
        _name = name;
        _path = path;
    }

    public override string ToString() => Name;
}
