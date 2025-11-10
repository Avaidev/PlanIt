using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.ViewModels;

public class CategoryCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    private readonly DbAccessService _db;
    
    public  CategoryCreationViewModel(OverlayService overlayService, DbAccessService db)
    {
        _overlayService = overlayService;
        _db = db;
        AddCategoryInteraction = new();
        
        NewCategory = new Category{Title = "",  Icon = Icons.First(), Color = Colors.First()};
    }
    
    // ReadOnly Collections
    public ObservableCollection<string> Colors { get; } = ["Default", "Red", "Orange", "Yellow", "Pink", "Purple", "Green", "Blue", "Emerald"];

    public ObservableCollection<string> Icons { get; } =
    [
        "CubesIcon", "EnvelopeIcon", "SmileIcon", "StarIcon", "MusicIcon", "BookIcon", "CardIcon", "DollarIcon",
        "PizzaIcon", "PillsIcon", "FolderIcon", "WeatherIcon", "CarIcon", "BusIcon", "BanIcon", "AppleIcon",
        "GhostIcon", "KeyIcon"
    ];

    #region Private attributes
    private Category _newCategory;
    #endregion


    #region Public attributes
    public Interaction<Category, Unit> AddCategoryInteraction;

    public Category NewCategory
    {
        get => _newCategory;
        set =>  this.RaiseAndSetIfChanged(ref _newCategory, value);
    }
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> HideCategoryOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(0);
        NewCategory = new Category { Title = "", Icon = Icons.First(), Color = Colors.First() };
    });

    public ReactiveCommand<Category, bool> ApplyCreation => ReactiveCommand.CreateFromTask<Category, bool>(async newCategory =>
    {
        if (NewCategory.Title.Length == 0)
        {
            Console.WriteLine("[CategoryCreation] Error: Enter Title");
            return false;
        }
        var inserted = await _db.InsertCategory(newCategory);
        if (inserted)
        {
            Console.WriteLine($"[CategoryCreation] Category {newCategory.Title} has been created");
            await AddCategoryInteraction.Handle(newCategory);
        }
        else
        { 
            Console.WriteLine("[ApplyCategoryCreating] Error: Category wasn't created");
        }
        HideCategoryOverlay.Execute().Subscribe();
        return inserted;
    });
    #endregion
}
