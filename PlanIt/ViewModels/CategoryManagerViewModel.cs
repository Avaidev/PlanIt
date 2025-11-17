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

public class CategoryManagerViewModel : ViewModelBase
{
    #region Private attributes
    private Category _newCategory;
    private readonly DbAccessService _db;
    #endregion
    
    #region Public attributes
    public Category NewCategory
    {
        get => _newCategory;
        set =>  this.RaiseAndSetIfChanged(ref _newCategory, value);
    }
    public ViewRepository ViewRepository { get; }
    public OverlayService OverlayService { get; }
    #endregion
    
    public  CategoryManagerViewModel(OverlayService overlayService, DbAccessService db, ViewRepository repository)
    {
        OverlayService = overlayService;
        _db = db;
        ViewRepository = repository;
        ReturnToDefault();
    }
    
    private void ReturnToDefault()
    {
        NewCategory = new Category { Title = "" };
    }

    public ReactiveCommand<Category, bool> RemoveCategory => ReactiveCommand.CreateFromTask<Category,bool>(async category =>
    {
        if (ViewRepository.CategoriesCollection.Count == 1)
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
                if (await _db.RemoveTasksMany(tasks))
                {
                    ViewRepository.RemovingTasksMany(tasks);
                    Console.WriteLine($"[WindowVM > RemoveCategory] All tasks of {category.Title} were removed");
                }
                else return false;
            }
            
            if (await _db.RemoveCategory(category))
            {
                Console.WriteLine($"[WindowVM > RemoveCategory] {category.Title} was removed");
                ViewRepository.CategoriesCollection.Remove(category);
                if (ViewRepository.SelectedCategory != null && ViewRepository.SelectedCategory.Equals(category)) ViewRepository.SelectedCategory = null;
                return true;
            }
            Console.WriteLine($"[WindowVM > RemoveCategory] Error: {category.Title} was not removed");
        }
        return false;
    });

    public ReactiveCommand<Category, Unit> EditCategory => ReactiveCommand.Create<Category, Unit>(category =>
    {
        NewCategory = new Category(category);
        OverlayService.ToggleVisibility(0, true);
        return Unit.Default;
    });
    
    public ReactiveCommand<Unit, Unit> HideCategoryOverlay => ReactiveCommand.Create(() =>
    {
        OverlayService.ToggleVisibility(0);
        ReturnToDefault();
    });
    private async Task<bool> CreateNewCategory(Category newCategory)
    {
        if (await _db.InsertCategory(newCategory))
        {
            Console.WriteLine($"[CategoryCreation] Category '{newCategory.Title}' was created");
            ViewRepository.CategoriesCollection.Add(newCategory);
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
            var index = ViewRepository.CategoriesCollection.IndexOf(newCategory);
            ViewRepository.CategoriesCollection[index] = newCategory;
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

        if (OverlayService.EditMode) return await UpdateCategory(newCategory);
        return await CreateNewCategory(newCategory);
    });
}
