using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace PlanIt.Services.DataServices;

public class DataContainer<T>
{
    public List<T> Items { get; set; } = new();

    public DataContainer(List<T> items)
    {
        Items = items;
    }
}

public class ObjectRepository<T> where T : class
{
    private readonly string _dataBasePath;
    private List<T>? _cache { get; set; } = null;
    
    public ObjectRepository(string dataBasePath)
    {
        _dataBasePath = dataBasePath;

        var directory = Path.GetDirectoryName(_dataBasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_dataBasePath))
        {
            File.Create(_dataBasePath);
        }
    }

    private async Task<List<T>> LoadAsync()
    {
        try
        {
            var bsonData = await File.ReadAllBytesAsync(_dataBasePath);
            if (bsonData.Length == 0) return [];
            var container = BsonSerializer.Deserialize<DataContainer<T>>(bsonData);
            return container.Items;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ObjectRepository > LoadAsync] Error in getting data from {_dataBasePath}: {ex.Source} - {ex.Message}");
            return [];
        }
    }
    
    private async Task<bool> SaveAsync(List<T> data)
    {
        try
        {
            var container = new DataContainer<T>(data);
            var bsonData = container.ToBson();
            await File.WriteAllBytesAsync(_dataBasePath, bsonData);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ObjectRepository > SaveAsync] Error saving data to {_dataBasePath}: {ex.Source} - {ex.Message}");
            return false;
        }
    }
    
    private ObjectId GetId(T entity)
    {
        var property = typeof(T).GetProperty("Id");
        if (property != null && property.PropertyType == typeof(ObjectId))
        {
            return  (ObjectId) property.GetValue(entity);
        }
        throw new InvalidOperationException($"[ObjectRepository > GetId] Entity {typeof(T).Name} does not have a string Id property");
    }

    private async Task<List<T>> GetEntitiesAsync(bool useCache = false)
    {
        if (!useCache)
        {
            _cache = null;
            return await LoadAsync();
        }
        _cache ??= await LoadAsync();
        return _cache;
    }

    public async Task<List<T>> GetAllAsync(bool useCache = false)
    {
        return (await GetEntitiesAsync(useCache)).ToList();
    }

    public async Task<T?> GetByIdAsync(ObjectId id, bool useCache = false)
    {
        return (await GetEntitiesAsync(useCache)).FirstOrDefault(e => GetId(e) == id);
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool useCache = false)
    {
        return (await GetEntitiesAsync(useCache)).FirstOrDefault(predicate.Compile());
    }

    public async Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, bool useCache = false)
    {
        return (await GetEntitiesAsync(useCache)).Where(predicate.Compile()).ToList();
    }

    public async Task<bool> AddAsync(T entity)
    {
        if (_cache != null)
        {

            _cache.Add(entity);
            return await SaveAsync(_cache);
        }

        var entities = await GetEntitiesAsync();
        entities.Add(entity);
        return await SaveAsync(entities);
        
    }
    
    public async Task<bool> UpdateAsync(T entity)
    {
        List<T> entities;
        if (_cache != null) entities = _cache;
        else entities = await GetEntitiesAsync();
        
        var entityId = GetId(entity);
        var toChange = entities.FirstOrDefault(e => GetId(e) == entityId);

        if (toChange != null)
        {
            var index = entities.IndexOf(toChange);
            entities.RemoveAt(index);
            entities.Insert(index, toChange);
            return await SaveAsync(entities);
        }
        Console.WriteLine($"[ObjectRepository > Add] Entity {typeof(T).Name} was not updated");
        return false;
    }
    
    public async  Task<bool> DeleteAsync(T  entity)
    {
        return await DeleteAsync(GetId(entity));
    }

    public async Task<bool> DeleteAsync(ObjectId id)
    {
        List<T> entities;
        if (_cache != null) entities = _cache;
        else entities = await GetEntitiesAsync();
        var toDelete = entities.FirstOrDefault(e => GetId(e) == id);
        if (toDelete != null)
        {
            entities.Remove(toDelete);
            return await SaveAsync(entities);
        }
        Console.WriteLine($"[ObjectRepository > Delete] Entity {typeof(T).Name} was not deleted");
        return false;
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var entities = await LoadAsync();
        return predicate == null ? entities.Count : entities.Count(predicate.Compile());
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var entities = await LoadAsync();
        return entities.Any(predicate.Compile());
    }
}