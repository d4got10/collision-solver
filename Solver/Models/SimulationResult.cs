namespace Solver.Models;

public readonly record struct SimulationResult(SimulationState[] History, bool Successful, double? ErrorTime);