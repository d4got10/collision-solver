namespace MyDiplomaSolver;

public readonly record struct SimulationResult(SimulationState[] History, bool Successful, double? ErrorTime);