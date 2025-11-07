namespace PlanIt.Models;
using MongoDB.Bson;
using System.Collections.Generic;

public class Category
{
    public ObjectId Id { get; set; }
    public required string Title { get; set; }
    public string Color  { get; set; } = "Default";
    public string Icon { get; set; } = "Cubes";
    public List<Task> Tasks { get; set; } = [];
}