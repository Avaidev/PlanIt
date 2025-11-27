using System;
using System.Reactive.Disposables.Fluent;
using PlanIt.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.UI.Views;

public partial class TaskCreationView : ReactiveUserControl<TaskManagerViewModel>
{
    public TaskCreationView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.Bind<TaskManagerViewModel, TaskCreationView, DateTimeOffset, DateTimeOffset?>(ViewModel,
                    vm => vm.SelectedDatePart,
                    view => view.TaskDatePicker.SelectedDate)
                .DisposeWith(disposable);
            
            this.Bind<TaskManagerViewModel, TaskCreationView, TimeSpan, TimeSpan?>(ViewModel,
                    vm => vm.SelectedTimePart,
                    view => view.TaskTimePicker.SelectedTime)
                .DisposeWith(disposable);
        });
    }
}