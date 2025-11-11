using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using PlanIt.Models;
using ReactiveUI;

namespace PlanIt.Services;

public class ViewRepository : ReactiveObject
{
    private ObservableCollection<Category> _categoriesCollection;
    private ObservableCollection<TaskItem> _tasksCollection;
    private Category? _selectedCategory;
    
    public ObservableCollection<Category> CategoriesCollection
    {
        get => _categoriesCollection;
        set => this.RaiseAndSetIfChanged(ref _categoriesCollection, value);
    }

    public ObservableCollection<TaskItem> TasksCollection
    {
        get => _tasksCollection;
        set => this.RaiseAndSetIfChanged(ref _tasksCollection, value);
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }
    
    public ObservableCollection<string> Colors { get; } = ["Default", "Red", "Orange", "Yellow", "Pink", "Purple", "Green", "Blue", "Emerald"];

    public ObservableCollection<string> Icons { get; } =
    [
        "CubesIcon", "EnvelopeIcon", "SmileIcon", "StarIcon", "MusicIcon", "BookIcon", "CardIcon", "DollarIcon",
        "PizzaIcon", "PillsIcon", "FolderIcon", "WeatherIcon", "CarIcon", "BusIcon", "BanIcon", "AppleIcon",
        "GhostIcon", "KeyIcon"
    ];

    public ObservableCollection<string> RepeatComboCollection { get; } =
    [
        "Don't repeat", "Every day", "Every 2 days", "Every 5 days", "Every week", "Every month", "Every year"
    ];
    public List<int?> RepeatComboValues { get; } = [null, 1, 2, 5, 7, 30, 365];

    public ObservableCollection<string> NotifyBeforeComboCollection { get; } =
    [
        "Don't notify", "1 hour before", "3 hours before", "1 day before", "2 days before", "5 days before", "week before"
    ];
    public List<int?> NotifyBeforeComboValues { get; } = [null, -1, -3, 1, 2, 5, 7];

    public ObservableCollection<string> ImportanceComboCollection { get; } =
    [
        "Not important", "Important"
    ];

    public List<bool> ImportanceComboValues { get; } = [false, true];
}