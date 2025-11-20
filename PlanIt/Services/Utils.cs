using System;
using System.Collections.Generic;
using System.Linq;
using PlanIt.Models;

namespace PlanIt.Services;

public static class Utils
{
    public static bool CheckDateForToday(DateTime date) => date.Date == DateTime.Today;
    public static bool CheckDateForTomorrow(DateTime date) => date.Date == DateTime.Today.AddDays(1).Date;
    public static bool CheckDateForLater(DateTime date) => date.Date >= DateTime.Today.AddDays(2).Date;
    public static bool CheckDateForScheduled(DateTime date) => date > DateTime.Now;

    public static void OrderTasks(IList<TaskItem> tasks)
    {
        var ordered = tasks.OrderByDescending(t => t.IsImportant)
            .ThenBy(t => t.IsDone)
            .ThenBy(t => t.CompleteDate).ToList();

        tasks.Clear();
        foreach (var item in ordered)
        {
            tasks.Add(item);
        }
    }
}