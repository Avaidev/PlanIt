using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using PlanIt.Core.Services;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Models;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Initialization
    public MainViewModel(
        BackgroundController backgroundController,
        ViewController viewController,
        DataAccessService dataAccessService,
        NavigationService navigationService,
        TaskManagerViewModel taskManagerVM,
        CategoryManagerViewModel  categoryManagerVM)
    {
        _backgroundController = backgroundController;
        _db = dataAccessService;
        _navigationService = navigationService;
        _navigationService.ViewModelChanged += (sender, args) => this.RaisePropertyChanged(nameof(CurrentViewModel));
        ViewController = viewController;

        TaskManagerVM = taskManagerVM;
        CategoryManagerVM = categoryManagerVM;
        
        _ = InitializeAsync();
        
        ThemeBindingRefreshService.Initialize();
        
        //Binds
        ViewController.WhenAnyValue(x => x.SelectedCategory)
            .Subscribe(_ =>
            {
                if (CurrentViewModel?.GetType() != typeof(Category)) _navigationService.NavigateTo<WindowViewModel>();
            });
        
        ViewController.WhenAnyValue(x => x.IsDarkTheme)
            .Subscribe(isDark =>
            {
                Application.Current!.RequestedThemeVariant = isDark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
                
                ThemeBindingRefreshService.RefreshAllActiveWindows();
            });
    }

    private async Task InitializeAsync()
    {
        ViewController.LoadingMessage = "Connecting...";
        await _backgroundController.Connect();
        await ViewController.InitializeAsync();
        await Task.Delay(500);
        ViewController.IsLoadingVisible = false;
        ViewController.ViewState = ViewController.ViewStates.CATEGORY;
        _navigationService.NavigateTo<WindowViewModel>();
    }
    #endregion
    
    #region Attributes
    private DataAccessService _db { get; }
    private readonly BackgroundController _backgroundController;
    private readonly NavigationService _navigationService;
    public ViewController ViewController { get; }
    public CategoryManagerViewModel CategoryManagerVM { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    public ViewModelBase? CurrentViewModel => _navigationService.CurrentViewModel;
    #endregion

    public ReactiveCommand<Unit, Unit> ShowTodayFilterView => ReactiveCommand.Create(() =>
    {
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.TODAY;
        _navigationService.NavigateTo<FilterTodayViewModel>();
        _ = ViewController.LoadTasksForTodayAsync();
    });
    
    public ReactiveCommand<Unit, Unit> ShowScheduledFilterView => ReactiveCommand.Create(() =>
    {
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.SCHEDULED;
        _navigationService.NavigateTo<FilterScheduledViewModel>();
        _ = ViewController.LoadNodesForScheduledAsync();

    });
    
    public ReactiveCommand<Unit, Unit> ShowCompletedFilter => ReactiveCommand.Create(() =>
    {
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.COMPLETED;
        _navigationService.NavigateTo<FilterCompletedViewModel>();
        _ = ViewController.LoadTasksForCompletedAsync();
        
    });
    
    public ReactiveCommand<Unit, Unit> ShowAllFilterView => ReactiveCommand.Create(() =>
    {
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.ALL;
        _navigationService.NavigateTo<FilterAllViewModel>();
        _ = ViewController.LoadNodesForAllAsync();
        

    });

    public ReactiveCommand<string, Unit> ShowSearchView => ReactiveCommand.Create<string, Unit>(searchParameter =>
    {
        ViewController.SelectedCategory = null;
        ViewController.ViewState = ViewController.ViewStates.SEARCH;
        _navigationService.NavigateTo<SearchViewModel>();
        ViewController.SearchParameter = searchParameter;
        _ = ViewController.LoadTasksForSearchAsync(searchParameter);
        return Unit.Default;
    });
    
    public ReactiveCommand<Unit, Unit> AddNewCategory => ReactiveCommand.Create(() =>
    {
        ViewController.OpenCategoryOverlay();
    });

    public void ShutDown()
    {
        _backgroundController.StopConnection();
        _backgroundController.Dispose();
    }
}
