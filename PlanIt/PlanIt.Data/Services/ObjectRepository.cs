using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace PlanIt.Data.Services;

public class DataContainer<T>
{
    public List<T> Items { get; set; } = new();

    public DataContainer(){}
    public DataContainer(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}

public class ObjectRepository<T>
{
    #region Initialization
    public ObjectRepository(string dataBasePath)
    {
        _dbPath = dataBasePath;
        _cache = new Dictionary<string, (DateTime timestamp, IEnumerable<T> data)>();
        
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_dbPath))
        {
            File.Create(_dbPath);
        }
    }
    #endregion

    #region Attributes
    private readonly string _dbPath;
    private Dictionary<string, (DateTime timestamp, IEnumerable<T> data)> _cache;
    private const int CACHE_LIFETIME = 5;
    #endregion

    private async Task<List<T>> LoadFromDbAsync()
    {
        try
        {
            var bsonData = await File.ReadAllBytesAsync(_dbPath);
            if (bsonData.Length == 0) return [];
            var container = BsonSerializer.Deserialize<DataContainer<T>>(bsonData);
            
            return container.Items;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ObjectRepository > LoadFromDbAsync] Error in getting data from {_dbPath}: {ex.Source} - {ex.Message}");
            return [];
        }
    }

    private async Task<bool> SaveToDbAsync(IEnumerable<T> data)
    {
        try
        {
            var container = new DataContainer<T>(data);
            var bsonData = container.ToBson();
            await File.WriteAllBytesAsync(_dbPath, bsonData);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ObjectRepository > SaveAsync] Error saving data to {_dbPath}: {ex.Source} - {ex.Message}");
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

    private IEnumerable<T>? GetFromCache(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.timestamp < TimeSpan.FromMinutes(CACHE_LIFETIME))
        {
            return cached.data;
        }
        return null;
    }

    private void CleanOldCacheEntries()
    {
        var expired = _cache.Where(pair => DateTime.UtcNow - pair.Value.timestamp > TimeSpan.FromMinutes(CACHE_LIFETIME * 2))
            .Select(pair => pair.Key).ToList();

        foreach (var key in expired)
        {
            _cache.Remove(key);
        }
    }

    private void CleanFullCache()
    {
        _cache.Clear();
    }

    private async Task<List<T>> GetEntitiesAsync(string? cacheKey = null)
    {
        if (cacheKey != null && GetFromCache(cacheKey) is { } cached)
        {
            return cached.ToList();
        }

        var data = await LoadFromDbAsync();
        if (cacheKey != null) _cache[cacheKey] = (DateTime.UtcNow, data);
        CleanOldCacheEntries();
        return data.ToList();
    }
    
    public async Task<List<T>> GetAllAsync(string? cacheKey = null)
    {
        return await GetEntitiesAsync(cacheKey);
    }

    public async Task<T?> GetByIdAsync(ObjectId id, string? cacheKey = null)
    {
        return (await GetEntitiesAsync(cacheKey)).FirstOrDefault(e => GetId(e) == id);
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? cacheKey = null)
    {
        return (await GetEntitiesAsync(cacheKey)).FirstOrDefault(predicate.Compile());
    }

    public async Task<IEnumerable<T>> FindManyAsync(Expression<Func<T, bool>> predicate, string? cacheKey = null)
    {
        return (await GetEntitiesAsync(cacheKey)).Where(predicate.Compile());
    }

    public async Task<bool> AddAsync(T entity)
    {
        CleanFullCache();
        var entities = await GetEntitiesAsync();
        entities.Add(entity);
        return await SaveToDbAsync(entities);
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        CleanFullCache();
        var entities = await GetEntitiesAsync();
        var entityId = GetId(entity);
        var toChange = entities.FirstOrDefault(e => GetId(e) == entityId);

        if (toChange != null)
        {
            var index = entities.IndexOf(toChange);
            entities[index] = entity;
            return await SaveToDbAsync(entities);
        }
        Console.WriteLine($"[ObjectRepository > Update] Entity {typeof(T).Name} was not found and updated");
        return false;
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        return await DeleteAsync(GetId(entity));
    }

    public async Task<bool> DeleteAsync(ObjectId id)
    {
        CleanFullCache();
        var entities = await GetEntitiesAsync();
        var toDelete = entities.FirstOrDefault(e => GetId(e) == id);
        if (toDelete != null)
        {
            entities.Remove(toDelete);
            return await SaveToDbAsync(entities);
        }
        Console.WriteLine($"[ObjectRepository > Delete] Entity {typeof(T).Name} was not deleted");
        return false;
    }

    public async Task<bool> ReplaceAllAsync(List<T> entities)
    {
        CleanFullCache();
        return await SaveToDbAsync(entities);
    }
    
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, string? cacheKey = null)
    {
        var entities = await GetEntitiesAsync(cacheKey);
        return predicate == null ? entities.Count : entities.Count(predicate.Compile());
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, string? cacheKey = null)
    {
        var entities = await GetEntitiesAsync(cacheKey);
        return entities.Any(predicate.Compile());
    } 
}