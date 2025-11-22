namespace PlanIt.Data.Interfaces;

public interface IObjectRepository<T> where T : class
{
    Task<bool> Insert(T item);
    Task<bool> Remove(T item);
    Task<bool> Update(T item);
    Task<bool> RemoveMany(List<T> items);
    Task<List<T>> GetAll();
}