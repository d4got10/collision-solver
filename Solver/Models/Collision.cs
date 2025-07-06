namespace Solver.Models;

public readonly record struct Collision(
    Wave Left,
    Wave Right,
    double EncounterTime);