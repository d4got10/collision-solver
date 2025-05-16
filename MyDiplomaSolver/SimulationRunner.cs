namespace MyDiplomaSolver;

public class SimulationRunner
{
    public SimulationResult Run(BorderConditions borderConditions)
    {
        double a = 3702.77;
        double b = 2378.63;

        var initialState = new SimulationState([], [ default ], -double.Epsilon);

        var simulation = new Simulation(initialState, borderConditions, a, b);

        List<SimulationState> history = new();

        var iterationNumber = 0;
        bool iterationSuccessful;
        Console.WriteLine($"Iteration [{iterationNumber}]:");
        LogState(simulation.State);
        do
        {
            iterationNumber++;
            Console.WriteLine($"Iteration [{iterationNumber}]:");
            iterationSuccessful = simulation.Iterate();
            if(iterationSuccessful)
                history.Add(simulation.State);
            LogState(simulation.State);
        } while (iterationSuccessful && iterationNumber < 10000);

        return new SimulationResult(history.ToArray(), !simulation.HadErrors, simulation.HadErrors ? simulation.ErrorTime : null);
    }
    
    private void LogState(SimulationState state)
    {
        foreach (var wave in state.Waves)
        {
            Console.WriteLine($"Wave[{wave.Id}] at {wave.GetPosition(state.Time) * 1000:0.00} with velocity {wave.Velocity:0.0000000}");
        }

        for (var i = 0; i < state.Segments.Length; i++)
        {
            var segment = state.Segments[i];
            Console.WriteLine($"Segment [{i}]: A={segment.Coefficients.A} B={segment.Coefficients.B} C={segment.Coefficients.C}");
        }
        Console.WriteLine();
    }
}