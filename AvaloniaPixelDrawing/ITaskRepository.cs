using System.Collections.Generic;

namespace AvaloniaPixelDrawing;

public interface ITaskRepository
{
    IReadOnlyList<TaskInfo> GetAll();
    TaskInfo CreateNew();
    void Save(TaskInfo taskInfo);
    void Remove(TaskInfo taskInfo);
}