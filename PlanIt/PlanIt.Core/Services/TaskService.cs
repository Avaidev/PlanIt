namespace PlanIt.Core.Services;

public static class TaskService
{
    public static async Task RenovateTasks(DataAccessService db)
    {
        var tasks = await db.Tasks.GetAll();
        foreach (var task in tasks)
        {
            if (task.Repeat == null) continue;
            var difference = (int)(DateTime.Today - task.CompleteDate.Date).TotalDays;
            
            if (difference <= 0) continue;
            task.IsDone = false;
            var intervalsNum = difference / task.Repeat.Value;
            if (difference % task.Repeat.Value != 0) intervalsNum++;
            
            task.CompleteDate = task.CompleteDate.AddDays(intervalsNum * task.Repeat.Value);
        }

        await db.Tasks.ReplaceList(tasks);
    }
}