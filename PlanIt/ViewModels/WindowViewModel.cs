using System;
using PlanIt.Services;
using PlanIt.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Styling;
using MongoDB.Bson;
using PlanIt.Services.DataServices;
using Task = System.Threading.Tasks.Task;

namespace PlanIt.ViewModels;

public class WindowViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    private readonly DbAccessService _db;
    private readonly ViewRepository _viewRepository;

    public WindowViewModel(OverlayService overlayService, DbAccessService db, ViewRepository repository)
    {
        _overlayService = overlayService;
        _db = db;
        _viewRepository =  repository;
        LoadCategoriesAsync();
        
        this.WhenAnyValue(x => x.IsDarkTheme)
            .Subscribe(isDark =>
            {
                Application.Current!.RequestedThemeVariant = isDark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            });
    }

    #region Private attributes
    private bool _isDarkTheme = true;
    private bool _panelVisible;
    private string _panelText;
    private string _panelIcon;
    private string _panelColor;
    private bool _panelPlusIsVisible;

    #endregion

    #region Public attributes

    public Category? WorkingCategory
    {
        get => _viewRepository.SelectedCategory;
        set {
            _viewRepository.SelectedCategory = value;
            if (value != null)
            {
                PanelText = value.Title;
                PanelIcon = value.Icon;
                PanelColor = value.Color;
                PanelPlusIsVisible = true;
                PanelVisible = true;
                LoadTasksByCategoryAsync();
            }
            else
            {
                PanelText = "";
                PanelIcon = "";
                PanelColor = "";
                PanelVisible = false;
            }
        }   
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _isDarkTheme, value);
    }

    public bool PanelVisible
    {
        get => _panelVisible;
        set => this.RaiseAndSetIfChanged(ref _panelVisible, value);
    }
    public string PanelText
    {
        get => _panelText;
        set => this.RaiseAndSetIfChanged(ref _panelText, value);
    }

    public string PanelIcon
    {
        get => _panelIcon;
        set => this.RaiseAndSetIfChanged(ref _panelIcon, value);
    }

    public string PanelColor
    {
        get => _panelColor;
        set => this.RaiseAndSetIfChanged(ref _panelColor, value);
    }

    public bool PanelPlusIsVisible
    {
        get => _panelPlusIsVisible;
        set => this.RaiseAndSetIfChanged(ref _panelPlusIsVisible, value);
    }
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> ShowCategoryOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(0);
    });

    public ReactiveCommand<Unit, Unit> ShowTaskOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(1);
    });

    public async Task LoadCategoriesAsync()
    {
        _viewRepository.CategoriesCollection = new ObservableCollection<Category>(await _db.GetAllCategories());
        WorkingCategory ??= _viewRepository.CategoriesCollection.First();
    }

    public async Task LoadTasksByCategoryAsync()
    {
        _viewRepository.TasksCollection = new ObservableCollection<TaskItem>(await _db.GetTasksByCategory(WorkingCategory!));
    }

    public ReactiveCommand<Category, bool> RemoveCategory => ReactiveCommand.CreateFromTask<Category, bool>(async category =>
    {
        if (await _db.CountCategories() == 1)
        {
            await MessageService.ErrorMessage("You can't delete last category!");
            
            return false;
        }
        if (!await MessageService.AskYesNoMessage($"Do you want to remove '{category.Title}' category?")) return false;
        if (category.TasksCount == 0 || category.TasksCount != 0 && await MessageService.AskYesNoMessage("To continue you must delete all tasks of this category. Would you like to delete them?"))
        {
            if (category.TasksCount != 0)
            {
                if (await _db.RemoveTasksMany(await _db.GetTasksByCategory(category)))
                    Console.WriteLine($"[WindowVM > RemoveCategory] All tasks of {category.Title} were removed");
                else return false;
            }
            
            if (await _db.RemoveCategory(category))
            {
                Console.WriteLine($"[WindowVM > RemoveCategory] {category.Title} was removed");
                _viewRepository.CategoriesCollection.Remove(category);
                return true;
            }
            Console.WriteLine($"[WindowVM > RemoveCategory] Error: {category.Title} was not removed");
        }
        return false;
    });

    public ReactiveCommand<TaskItem, bool> RemoveTask => ReactiveCommand.CreateFromTask<TaskItem, bool>( async task =>
    {
        if (!await MessageService.AskYesNoMessage($"Do you want to delete '{task.Title}' task?")) return false;
        var notificationRemove = task.Notification != null ? _db.RemoveNotification((ObjectId)task.Notification) : null;
        var taskRemove = _db.RemoveTask(task);
        WorkingCategory!.TasksCount--;
        var updateCategory = _db.UpdateCategory(WorkingCategory!);

        if (notificationRemove == null || await notificationRemove && await taskRemove && await updateCategory)
        {
            Console.WriteLine($"[WindowVM > RemoveTask] Task '{task.Title}' was removed");
            _viewRepository.TasksCollection.Remove(task);
            return true;
        }
        Console.WriteLine($"[WindowVM > RemoveTask] Error: '{task.Title}' was not removed");
        return false;
    });

    #endregion
}