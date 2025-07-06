using System;
using Solver;

namespace AvaloniaPixelDrawing;

public class TaskInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public double Lambda { get; set; }
    public double Mu { get; set; }
    public double V { get; set; }
    public double Ro { get; set; }
    public BorderConditions BorderConditions { get; set; }
}