using System;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using PlanIt.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>

{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.Bind(ViewModel,
                    vm => vm.TaskCreationVM.SelectedDatePart,
                    view => view.TaskDatePicker.SelectedDate)
                .DisposeWith(disposable);
            
            this.Bind(ViewModel,
                vm => vm.TaskCreationVM.SelectedTimePart,
                view => view.TaskTimePicker.SelectedTime)
                .DisposeWith(disposable);
            
#if DEBUG
            this.AttachDevTools();
#endif
        });
    }
}