using System;
using PlanIt.Services;
using PlanIt.Models;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using MongoDB.Bson;
using PlanIt.Services.DataServices;

namespace PlanIt.ViewModels;

public class WindowViewModel : ViewModelBase
{
    #region Initialization
    public WindowViewModel(ViewController controller, TaskManagerViewModel taskManagerViewModel)
    {
        ViewController = controller;
        TaskManagerVM = taskManagerViewModel;
    }
    #endregion
    
    #region Attributes
    public TaskManagerViewModel TaskManagerVM { get; }
    public ViewController ViewController { get;  }
    #endregion
    
    public ReactiveCommand<Unit, Unit> AddNewTask => ReactiveCommand.Create(() =>
    {
        ViewController.OpenTaskOverlay();
        TaskManagerVM.SetStartParameters(category: ViewController.SelectedCategory);
    });
}