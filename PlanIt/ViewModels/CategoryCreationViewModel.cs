using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
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
    private readonly ViewRepository _viewRepository;
    
    
    public  CategoryCreationViewModel(OverlayService overlayService, DbAccessService db, ViewRepository repository)
    {
        _overlayService = overlayService;
        _db = db;
        _viewRepository = repository;
        ReturnToDefault();
    }

    #region Private attributes
    private Category _newCategory;
    #endregion


    #region Public attributes
    public Category NewCategory
    {
        get => _newCategory;
        set =>  this.RaiseAndSetIfChanged(ref _newCategory, value);
    }
    #endregion

    #region Commands

    private void ReturnToDefault()
    {
        NewCategory = new Category { Title = ""};
    }
    
    public ReactiveCommand<Unit, Unit> HideCategoryOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(0);
        ReturnToDefault();
    });

    private async Task<bool> CreateNewCategory(Category newCategory)
    {
        if (await _db.InsertCategory(newCategory))
        {
            Console.WriteLine($"[CategoryCreation] Category '{newCategory.Title}' was created");
            _viewRepository.CategoriesCollection.Add(newCategory);
            HideCategoryOverlay.Execute().Subscribe();
            return true;
        }
        Console.WriteLine("[CategoryCreation] Error: Category wasn't created");
        return false;
    }

    private async Task<bool> UpdateCategory(Category newCategory)
    {
        if (await _db.UpdateCategory(newCategory))
        {
            Console.WriteLine($"[CategoryCreation] Category '{newCategory.Title}' was updated");
            var index = _viewRepository.CategoriesCollection.IndexOf(newCategory);
            _viewRepository.CategoriesCollection[index] = newCategory;
            _viewRepository.SelectedCategory = newCategory;
            HideCategoryOverlay.Execute().Subscribe();
            return true;
        }
        Console.WriteLine("[CategoryCreation] Error: Category wasn't updated");
        return false;
    }

    public ReactiveCommand<Category, bool> ApplyCreation => ReactiveCommand.CreateFromTask<Category, bool>(async newCategory =>
    {
        if (NewCategory.Title.Length == 0)
        {
            await MessageService.ErrorMessage("Enter category title!");
            return false;
        }

        if (_overlayService.ToEditObject != null) return await UpdateCategory(newCategory);
        return await CreateNewCategory(newCategory);
    });
    #endregion
}
