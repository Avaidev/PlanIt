using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Core.Services.DateTimeMonitor;

public class TimeObjectRepositoryAdapter<T> : IObjectRepository<ITimedObject> where T : class, ITimedObject
{
    private readonly IObjectRepository<T> _objectRepo;
    
    public TimeObjectRepositoryAdapter(IObjectRepository<T> repo)
    {
        _objectRepo = repo;
    }

    public async Task<bool> Insert(ITimedObject item)
    {
        if (item is T t)
        {
            return await _objectRepo.Insert(t);
        }
        Console.WriteLine("[TimeObjectRepositoryAdapter > Insert] Error: Cannot insert object of wrong type]");
        return false;
    }

    public async Task<bool> Remove(ITimedObject item)
    {
        if (item is T t)
        {
            return await _objectRepo.Remove(t);
        }
        Console.WriteLine("[TimeObjectRepositoryAdapter > Remove] Error: Cannot remove object of wrong type]");
        return false;
    }

    public async Task<bool> Update(ITimedObject item)
    {
        if (item is T t)
        {
            return await _objectRepo.Update(t);
        }
        Console.WriteLine("[TimeObjectRepositoryAdapter > Update] Error: Cannot update object of wrong type]");
        return false;
    }

    public async Task<bool> RemoveMany(List<ITimedObject> items)
    {
        if (items is List<T> t)
        {
            return await _objectRepo.RemoveMany(t);
        }
        Console.WriteLine("[TimeObjectRepositoryAdapter > RemoveMany] Error: Cannot remove objects of wrong type]");
        return false;
    }

    public async Task<List<ITimedObject>> GetAll()
    {
        return (await _objectRepo.GetAll()).Cast<ITimedObject>().ToList();
    }
}