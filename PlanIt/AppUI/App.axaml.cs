using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanIt.Core.Services;
using PlanIt.Core.Services.Pipe;
using PlanIt.UI.Services;
using PlanIt.UI.ViewModels;
using MainView = PlanIt.UI.Views.MainView;

namespace PlanIt.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
        });

        collection.AddSingleton<DataAccessService>();
        collection.AddSingleton<ViewController>();
        collection.AddSingleton<NavigationService>();
        collection.AddSingleton<PipeClientController>();
        collection.AddSingleton<BackgroundController>();
        collection.AddSingleton<TaskManagerViewModel>();
        collection.AddSingleton<CategoryManagerViewModel>();
        collection.AddSingleton<MainViewModel>();
        
        collection.AddTransient<WindowViewModel>();
        collection.AddTransient<FilterAllViewModel>();
        collection.AddTransient<FilterCompletedViewModel>();
        collection.AddTransient<FilterScheduledViewModel>();
        collection.AddTransient<FilterTodayViewModel>();
        collection.AddTransient<SearchViewModel>();
        
        var services = collection.BuildServiceProvider();
        var mainVm = services.GetRequiredService<MainViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainView
            {
                DataContext = mainVm
            };
            
            desktop.ShutdownRequested += OnShutdownRequested;
            
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.ShutDown();
        }
    }
    
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}