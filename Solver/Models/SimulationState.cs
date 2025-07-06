namespace Solver;

public readonly record struct SimulationState(
    Wave[] Waves,
    Segment[] Segments,
    double Time);