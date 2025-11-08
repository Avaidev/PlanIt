using System;
using PlanIt.Services;
using PlanIt.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PlanIt.ViewModels;

public class WindowViewModel : ViewModelBase
{
    // BackEnd
    
    public class Panel
    {
        public required string Text { get; set; }
        public required string Color { get; set; }
        public required string Icon { get; set; }
        public required bool PlusIsVisible { get; set; }
    }
    
    private readonly OverlayService _overlayService;
    private readonly DataAccess _db;

    public WindowViewModel(OverlayService overlayService, DataAccess db)
    {
        _overlayService = overlayService;
        _db = db;
        Categories = new ObservableCollection<Category>(_db.Categories.Find(_db.GetAllFilter<Category>()).ToList());
        Category = Categories.FirstOrDefault(new Category{Title = "Default"});
        PanelElement = new Panel{Text = Category.Title, Color = Category.Color, Icon = Category.Icon, PlusIsVisible = true};
        
        this.WhenAnyValue(x => x.IsDarkTheme)
            .Subscribe(isDark =>
            {
                Application.Current!.RequestedThemeVariant = isDark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            });

        this.WhenAnyValue(x => x._overlayService.CreatedCategory)
            .Subscribe(createdCategory =>
            {
                if (createdCategory == null) return;
                Categories.Add(createdCategory);
                _overlayService.CreatedCategory = null;
            });
    }
    
    // Control attributes
    //      private
    private Category _category;
    private ObservableCollection<Category> _categories;
    private bool _isDarkTheme = true;
    
    //      public
    public Category Category
    {
        get => _category;
        set => this.RaiseAndSetIfChanged(ref _category, value);
    }
    public ObservableCollection<Category> Categories
    {
        get  => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _isDarkTheme, value);
    }
    public Panel PanelElement { get; set; }
    
    // Commands
    public ReactiveCommand<Unit, Unit> ShowCategoryOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(0);
    });

    public ReactiveCommand<Unit, Unit> ShowTaskOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(1);
    });
}