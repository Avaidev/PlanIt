using System.Reactive.Disposables.Fluent;
using PlanIt.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.Views;

public partial class TaskCreationView : ReactiveUserControl<TaskManagerViewModel>
{
    public TaskCreationView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.Bind(ViewModel,
                    vm => vm.SelectedDatePart,
                    view => view.TaskDatePicker.SelectedDate)
                .DisposeWith(disposable);
            
            this.Bind(ViewModel,
                    vm => vm.SelectedTimePart,
                    view => view.TaskTimePicker.SelectedTime)
                .DisposeWith(disposable);
        });
    }
}