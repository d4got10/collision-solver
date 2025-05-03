namespace MyDiplomaSolver;

public readonly record struct Wave(
    int Id,
    int SourceId,
    int IndexInArray,
    double StartPosition,
    double StartTime,
    double Velocity);

public static class WaveExtensions
{    
    public static double GetPosition(this Wave wave, double atTime)
    {
        return wave.StartPosition + wave.Velocity * (atTime - wave.StartTime);
    }
}