namespace Solver;

public class SimulationRunner
{
    public SimulationResult Run(BorderConditions borderConditions, double speedA, double speedB)
    {
        var initialState = new SimulationState([], [ default ], -double.Epsilon);

        var simulation = new Simulation(initialState, borderConditions, speedA, speedB);

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
            if (iterationSuccessful)
            {
                if (history.Count > 0)
                {
                    CheckWavesOrdering(history[^1].Waves, (history[^1].Time + simulation.State.Time) * 0.5);
                }

                history.Add(simulation.State);
            }

            LogState(simulation.State);
        } while (iterationSuccessful && iterationNumber < 10000);

        Console.WriteLine($"Last Time: {history[^1].Time}");
        return new SimulationResult(history.ToArray(), !simulation.HadErrors, simulation.HadErrors ? simulation.ErrorTime : null);
    }

    private void CheckWavesOrdering(Wave[] waves, double time)
    {
        var x = 0.0;
        foreach (var wave in waves.Reverse())
        {
            var newX = wave.GetPosition(time);
            if (newX < x)
            {
                throw new Exception("Wave order incorrect");
            }
            x = newX;
        }
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