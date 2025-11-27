using Avalonia;
using PlanIt.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.UI.Views;

public partial class MainView : ReactiveWindow<MainViewModel>

{
    public MainView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
#if DEBUG
            this.AttachDevTools();
#endif
        });
    }
}