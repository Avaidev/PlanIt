namespace PlanIt.Models;
using MongoDB.Driver;

public class DataAccess
{
    private readonly MongoClient _client;
    private IMongoDatabase _db;
    
    public IMongoCollection<Category> Categories;
    public IMongoCollection<Notification> Notifications;

    public FilterDefinition<T> GetAllFilter<T>() => Builders<T>.Filter.Empty;

    public DataAccess(string host, short port, string db_name)
    {
        _client = new MongoClient($"mongodb://{host}:{port}");
        _db = _client.GetDatabase(db_name);
        Notifications = _db.GetCollection<Notification>("notifications");
        Categories = _db.GetCollection<Category>("categories");
    }

    public void ChangeDb(string db_name)
    {
        _db = _client.GetDatabase(db_name);
    }
}