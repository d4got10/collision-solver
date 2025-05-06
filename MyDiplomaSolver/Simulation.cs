using System.Diagnostics;
using MathNet.Numerics.RootFinding;

namespace MyDiplomaSolver;

public class Simulation
{
    public const double CollisionTimeThreshold = 100_000;
    
    public Simulation(SimulationState initialState, BorderCondition borderCondition, double speedA, double speedB)
    {
        State = initialState;
        BorderCondition = borderCondition;
        SpeedA = speedA;
        SpeedB = speedB;
    }
    
    public SimulationState State { get; private set; }
    public BorderCondition BorderCondition { get; }
    public double SpeedA { get; private set; }
    public double SpeedB { get; private set; }

    private static Wave _borderWave = default;

    private static int _waveId = 1;
    private static int _parentId = 1;

    public bool Iterate()
    {
        var nextConditionPointTime = GetNextBorderConditionPointTime();
        var collisions = CalculateCollision().ToArray();
        if (collisions.Length == 0)
        {
            if (!double.IsPositiveInfinity(nextConditionPointTime))
            {
                State = ApplyNextBorderConditionPoint(nextConditionPointTime);
                return true;
            }
            
            Console.WriteLine("No collisions. Terminating...");
            return false;
        }
        
        Console.WriteLine($"Collision count: {collisions.Length}");
        var collision = collisions.MinBy(c => c.EncounterTime);

        if (nextConditionPointTime < collision.EncounterTime)
        {
            State = ApplyNextBorderConditionPoint(nextConditionPointTime);
        }
        else
        {
            try
            {
                State = ApplyCollision(collision);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"Получена ошибка при расчёте столкновения волн: {exception.Message}");
                return false;
            }
        }
        
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

    private SimulationState ApplyNextBorderConditionPoint(double time)
    {
        var (leftPoint, rightPoint) = GetBorderConditionPoints(time);
        Console.WriteLine($"Applying border condition between {leftPoint.Time} and {rightPoint.Time}");
        
        var right = State.Segments[^1];

        var ccl = 0;
        var ccr = right.Coefficients.C > 0 ? SpeedB : SpeedA;

        var phi = leftPoint.Value;
        var k = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
        
        Func<double[], double[]> equations = vars =>
        {
            double a = vars[0]; // Ai
            double b = vars[1]; // Bi
            double c = vars[2]; // Ci

            var cci = c >= 0 ? SpeedB : SpeedA;
            
            double r = vars[3]; // Ri
        
            return
            [
                phi + k * (time - leftPoint.Time) - a,
                k - b,
                right.Coefficients.B + right.Coefficients.C * r - b - c * r,
                r*r - (ccr * ccr * right.Coefficients.C - cci * cci * c) / (right.Coefficients.C - c),
            ];
        };
    
        Console.WriteLine($"{ right.Coefficients.B + right.Coefficients.C } * r - b - c * r = 0");
        Console.WriteLine($"r*r - ({ccr * ccr * right.Coefficients.C} - cci * cci * c) / ({right.Coefficients.C} - c) = 0");
        
        // Начальное предположение
        var initialGuess = new[]
        {
            phi + k * (time - leftPoint.Time),
            k,
            -k/SpeedB,
            SpeedB,
        };
    
        // Решение системы методом Ньютона
        try
        {
            var solution = Broyden.FindRoot(equations, initialGuess);

            var wave = new Wave
            {
                Id = NextId(),
                IndexInArray = State.Waves.Length,
                SourceId = NextSourceId(),
                StartPosition = 0,
                StartTime = time,
                Velocity = solution[3]
            };

            var segment = new Segment
            {
                Left = default,
                Right = wave,
                Coefficients = new Coefficients(solution[0], solution[1], solution[2])
            };

            var waves = State.Waves.Append(wave).ToArray();
            var segments = State.Segments.Append(segment).ToArray();

            segments[^2] = segments[^2] with
            {
                Left = wave 
            };

            return new SimulationState(waves, segments, time);
        }
        catch
        {
            Console.WriteLine($"Broke on border condition between {leftPoint.Time} and {rightPoint.Time}");
            throw;
        }
    }

    private double GetNextBorderConditionPointTime()
    {
        if (BorderCondition.Points[^2].Time <= State.Time)
        {
            return double.PositiveInfinity;
        }
        
        var point = BorderCondition.Points.First(x => x.Time > State.Time);
        return point.Time;
    }
    
    private (BorderConditionPoint Left, BorderConditionPoint Right) GetBorderConditionPoints(double time)
    {
        var right = BorderCondition.Points.FirstOrDefault(x => x.Time > time);
        if (right == default)
        {
            right = new BorderConditionPoint(double.PositiveInfinity, 0);
        }
        var left = BorderCondition.Points.Last(x => x.Time <= time);
        return (left, right);
    }
    
