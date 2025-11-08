using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using PlanIt.Models;
using PlanIt.Services;
using ReactiveUI;

namespace PlanIt.ViewModels;

public class CategoryCreationViewModel : ViewModelBase
{
    // BackEnd
    private readonly OverlayService _overlayService;
    private readonly DataAccess _db;
    
    public  CategoryCreationViewModel(OverlayService overlayService, DataAccess db)
    {
        _overlayService = overlayService;
        _db = db;
        SelectedColor = Colors.First();
        SelectedIcon = Icons.First();
        EnteredTitle = "";
    }
    
    // ReadOnly Collections
    public ObservableCollection<string> Colors { get; } = ["Default", "Red", "Orange", "Yellow", "Pink", "Purple", "Green", "Blue", "Emerald"];

    public ObservableCollection<string> Icons { get; } =
    [
        "CubesIcon", "EnvelopeIcon", "SmileIcon", "StarIcon", "MusicIcon", "BookIcon", "CardIcon", "DollarIcon",
        "PizzaIcon", "PillsIcon", "FolderIcon", "WeatherIcon", "CarIcon", "BusIcon", "BanIcon", "AppleIcon",
        "GhostIcon", "KeyIcon"
    ];
    
    // Control attributes
    //      private
    private string _selectedColor;
    private string _selectedIcon;
    private string _enteredTitle;
    
    
    //      public
    public string SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }
    public string SelectedIcon
    {
        get => _selectedIcon;
        set => this.RaiseAndSetIfChanged(ref _selectedIcon, value);
    }

    public string EnteredTitle
    {
        get => _enteredTitle;
        set => this.RaiseAndSetIfChanged(ref _enteredTitle, value);
    }
    

    // Commands
    public ReactiveCommand<Unit, Unit> HideCategoryOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(0);
        SelectedColor = Colors.First();
        SelectedIcon = Icons.First();
        EnteredTitle = "";
    });

    public ReactiveCommand<Unit, Unit> ApplyCreation => ReactiveCommand.Create(() =>
    {
        if (EnteredTitle.Length == 0)
        {
            Console.WriteLine("Enter Title");
            return;
        }
        var newCategory = new Category{Title = EnteredTitle, Color = SelectedColor, Icon = SelectedIcon};
        _db.Categories.InsertOne(newCategory);
        Console.WriteLine($"[CategoryCreation] Category {newCategory.Title} has been created");
        _overlayService.CreatedCategory =  newCategory;
        HideCategoryOverlay.Execute().Subscribe();
    });
}