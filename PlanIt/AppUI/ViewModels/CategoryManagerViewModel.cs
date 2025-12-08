using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Data.Models;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class CategoryManagerViewModel : ViewModelBase
{
    #region Initialization
    public  CategoryManagerViewModel(DataAccessService db, ViewController controller, ILogger<CategoryManagerViewModel> logger, BackgroundController backgroundController)
    {
        _logger = logger;
        _db = db;
        ViewController = controller;
        _backgroundController = backgroundController;
        ReturnToDefault();
    }
    
    private void ReturnToDefault()
    {
        NewCategory = new Category { Title = "" };
        _editMode = false;
    }
    #endregion
    
    #region Attributes
    private Category _newCategory;
    private readonly ILogger<CategoryManagerViewModel> _logger;
    private readonly DataAccessService _db;
    private readonly BackgroundController _backgroundController;
    
    private bool _editMode;
    
    public Category NewCategory
    {
        get => _newCategory;
        set =>  this.RaiseAndSetIfChanged(ref _newCategory, value);
    }
    public ViewController ViewController { get; }
    #endregion

    public ReactiveCommand<Category, bool> RemoveCategory => ReactiveCommand.CreateFromTask<Category,bool>(async category =>
    {
        if (ViewController.CategoriesCollection.Count == 1)
        {
            await MessageService.ErrorMessage("You can't delete last category!");
            return false;
        }
        if (!await MessageService.AskYesNoMessage($"Do you want to remove '{category.Title}' category?")) return false;
        if (category.TasksCount == 0 || category.TasksCount != 0 && await MessageService.AskYesNoMessage("To continue you must delete all tasks of this category. Would you like to delete them?"))
        {
            if (category.TasksCount != 0)
            {
                var tasks = await _db.Tasks.GetTasksByCategory(category);
                if (await _db.Tasks.RemoveMany(tasks))
                {
                    foreach (var task in tasks)
                    {
                        await _backgroundController.SendData(task.Id.ToByteArray(), 0);
                        ViewController.DecreaseFiltersNumbers(task);
                        await Task.Delay(100);
                    }
                    Console.WriteLine($"[CategoryManager > RemoveCategory] All tasks of {category.Title} were removed");
                }
                else return false;
            }
            
            if (await _db.Categories.Remove(category))
            {
                _logger.LogInformation("[CategoryManager > RemoveCategory] {CategoryTitle} was removed", category.Title);
                ViewController.RemoveCategoryFromView(category);
                return true;
            }
            _logger.LogError("[WindowVM > RemoveCategory] Error: {CategoryTitle} was not removed", category.Title);
            await MessageService.ErrorMessage($"Error: {category.Title} was not removed");
        }
        return false;
    });

    public ReactiveCommand<Category, bool> EditCategory => ReactiveCommand.Create<Category, bool>(category =>
    {
        NewCategory = new Category(category);
        _editMode = true;
        ViewController.CreateWindowTitle = "Edit Category";
        ViewController.OpenCategoryOverlay();
        return true;
    });
    
    public ReactiveCommand<Unit, Unit> HideOverlay => ReactiveCommand.Create(() =>
    {
        ViewController.CloseCategoryOverlay();
        ReturnToDefault();
    });
    
    private async Task<bool> Create(Category newCategory)
    {
        if (await _db.Categories.Insert(newCategory))
        {
            _logger.LogInformation("[CategoryManager > CreateNew] Category '{NewCategoryTitle}' was created", newCategory.Title);
            ViewController.AddCategoryToView(newCategory);
            HideOverlay.Execute().Subscribe();
            return true;
        }
        _logger.LogError("[CategoryManager > CreateNew] Error: Category '{NewCategoryTitle} wasn't created", newCategory.Title);
        await MessageService.ErrorMessage($"Error: {newCategory.Title} was not created");
        return false;
    }

    private async Task<bool> Update(Category newCategory)
    {
        if (await _db.Categories.Update(newCategory))
        {
            _logger.LogInformation("[CategoryManager > Update] Category '{NewCategoryTitle}' was updated", newCategory.Title);
            ViewController.ChangeCategoryInView(newCategory);
            HideOverlay.Execute().Subscribe();
            return true;
        }
        _logger.LogError("[CategoryManager > Update] Error: Category '{NewCategoryTitle} wasn't updated", newCategory.Title);
        await MessageService.ErrorMessage($"Error: {newCategory.Title} was not updated");
        return false;
    }

    public ReactiveCommand<Category, bool> ApplyCreation => ReactiveCommand.CreateFromTask<Category, bool>(async newCategory =>
    {
        if (NewCategory.Title.Length == 0)
        {
            await MessageService.ErrorMessage("Enter category title!");
            return false;
        }

        if (_editMode) return await Update(newCategory);
        return await Create(newCategory);
    });
}
