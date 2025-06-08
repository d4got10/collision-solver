using System;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public class TaskInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public double SpeedA { get; set; }
    public double SpeedB { get; set; }
    public BorderConditions BorderConditions { get; set; }
}