using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace PlanIt.UI.Services;
public static class ThemeBindingRefreshService
{
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        
    }

    public static void RefreshAllActiveWindows()
    {
        foreach (var window in GetActiveWindows())
        {
            RefreshVisualTreeBindings(window);
        }
    }

    private static IEnumerable<Window> GetActiveWindows()
    {
        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            if (singleView.MainView is Window mainWindow)
                yield return mainWindow;
        }
        else if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                yield return window;
            }
        }
    }

    public static void RefreshVisualTreeBindings(Visual? visual)
    {
        if (visual == null) return;

        RefreshControlBindings(visual as StyledElement);

        foreach (var child in visual.GetVisualChildren())
        {
            RefreshVisualTreeBindings(child);
        }
    }

    private static void RefreshControlBindings(StyledElement? control)
    {
        if (control == null) return;

        try
        {
            var originalDataContext = control.DataContext;
            control.DataContext = null;
            control.DataContext = originalDataContext;

            if (control is Layoutable layoutable)
            {
                layoutable.InvalidateMeasure();
                layoutable.InvalidateArrange();
            }

            if (control is ItemsControl itemsControl)
            {
                RefreshItemsControlBindings(itemsControl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing bindings for {control.GetType().Name}: {ex.Message}");
        }
    }

    private static void RefreshItemsControlBindings(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items == null) return;
        itemsControl.InvalidateMeasure();
        itemsControl.InvalidateArrange();
    }
}