    private SimulationState ApplyBorderWaveCollision(Collision collision)
    {
        var wave = collision.Right;
        var t = collision.EncounterTime;
     
        var middle = State.Segments[^1];
        var right = State.Segments[^2];

        var ccl = 0;
        var ccr = right.Coefficients.C > 0 ? SpeedB : SpeedA;

        var last = BorderCondition.Points[^1];
        var prev = BorderCondition.Points[^2];
        var phi = prev.Value;
        var k = (last.Value - prev.Value) / (last.Time - prev.Time);
        
        Func<double[], double[]> equations = vars =>
        {
            double a = vars[0]; // Ai
            double b = vars[1]; // Bi
            double c = vars[2]; // Ci

            var cci = c >= 0 ? SpeedB : SpeedA;
            
            double r = vars[3]; // Ri
        
            return
            [
                phi + k * (t - prev.Time) - a,
                k - b,
                right.Coefficients.B + right.Coefficients.C * r - b - c * r,
                r*r - (ccr * ccr * right.Coefficients.C - cci * cci * c) / (right.Coefficients.C - c),
            ];
        };
    
        Console.WriteLine($"{ right.Coefficients.B + right.Coefficients.C } * r - b - c * r = 0");
        Console.WriteLine($"r*r - ({ccr * ccr * right.Coefficients.C} - cci * cci * c) / ({right.Coefficients.C} - c) = 0");
        
        // Начальное предположение
        var initialGuess = new[]
        {
            phi + k * (t - prev.Time),
            k,
            -k/SpeedA,
            SpeedA,
        };
    
        // Решение системы методом Ньютона
        try
        {
            var solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-06D);

            var waves = State.Waves.ToArray();
            var segments = State.Segments.ToArray();
        
            waves[wave.IndexInArray] = wave with
            {
                Id = NextId(),
                SourceId = NextSourceId(),
                StartPosition = 0,
                StartTime = t,
                Velocity = solution[3]
            };

            segments[^1] = segments[^1] with
            {
                Coefficients = new Coefficients(solution[0], solution[1], solution[2])
            };

            return new SimulationState(Waves: waves, Segments: segments, Time: t);
        }
        catch
        {
            Console.WriteLine($"Broke on border collision between {wave.Id} and border");
            throw;
        }
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

        var leftWave = State.Waves[middle.Left.IndexInArray];
        var rightWave = State.Waves[middle.Right.IndexInArray];
        
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
                r*r - (ccr * ccr * right.Coefficients.C - cci * cci * c) / (right.Coefficients.C - c),
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

        double[] speeds = [SpeedA, SpeedB, (SpeedA + SpeedB) / 2];
        
        // Решение системы методом Ньютона
        foreach (var speedA in speeds)
        {
            foreach (var speedB in speeds)
            {
                try
                {
                    initialGuess[3] = -speedA;
                    initialGuess[4] = speedB;

                    var solution = Broyden.FindRoot(equations, initialGuess);

                    var cci = solution[2] > 0 ? SpeedB : SpeedA;
                    var li = Math.Abs(solution[3]);
                    var ri = Math.Abs(solution[4]);

                    // Debug.Assert(ccl <= li && li <= cci, $"Скорость левой границы не выполнило условие c(Cl) [{ccl}] <= Li [{li}] <= c(Ci) [{cci}]");
                    // Debug.Assert(ccr <= ri && ri <= cci, $"Скорость правой границы не выполнило условие c(Cr) [{ccr}] <= Ri [{ri}] <= c(Ci) [{cci}]");

                    return solution;
                }
                catch (Exception ex)
                {
                }
            }
        }
        
        Console.WriteLine($"Broke on collision between {leftWave.Id} and {rightWave.Id}");
        throw new Exception();
        
        try
        {
            var solution = Broyden.FindRoot(equations, initialGuess);
            
            var cci = solution[2] > 0 ? SpeedB : SpeedA;
            var li = Math.Abs(solution[3]);
            var ri = Math.Abs(solution[4]);
            
            // Debug.Assert(ccl <= li && li <= cci, $"Скорость левой границы не выполнило условие c(Cl) [{ccl}] <= Li [{li}] <= c(Ci) [{cci}]");
            // Debug.Assert(ccr <= ri && ri <= cci, $"Скорость правой границы не выполнило условие c(Cr) [{ccr}] <= Ri [{ri}] <= c(Ci) [{cci}]");
            
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
        if (State.Waves.Length == 0)
        {
            yield break;
        }
        
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