namespace Solver.Models;

public readonly record struct Wave(
    int Id,
    int SourceId,
    int IndexInArray,
    double StartPosition,
    double StartTime,
    double Velocity);