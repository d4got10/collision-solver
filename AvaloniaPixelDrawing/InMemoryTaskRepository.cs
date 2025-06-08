using System.Collections.Generic;

namespace AvaloniaPixelDrawing;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskInfo> _tasks = new();
    
    public IReadOnlyList<TaskInfo> GetAll()
    {
        return _tasks;
    }

    public TaskInfo CreateNew()
    {
        var task = new TaskInfo
        {
            Name = $"Задача {_tasks.Count + 1}"
        };

        _tasks.Add(task);
        return task;
    }

    public void Save(TaskInfo taskInfo)
    {
    }

    public void Remove(TaskInfo taskInfo)
    {
    }
}