using MongoDB.Bson;
using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;

namespace PlanIt.Data.Services;

public class CategoriesRepository : IObjectRepository<Category>
{
    private ObjectRepository<Category> _categoryRepo = new(Utils.GetFilePath("categories.bson"));
    
    #region Categories
    public async Task<Category?> GetCategoryById(ObjectId id)
    {
        return await _categoryRepo.FindAsync(c => c.Id == id);
    }
    
    public async Task ReplaceList(List<Category> categories)
    {
        await _categoryRepo.ReplaceAllAsync(categories);
    }
    public async Task<List<Category>> GetAll()
    {
        return await _categoryRepo.GetAllAsync("all");
    }

    public async Task<bool> Insert(Category category)
    {
        return await _categoryRepo.AddAsync(category);
    }

    public async Task<bool> Remove(Category category)
    {
        return await _categoryRepo.DeleteAsync(category);
    }
    
    public async Task<bool> RemoveMany(List<Category> categories)
    {
        bool allDeleted = true;
        foreach (var category in categories)
        {
            allDeleted = await Remove(category);
        }
        return allDeleted;
    }

    public async Task<bool> Update(Category category)
    {
        return await _categoryRepo.UpdateAsync(category);
    }

    public async Task<int> CountAll()
    {
        return await _categoryRepo.CountAsync();
    }
    #endregion

}