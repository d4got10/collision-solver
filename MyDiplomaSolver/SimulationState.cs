namespace MyDiplomaSolver;

public readonly record struct SimulationState(
    Wave[] Waves,
    Segment[] Segments,
    double Time);