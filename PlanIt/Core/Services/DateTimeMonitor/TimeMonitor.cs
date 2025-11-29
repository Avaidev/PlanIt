using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Core.Services.DateTimeMonitor;

public class TimeMonitor : IDisposable
{   
    #region Initialization
    public TimeMonitor(ILogger<TimeMonitor> logger)
    {
        _logger = logger;
        _queue = new();
        _activeItems = new();
        _lock = new object();
        _isRunning = false;
    }
    #endregion
    
    #region Attributes
    private readonly PriorityQueue<IMonitorItem, DateTime> _queue;
    private readonly Dictionary<ObjectId, IMonitorItem> _activeItems;
    private readonly ILogger<TimeMonitor> _logger;
    private readonly object _lock;
    private CancellationTokenSource? _cancellationTokenSource;
    private IObjectRepository<ITimedObject>? _repository;
    private bool _isRunning;
    private Task? _monitorTask;
    private const int MAX_ACTIVE_ELEMENTS = 6;
    private bool _disposed = false;

    private event Action<ITimedObject, IMonitorItem.TargetTimeContext>? ObjectEventCallback;
    #endregion
    
    private async Task MonitoringLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await CheckPassedItemsAsync(token);
                await Task.Delay(1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("[TimeMonitor] Exception in monitoring: {ex}", ex.Message);
                await Task.Delay(5000, token);
            }
        }
    }

    private async Task CheckPassedItemsAsync(CancellationToken token)
    {
        var currentTime = DateTime.Now;
        var completedItems = new List<IMonitorItem>();

        lock (_lock)
        {
            while (_queue.TryPeek(out var item, out var priority) && priority <= currentTime)
            {
                _queue.Dequeue();
                if (_activeItems.ContainsKey(item.Id))
                {
                    completedItems.Add(item);
                    _activeItems.Remove(item.Id);
                }
            }
        }

        foreach (var item in completedItems)
        {
            if (token.IsCancellationRequested) break;

            try
            {
                await Task.Run(() => item.ExecuteCallback(), token);
                await Task.Run(() => LoadMonitorsAsync(), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("[TimeMonitor] Exception in items checking: {0} from source {1}", ex.Message, ex.Source);
            }
        }
    }

    private async Task<List<ITimedObject>> GetTasksFromDbAsync(int count)
    {
        HashSet<ObjectId> activeObjectIds;
        lock (_lock)
        {
            activeObjectIds = new HashSet<ObjectId>(_activeItems.Values.Select(t => t.Id));
        }

        var objects = await _repository!.GetAll();
        using var closestObjectsEnumerator = objects
            .Where(obj => !obj.IsDone)
            .Where(obj => !activeObjectIds.Contains(obj.Id))
            .Where(obj => (obj.NotifyDate != null && Utils.CheckDateForTodayScheduled((DateTime)obj.NotifyDate)) ||
                          Utils.CheckDateForTodayScheduled(obj.CompleteDate))
            .OrderBy(t => t.TargetTime).GetEnumerator();
        if (!closestObjectsEnumerator.MoveNext()) return [];
        var timeOfFirst = closestObjectsEnumerator.Current.TargetTime;
        var i = 0;
        List<ITimedObject> result = [closestObjectsEnumerator.Current];
        while (closestObjectsEnumerator.MoveNext())
        {
            if (closestObjectsEnumerator.Current.TargetTime == timeOfFirst || i < count) 
                result.Add(closestObjectsEnumerator.Current);
            i++;
        }
        return result;
    }
    
    private async Task LoadMonitorsAsync(bool oneMore = false)
    {
        int needed;
        lock (_lock)
        {
            needed = MAX_ACTIVE_ELEMENTS - _activeItems.Count;
        }

        if (needed <= 0 && oneMore) needed = 1;
        else if (oneMore) needed++;
        
        if (needed <= 0) return;
        var closestTasks = await Task.Run(() => GetTasksFromDbAsync(needed));
        lock (_lock)
        {
            foreach (var task in closestTasks)
            {
                if (!_activeItems.Values.Any(t =>  t.Id == task.Id))
                {
                    RegisterObjectForMonitoring(task);
                }
            }
        }
    }

    private void RegisterObjectForMonitoring(ITimedObject timedObject)
    {
        AddMonitor(timedObject, (obj, context) => { ObjectEventCallback?.Invoke(obj, context); });
    }

    public void RegisterNonObjForMonitoring(DateTime targetTime, NonObjectCallback callback, int repeat = 0)
    {
        AddMonitor(targetTime, callback, repeat);
    }

    private void AddMonitor(DateTime targetTime, NonObjectCallback callback, int repeat = 0)
    {
        var item = new MonitorItem<ITimedObject>(targetTime, callback, repeat);
        lock (_lock)
        {
            _activeItems.Add(item.Id, item);
            _queue.Enqueue(item, item.TargetTime);
        }
        _logger.LogInformation("[TimeMonitor] New non-object monitor added with ID: {id}", item.Id);
    }

    private void AddMonitor<T>(T obj, ObjectCallback<T> callback) where T : ITimedObject
    {
        var item = new MonitorItem<T>(obj, callback);
        lock (_lock)
        {
            _activeItems.Add(item.Id, item);
            _queue.Enqueue(item, item.TargetTime);
        }
        _logger.LogInformation("[TimeMonitor] New object monitor added with ID: {id}", item.Id);
    }

    public void TryAddOne(ObjectId id)
    {
        try
        {
            LoadMonitorsAsync(true).Wait();
            PriorityQueue<IMonitorItem, DateTime> tempQueue;
            lock (_lock)
            {
                if (_queue.Count <= MAX_ACTIVE_ELEMENTS + 1) return;
                tempQueue = new PriorityQueue<IMonitorItem, DateTime>(_queue.UnorderedItems);
            }

            bool isFirst = true;
            IMonitorItem? firstElement = null;
            IMonitorItem? lastElement = null;
            while (tempQueue.TryDequeue(out var item, out _))
            {
                if (!item.IsObject) continue;
                if (isFirst) firstElement = item;
                lastElement = item;
                isFirst = false;
            }

            if (firstElement != null && lastElement != null && !firstElement.TargetTime.Equals(lastElement.TargetTime))
            {
                lock (_lock)
                {
                    if (_queue.Remove(lastElement!, out var removedElement, out var priority))
                    {
                        if (removedElement.Id == lastElement!.Id) _activeItems.Remove(lastElement.Id);
                        else _queue.Enqueue(removedElement, priority);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[TimeMonitor] Failed to add monitor with ID: {id} Exception: {ex}", id, ex.Message);
        }

    }
    
    public bool RemoveMonitor(ObjectId id, bool reload = true)
    {
        var removed = false;
        lock (_lock)
        {
            if (_activeItems.Remove(id, out var element)
                && _queue.Remove(element, out var actuallyRemoved, out var priority))
            {
                if (actuallyRemoved.Id != id) _queue.Enqueue(actuallyRemoved, priority);
                removed = true;
            }
        }

        _logger.LogInformation(removed
                ? "[TimeMonitor] Monitor removed with ID: {id}"
                : "[TimeMonitor] Monitor no longer exists with ID: {id}", id);

        if(reload) Task.Run(async () => await LoadMonitorsAsync());
        return removed;
    }

    public void ClearAllItems()
    {
        lock (_lock)
        {
            _activeItems.Clear();
            _queue.Clear();
        }
    }

    public async Task PrepareMonitorAsync(IObjectRepository<ITimedObject> repository, Action<ITimedObject, IMonitorItem.TargetTimeContext> callback)
    {
        _logger.LogInformation("[TimeMonitor] Preparing monitor...");
        _repository = repository;
        ObjectEventCallback += callback;
        await LoadMonitorsAsync();
        _logger.LogInformation("[TimeMonitor] Monitor prepared successfully");
    }

    public void StartMonitoring()
    {
        if (_repository == null)
        {
            _logger.LogError("[TimeMonitor] Monitor not prepared! Call PrepareMonitorAsync() first");
            return;
        }
        _cancellationTokenSource = new CancellationTokenSource();
        
        if (_isRunning) return;
        _monitorTask = Task.Run(async () => await MonitoringLoopAsync(_cancellationTokenSource!.Token));
        _isRunning = true;
        _logger.LogInformation("[TimeMonitor] Monitoring started");
        
    }
    
    public void StopMonitoring()
    {
        if(_disposed) return;
        _cancellationTokenSource?.Cancel();
        _isRunning = false;
        _logger.LogInformation("[TimeMonitor] Monitoring stopped");
    }
    

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopMonitoring();
        _cancellationTokenSource?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}