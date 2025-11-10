using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace PlanIt.Services.DataServices;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(bool useCache = false);
    Task<T?> GetByIdAsync(ObjectId id, bool  useCache = false);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool useCache = false);
    Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, bool useCache = false);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(ObjectId id);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}