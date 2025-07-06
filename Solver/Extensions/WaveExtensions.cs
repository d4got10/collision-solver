using Solver.Models;

namespace Solver.Extensions;

public static class WaveExtensions
{    
    public static double GetPosition(this Wave wave, double atTime)
    {
        return wave.StartPosition + wave.Velocity * (atTime - wave.StartTime);
    }
}