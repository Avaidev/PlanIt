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
    #region Private attributes
    private ViewModelBase _currentViewModel;
    private FilterTodayViewModel? _filterTodayVM;
    private FilterScheduledViewModel? _filterScheduledVM;
    private FilterImportantViewModel? _filterImportantVM;
    private FilterAllViewModel? _filterAllVM;
    
    private DbAccessService _db { get; }
    #endregion
    
    #region Public attributes
    public CategoryManagerViewModel CategoryManagerVM { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    public WindowViewModel WindowVM { get; }
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public OverlayService OverlayService { get; }
    public ViewRepository ViewRepository { get; }
    #endregion
    
    public MainViewModel()
    {
        _db = new DbAccessService();
        OverlayService = new OverlayService();
        ViewRepository = new ViewRepository(_db);

        TaskManagerVM = new TaskManagerViewModel(OverlayService, _db, ViewRepository);
        CategoryManagerVM = new CategoryManagerViewModel(OverlayService, _db, ViewRepository);
        WindowVM = new WindowViewModel(OverlayService, _db, ViewRepository, TaskManagerVM);
        
        CurrentViewModel = WindowVM;
        
        ViewRepository.WhenAnyValue(x => x.SelectedCategory)
            .Subscribe(_ =>
            {
                if (CurrentViewModel.GetType() != typeof(Category)) CurrentViewModel = WindowVM;
            });
        
        ViewRepository.WhenAnyValue(x => x.IsDarkTheme)
            .Subscribe(isDark =>
            {
                Application.Current!.RequestedThemeVariant = isDark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            });
    }

    #region Commands
    public ReactiveCommand<Unit, Unit> ShowTodayFilterView => ReactiveCommand.Create(() =>
    {
        _filterTodayVM ??= new FilterTodayViewModel();
        ViewRepository.SelectedCategory = null;
        CurrentViewModel = _filterTodayVM;
    });
    
    public ReactiveCommand<Unit, Unit> ShowScheduledFilterView => ReactiveCommand.Create(() =>
    {
        _filterScheduledVM ??= new FilterScheduledViewModel();
        ViewRepository.SelectedCategory = null;
        CurrentViewModel = _filterScheduledVM;
    });
    
    public ReactiveCommand<Unit, Unit> ShowImportantFilterView => ReactiveCommand.Create(() =>
    {
        _filterImportantVM ??= new FilterImportantViewModel();
        ViewRepository.SelectedCategory = null;
        CurrentViewModel = _filterImportantVM;
    });
    
    public ReactiveCommand<Unit, Unit> ShowAllFilterView => ReactiveCommand.Create(() =>
    {
        _filterAllVM ??= new FilterAllViewModel(ViewRepository, _db, OverlayService, TaskManagerVM);
        ViewRepository.SelectedCategory = null;
        CurrentViewModel = _filterAllVM;
        ViewRepository.LoadNodesForAllAsync();
    });
    
    public ReactiveCommand<Unit, Unit> ShowCategoryOverlay => ReactiveCommand.Create(() =>
    {
        OverlayService.ToggleVisibility(0);
    });
    #endregion
}
