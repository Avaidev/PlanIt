using System;
using Microsoft.Extensions.DependencyInjection;
using PlanIt.UI.ViewModels;

namespace PlanIt.UI.Services;

public class NavigationService
{
    #region Initialization
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    #endregion
    
    #region Attributes
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentViewModel;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            ViewModelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ViewModelChanged;
    #endregion

    public void NavigateTo<T>() where T : ViewModelBase
    {
        CurrentViewModel = _serviceProvider.GetRequiredService<T>();
    }
    
}