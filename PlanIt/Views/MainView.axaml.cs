using System;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using PlanIt.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.Views;

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