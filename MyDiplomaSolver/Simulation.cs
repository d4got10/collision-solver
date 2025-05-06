using MathNet.Numerics.RootFinding;

namespace MyDiplomaSolver;

public class Simulation
{
    public const double CollisionTimeThreshold = 100_000;
    
    public Simulation(SimulationState initialState, double speedA, double speedB)
    {
        State = initialState;
        SpeedA = speedA;
        SpeedB = speedB;
    }
    
    public SimulationState State { get; private set; }
    public double SpeedA { get; private set; }
    public double SpeedB { get; private set; }

    private static Wave _borderWave = default;

    private static int _waveId = 1;
    private static int _parentId = 1;

    public bool Iterate()
    {
        var collisions = CalculateCollision().ToArray();
        if (collisions.Length == 0)
        {
            Console.WriteLine("No collisions. Terminating...");
            return false;
        }
        
        Console.WriteLine($"Collision count: {collisions.Length}");
        var collision = collisions.MinBy(c => c.EncounterTime);
        State = ApplyCollision(collision);
        return true;
    }

    public static int NextId() => _waveId++;
    public static int NextSourceId() => _parentId++;

    private SimulationState ApplyCollision(Collision collision)
    {
        Console.WriteLine($"Applying collision between {collision.Left.Id} and {collision.Right.Id}");
        
        if (collision.Left.Id == _borderWave.Id)
        {
            return ApplyBorderWaveCollision(collision);
        }

        return ApplyDoubleWaveCollision(collision);
    }

    private SimulationState ApplyBorderWaveCollision(Collision collision)
    {
        var wave = collision.Right;
        var t = collision.EncounterTime;
        
        var waves = State.Waves.ToArray();
        var segments = State.Segments.ToArray();
        
        waves[wave.IndexInArray] = wave with
        {
            Id = NextId(),
            SourceId = NextSourceId(),
            StartPosition = 0,
            StartTime = t,
            Velocity = -wave.Velocity
        };

        return new SimulationState(Waves: waves, Segments: segments, Time: t);
    }
    
    private SimulationState ApplyDoubleWaveCollision(Collision collision)
    {
        var leftWave = collision.Left;
        var rightWave = collision.Right;

        var left = State.Segments[leftWave.IndexInArray + 1];
        var middle = State.Segments[leftWave.IndexInArray];
        var right = State.Segments[rightWave.IndexInArray];
        
        var t = collision.EncounterTime;
        var x = leftWave.StartPosition + leftWave.Velocity * (t - leftWave.StartTime);
        
        var solution = GetSolutionForTwoWaveCollision(left, middle, right, t, x);

        var sourceId = NextSourceId();
        var waves = State.Waves.ToArray();
        var segments = State.Segments.ToArray();

        waves[rightWave.IndexInArray] = waves[rightWave.IndexInArray] with
        {
            Id = NextId(),
            SourceId = sourceId,
            StartTime = t,
            StartPosition = x,
            Velocity = solution[4]
        };
        
        waves[leftWave.IndexInArray] = waves[leftWave.IndexInArray] with
        {
            Id = NextId(),
            SourceId = sourceId,
            StartTime = t,
            StartPosition = x,
            Velocity = solution[3]
        };

        segments[leftWave.IndexInArray] = segments[leftWave.IndexInArray] with
        {
            Left = waves[leftWave.IndexInArray],
            Right = waves[rightWave.IndexInArray],
            Coefficients = new Coefficients
            {
                A = solution[0],
                B = solution[1],
                C = solution[2],
            }
        };

        return State with
        {
            Waves = waves,
            Segments = segments,
            Time = t,
        };
    }

    private double[] GetSolutionForTwoWaveCollision(Segment left, Segment middle, Segment right, double t, double x)
    {
        var ccl = left.Coefficients.C > 0 ? SpeedB : SpeedA;
        var ccr = right.Coefficients.C > 0 ? SpeedB : SpeedA;

        var leftWave = middle.Left;
        var rightWave = middle.Right;
        
        Console.WriteLine($"Getting solution for collision between {leftWave.Id} and {rightWave.Id}");
        
        Func<double[], double[]> equations = vars =>
        {
            double a = vars[0]; // Ai
            double b = vars[1]; // Bi
            double c = vars[2]; // Ci

            var cci = c > 0 ? SpeedB : SpeedA;
            
            double l = vars[3]; // Li
            double r = vars[4]; // Ri
        
            return
            [
                left.Coefficients.A + left.Coefficients.B * (t - leftWave.StartTime) + left.Coefficients.C * x - a - c * x,
                left.Coefficients.B + left.Coefficients.C * l - b - c * l,
                right.Coefficients.B + right.Coefficients.C * r - b - c * r,
                l*l - (ccl * ccl * left.Coefficients.C - cci * cci * c) / (left.Coefficients.C - c),
                r*r - (ccr * ccr * left.Coefficients.C - cci * cci * c) / (left.Coefficients.C - c),
            ];
        };
    
        // Начальное предположение
        var initialGuess = new[]
        {
            (left.Coefficients.A + right.Coefficients.A) / 2,
            (left.Coefficients.B + right.Coefficients.B) / 2,
            (left.Coefficients.C + right.Coefficients.C) / 2,
            -(SpeedA + SpeedB) / 2,
            (SpeedA + SpeedB) / 2,
        };
    
        // Решение системы методом Ньютона
        try
        {
            var solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-09D);
            return solution;
        }
        catch
        {
            Console.WriteLine($"Broke on collision between {leftWave.Id} and {rightWave.Id}");
            throw;
        }
    }

    private IEnumerable<Collision> CalculateCollision()
    {
        if (CheckCollisionWithBorder(State.Waves[^1], out var collision))
        {
            yield return collision;
        }
        
        for (var i = State.Waves.Length - 1; i > 0; i--)
        {
            var leftWave = State.Waves[i];
            var rightWave = State.Waves[i - 1];

            if (leftWave.SourceId == rightWave.SourceId)
            {
                continue;
            }

            var leftWavePosition = leftWave.GetPosition(State.Time);
            var rightWavePosition = rightWave.GetPosition(State.Time);

            var relativeVelocity = rightWave.Velocity - leftWave.Velocity;
            if (Math.Round(relativeVelocity, 6) < 0)
            {
                var timeToCollision = (rightWavePosition - leftWavePosition) / -relativeVelocity;

                if (timeToCollision < CollisionTimeThreshold)
                {
                    yield return new Collision(leftWave, rightWave, State.Time + timeToCollision);
                }
            }
        }
    }

    private bool CheckCollisionWithBorder(Wave wave, out Collision collision)
    {
        if (wave.Velocity < 0)
        {
            var timeToCollision = wave.GetPosition(State.Time) / -wave.Velocity;
            collision = new Collision(_borderWave, wave, State.Time + timeToCollision);
            return true;
        }
        
        collision = default;
        return false;
    }
}