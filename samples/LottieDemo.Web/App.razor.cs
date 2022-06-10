using Avalonia.Web.Blazor;

namespace LottieDemo.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<LottieDemo.App>()
            .SetupWithSingleViewLifetime();
    }
}
