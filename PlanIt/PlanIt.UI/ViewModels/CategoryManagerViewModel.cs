using System;
using System.Reactive;
using System.Threading.Tasks;
using MongoDB.Bson;
using PlanIt.Core.Models;
using PlanIt.Core.Services;
using PlanIt.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class CategoryManagerViewModel : ViewModelBase
{
    #region Initialization
    public  CategoryManagerViewModel(DbAccessService db, ViewController controller)
    {
        _db = db;
        ViewController = controller;
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
    private readonly DbAccessService _db;
    
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
                var tasks = await _db.GetTasksByCategory(category);
                foreach (var task in tasks)
                {
                    if (task.Notification != null) await _db.RemoveNotification((ObjectId)task.Notification);
                }
                if (await _db.RemoveTasksMany(tasks))
                {
                    ViewController.AfterRemovingTasksMany(tasks);
                    Console.WriteLine($"[CategoryManager > RemoveCategory] All tasks of {category.Title} were removed");
                }
                else return false;
            }
            
            if (await _db.RemoveCategory(category))
            {
                Console.WriteLine($"[CategoryManager > RemoveCategory] {category.Title} was removed");
                ViewController.RemoveCategoryFromView(category);
                return true;
            }
            Console.WriteLine($"[WindowVM > RemoveCategory] Error: {category.Title} was not removed");
            await MessageService.ErrorMessage($"Error: {category.Title} was not removed");
        }
        return false;
    });

    public ReactiveCommand<Category, bool> EditCategory => ReactiveCommand.Create<Category, bool>(category =>
    {
        NewCategory = new Category(category);
        _editMode = true;
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
        if (await _db.InsertCategory(newCategory))
        {
            Console.WriteLine($"[CategoryManager > CreateNew] Category '{newCategory.Title}' was created");
            ViewController.AddCategoryToView(newCategory);
            HideOverlay.Execute().Subscribe();
            return true;
        }
        Console.WriteLine("[CategoryManager > CreateNew] Error: Category wasn't created");
        await MessageService.ErrorMessage($"Error: {newCategory.Title} was not created");
        return false;
    }

    private async Task<bool> Update(Category newCategory)
    {
        if (await _db.UpdateCategory(newCategory))
        {
            Console.WriteLine($"[CategoryManager > Update] Category '{newCategory.Title}' was updated");
            ViewController.ChangeCategoryInView(newCategory);
            HideOverlay.Execute().Subscribe();
            return true;
        }
        Console.WriteLine("[CategoryManager > Update] Error: Category wasn't updated");
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
