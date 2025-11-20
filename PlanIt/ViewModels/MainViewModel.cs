using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using MongoDB.Bson;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Initialization
    public MainViewModel()
    {
        _db = new DbAccessService();
        ViewController = new ViewController(_db);

        TaskManagerVM = new TaskManagerViewModel(_db, ViewController);
        CategoryManagerVM = new CategoryManagerViewModel(_db, ViewController);
        WindowVM = new WindowViewModel(ViewController, TaskManagerVM);
        
        CurrentViewModel = WindowVM;
        ViewController.ViewState = ViewController.ViewStates.CATEGORY;
        
        ViewController.WhenAnyValue(x => x.SelectedCategory)
            .Subscribe(_ =>
            {
                if (CurrentViewModel.GetType() != typeof(Category)) CurrentViewModel = WindowVM;
            });
        
        ViewController.WhenAnyValue(x => x.IsDarkTheme)
            .Subscribe(isDark =>
            {
                Application.Current!.RequestedThemeVariant = isDark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            });
    }
    #endregion
    
    #region Attributes
    private DbAccessService _db { get; }
    private ViewModelBase _currentViewModel;
    private FilterTodayViewModel? _filterTodayVM;
    private FilterScheduledViewModel? _filterScheduledVM;
    private FilterImportantViewModel? _filterImportantVM;
    private FilterAllViewModel? _filterAllVM;
    private SearchViewModel? _searchVM;
    
    public CategoryManagerViewModel CategoryManagerVM { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    public WindowViewModel WindowVM { get; }
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }
    public ViewController ViewController { get; }
    #endregion

    public ReactiveCommand<Unit, Unit> ShowTodayFilterView => ReactiveCommand.Create(() =>
    {
        _filterTodayVM ??= new FilterTodayViewModel(ViewController, TaskManagerVM);
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.TODAY;
        CurrentViewModel = _filterTodayVM;
        ViewController.LoadTasksForTodayAsync();
    });
    
    public ReactiveCommand<Unit, Unit> ShowScheduledFilterView => ReactiveCommand.Create(() =>
    {
        _filterScheduledVM ??= new FilterScheduledViewModel(ViewController,  TaskManagerVM);
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.SCHEDULED;
        CurrentViewModel = _filterScheduledVM;
        ViewController.LoadNodesForScheduledAsync();
    });
    
    public ReactiveCommand<Unit, Unit> ShowImportantFilterView => ReactiveCommand.Create(() =>
    {
        _filterImportantVM ??= new FilterImportantViewModel(ViewController, TaskManagerVM);
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.IMPORTANT;
        CurrentViewModel = _filterImportantVM;
        ViewController.LoadTasksForImportantAsync();
    });
    
    public ReactiveCommand<Unit, Unit> ShowAllFilterView => ReactiveCommand.Create(() =>
    {
        _filterAllVM ??= new FilterAllViewModel(ViewController, TaskManagerVM);
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.ALL;
        CurrentViewModel = _filterAllVM;
        ViewController.LoadNodesForAllAsync();
    });

    public ReactiveCommand<string, Unit> ShowSearchView => ReactiveCommand.Create<string, Unit>(searchParameter =>
    {
        _searchVM ??= new SearchViewModel(ViewController, TaskManagerVM);
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.SEARCH;
        CurrentViewModel = _searchVM;
        _searchVM.PanelSearchText = searchParameter;
        ViewController.LoadTasksForSearchAsync(searchParameter);
        return Unit.Default;
    });
    
    public ReactiveCommand<Unit, Unit> AddNewCategory => ReactiveCommand.Create(() =>
    {
        ViewController.OpenCategoryOverlay();
    });
}
