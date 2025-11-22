using System.Collections.ObjectModel;
using ReactiveUI;

namespace PlanIt.Data.Models;

public class Node : ReactiveObject
{
    public string NodeTitle { get; }
    public Category? Category { get; }

    public ObservableCollection<TaskItem> Tasks { get; }


    public Node(Category category, List<TaskItem> tasks)
    {
        NodeTitle = "";
        Category = category;
        Tasks = new ObservableCollection<TaskItem>(tasks);
    }

    public Node(string title, List<TaskItem> tasks)
    {
        NodeTitle = title;
        Tasks = new ObservableCollection<TaskItem>(tasks);
    }
}