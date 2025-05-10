using System.Diagnostics;
using MathNet.Numerics.RootFinding;

namespace MyDiplomaSolver;

public class Simulation
{
    public const double CollisionTimeThreshold = 100_000;
    
    public Simulation(SimulationState initialState, BorderConditions borderConditions, double speedA, double speedB)
    {
        State = initialState;
        BorderConditions = borderConditions;
        SpeedA = speedA;
        SpeedB = speedB;
    }
    
    public SimulationState State { get; private set; }
    public BorderConditions BorderConditions { get; }
    public double SpeedA { get; private set; }
    public double SpeedB { get; private set; }
    public bool HadErrors { get; private set; }

    private static Wave _borderWave = default;

    private static int _waveId = 1;
    private static int _parentId = 1;

    public bool Iterate()
    {
        HadErrors = false;
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
                HadErrors = true;
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

        double[] solution = [];
        
        // Решение системы методом Ньютона
        try
        {
            solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-6);
        }
        catch
        {
            if (right.Coefficients.C >= 0)
            {
                Console.WriteLine($"Broke on border condition between {leftPoint.Time} and {rightPoint.Time}");
                throw;
            }
        }

        // Two wave
        if (solution.Length == 0 || right.Coefficients.C < 0 && solution[2] > 0)
        {
            return ApplyNextBorderConditionPointWithRigidZone(time);
        }

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
    
    private SimulationState ApplyNextBorderConditionPointWithRigidZone(double time)
    {
        var (leftPoint, rightPoint) = GetBorderConditionPoints(time);
        Console.WriteLine($"Applying border condition with rigid zone between {leftPoint.Time} and {rightPoint.Time}");
        
        var right = State.Segments[^1];

        var phi = leftPoint.Value;
        var k = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);

        var aj = phi + k * (time - leftPoint.Time);
        var bj = k;
        var ai = aj;
        var bi = right.Coefficients.B + right.Coefficients.C * SpeedA;
        var cj = (bi - k) / SpeedB;
        
        // Решение системы методом Ньютона
        try
        {
            var leftWave = new Wave
            {
                Id = NextId(),
                IndexInArray = State.Waves.Length + 1,
                SourceId = NextSourceId(),
                StartPosition = 0,
                StartTime = time,
                Velocity = SpeedB
            };
            
            var rightWave = new Wave
            {
                Id = NextId(),
                IndexInArray = State.Waves.Length,
                SourceId = NextSourceId(),
                StartPosition = 0,
                StartTime = time,
                Velocity = SpeedA
            };

            var leftSegment = new Segment
            {
                Left = default,
                Right = leftWave,
                Coefficients = new Coefficients(aj, bj, cj)
            };
            
            var rightSegment = new Segment
            {
                Left = leftWave,
                Right = rightWave,
                Coefficients = new Coefficients(ai, bi, 0)
            };

            var waves = State.Waves.Append(rightWave).Append(leftWave).ToArray();
            var segments = State.Segments.Append(rightSegment).Append(leftSegment).ToArray();

            segments[^3] = segments[^3] with
            {
                Left = rightWave
            };

            return new SimulationState(waves, segments, time);
        }
        catch
        {
            Console.WriteLine($"Broke on border condition with rigid zone between {leftPoint.Time} and {rightPoint.Time}");
            throw;
        }
    }

    private double GetNextBorderConditionPointTime()
    {
        if (BorderConditions.Points[^2].Time <= State.Time)
        {
            return double.PositiveInfinity;
        }
        
        var point = BorderConditions.Points.First(x => x.Time > State.Time);
        return point.Time;
    }
    
    private (BorderConditionPoint Left, BorderConditionPoint Right) GetBorderConditionPoints(double time)
    {
        var right = BorderConditions.Points.FirstOrDefault(x => x.Time > time);
        if (right == default)
        {
            right = new BorderConditionPoint(double.PositiveInfinity, 0);
        }
        var left = BorderConditions.Points.Last(x => x.Time <= time);
        return (left, right);
    }

    private SimulationState ApplyBorderWaveCollisionWithRigidZone(Collision collision)
    {
        var time = collision.EncounterTime;
        var (leftPoint, rightPoint) = GetBorderConditionPoints(time);
        Console.WriteLine($"Applying border condition with rigid zone between {leftPoint.Time} and {rightPoint.Time}");
        
        var right = State.Segments[^1];

        var phi = leftPoint.Value;
        var k = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);

        var aj = phi + k * (time - leftPoint.Time);
        var bj = k;
        var ai = aj;
        var bi = right.Coefficients.B + right.Coefficients.C * SpeedA;
        var cj = (bi - k) / SpeedB;
        
        var leftWave = new Wave
        {
            Id = NextId(),
            IndexInArray = State.Waves.Length,
            SourceId = NextSourceId(),
            StartPosition = 0,
            StartTime = time,
            Velocity = SpeedB
        };
            
        var rightWave = new Wave
        {
            Id = NextId(),
            IndexInArray = State.Waves.Length - 1,
            SourceId = NextSourceId(),
            StartPosition = 0,
            StartTime = time,
            Velocity = SpeedA
        };

        var leftSegment = new Segment
        {
            Left = default,
            Right = leftWave,
            Coefficients = new Coefficients(aj, bj, cj)
        };
            
        var rightSegment = new Segment
        {
            Left = leftWave,
            Right = rightWave,
            Coefficients = new Coefficients(ai, bi, 0)
        };

        var waves = State.Waves.SkipLast(1).Append(rightWave).Append(leftWave).ToArray();
        var segments = State.Segments.SkipLast(1).Append(rightSegment).Append(leftSegment).ToArray();

        segments[^3] = segments[^3] with
        {
            Left = rightWave
        };

        return new SimulationState(waves, segments, time);
    }
    
    private SimulationState ApplyBorderWaveCollision(Collision collision)
    {
        var wave = collision.Right;
        var t = collision.EncounterTime;
     
        var middle = State.Segments[^1];
        var right = State.Segments[^2];

        var ccl = 0;
        var ccr = right.Coefficients.C > 0 ? SpeedB : SpeedA;

        var last = BorderConditions.Points[^1];
        var prev = BorderConditions.Points[^2];
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

        double[] solution = [];
        // Решение системы методом Ньютона
        try
        {
            solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-06D);
        }
        catch
        {
            if (right.Coefficients.C >= 0)
            {
                Console.WriteLine($"Broke on border collision between {wave.Id} and border");
                throw;
            }
        }

        if (solution.Length == 0 || right.Coefficients.C < 0 && solution[2] > 0)
        {
            return ApplyBorderWaveCollisionWithRigidZone(collision);
        }

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
    
    private SimulationState ApplyDoubleWaveCollision(Collision collision)
    {
        var leftWave = collision.Left;
        var rightWave = collision.Right;

        var left = State.Segments[leftWave.IndexInArray + 1];
        var middle = State.Segments[leftWave.IndexInArray];
        var right = State.Segments[rightWave.IndexInArray];
        
        var t = collision.EncounterTime;
        var x = leftWave.StartPosition + leftWave.Velocity * (t - leftWave.StartTime);

        double[] solution = [];
        try
        {
            solution = GetSolutionForTwoWaveCollision(left, middle, right, t, x);
        }
        catch (Exception ex)
        {
            if (right.Coefficients.C >= 0)
            {
                Console.WriteLine($"Broke on collision between {leftWave.Id} and {rightWave.Id}");
                throw;
            }
        }

        // Two wave
        if (solution.Length == 0 || right.Coefficients.C < 0 && solution[2] > 0)
        {
            solution = GetSolutionForTwoWaveCollisionWithRigidZone(left, middle, right, t, x);

            var ai = solution[0];
            var aj = solution[1];
            var bj = solution[2];
            var cj = solution[3];
            var l = solution[4];
            var bi = solution[5];
            
            var sourceId = NextSourceId();
            var waves = State.Waves.ToArray();
            var segments = State.Segments.ToArray();

            var wavesFromRight = waves.Take(rightWave.IndexInArray);
            var wavesFromLeft = waves.Skip(leftWave.IndexInArray + 1);

            var segmentsFromRight = segments.Take(leftWave.IndexInArray).ToArray();
            var segmentsFromLeft = segments.Skip(leftWave.IndexInArray + 1).ToArray();

            var resultRightWave = new Wave
            {
                Id = NextId(),
                SourceId = sourceId,
                StartTime = t,
                StartPosition = x,
                Velocity = SpeedA
            };
            
            var resultMiddleWave = new Wave
            {
                Id = NextId(),
                SourceId = sourceId,
                StartTime = t,
                StartPosition = x,
                Velocity = SpeedB
            };
            
            var resultLeftWave = new Wave
            {
                Id = NextId(),
                SourceId = sourceId,
                StartTime = t,
                StartPosition = x,
                Velocity = l
            };

            var leftSegment = new Segment
            {
                Left = resultLeftWave,
                Right = resultMiddleWave,
                Coefficients = new Coefficients
                {
                    A = aj,
                    B = bj,
                    C = cj
                }
            };
            
            var rightSegment = new Segment
            {
                Left = resultMiddleWave,
                Right = resultRightWave,
                Coefficients = new Coefficients
                {
                    A = ai,
                    B = bi,
                    C = 0
                }
            };

            waves = wavesFromRight
                .Concat([resultRightWave, resultMiddleWave, resultLeftWave])
                .Concat(wavesFromLeft)
                .Select((wave, i) => wave with
                {
                    IndexInArray = i
                })
                .ToArray();

            segmentsFromRight[^1] = segmentsFromRight[^1] with
            {
                Left = resultRightWave
            };
            
            segmentsFromLeft[0] = segmentsFromLeft[0] with
            {
                Right = resultLeftWave
            };

            segments = segmentsFromRight
                .Concat([rightSegment, leftSegment])
                .Concat(segmentsFromLeft)
                .Select((s, i) => s with
                {
                    Left = s.Left with
                    {
                        IndexInArray = i
                    },
                    Right = s.Right with
                    {
                        IndexInArray = i - 1
                    }
                })
                .ToArray();
            
            return new SimulationState(
                Waves: waves, 
                Segments: segments, 
                Time: t);
        }
        else
        {
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
            -(left.Coefficients.C + right.Coefficients.C) / 2,
            -(SpeedA + SpeedB) / 2,
            (SpeedA + SpeedB) / 2,
        };

        double[] speeds = [SpeedA, SpeedB, (SpeedA + SpeedB) / 2];
        double[] coefficients = [-(left.Coefficients.C + right.Coefficients.C) / 2, (left.Coefficients.C + right.Coefficients.C) / 2];
        
        // Решение системы методом Ньютона
        foreach (var speedA in speeds)
        {
            foreach (var speedB in speeds)
            {
                foreach (var coef in coefficients)
                {
                    try
                    {
                        initialGuess[2] = coef;
                        initialGuess[3] = -speedA;
                        initialGuess[4] = speedB;

                        var solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-06D);

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
        }
        
        throw new Exception();
    }

    private double[] GetSolutionForTwoWaveCollisionWithRigidZone(Segment left, Segment middle, Segment right, double t, double x)
    {
        var ccl = left.Coefficients.C > 0 ? SpeedB : SpeedA;

        var leftWave = State.Waves[middle.Left.IndexInArray];
        var rightWave = State.Waves[middle.Right.IndexInArray];
        
        Console.WriteLine($"Getting solution for collision with rigidzone between {leftWave.Id} and {rightWave.Id}");

        var bi = right.Coefficients.B + right.Coefficients.C * SpeedA;
        var al = left.Coefficients.A;
        var bl = left.Coefficients.B;
        var cl = left.Coefficients.C;
        var tl = leftWave.StartTime;
        
        Func<double[], double[]> equations = vars =>
        {
            double ai = vars[0];

            double aj = vars[1];
            double bj = vars[2];
            double cj = vars[3];
            double l = vars[4]; 

            var ccj = cj > 0 ? SpeedB : SpeedA;

            return
            [
                aj - cj * x - ai,
                bj + cj * SpeedB - bi,
                al + bl * (t - tl) + cl * x - aj + cj * x,
                bl + cl * l - bj - cj * l,
                l * l - (ccl * ccl * cl - ccj * ccj * cj) / (cl - cj)
            ];
        };
        
    
        // Начальное предположение
        var initialGuess = new[]
        {
            (left.Coefficients.A + right.Coefficients.A) / 2,
            (left.Coefficients.A + right.Coefficients.A) / 2,
            (left.Coefficients.B + right.Coefficients.B) / 2,
            (left.Coefficients.C + right.Coefficients.C) / 2,
            -(SpeedA + SpeedB) / 2,
        };

        double[] speeds = [SpeedA, SpeedB, (SpeedA + SpeedB) / 2];
        
        // Решение системы методом Ньютона
        foreach (var speedA in speeds)
        {
            try
            {
                initialGuess[4] = -speedA;

                var solution = Broyden.FindRoot(equations, initialGuess, accuracy: 1E-6);

                return solution.Append(bi).ToArray();
            }
            catch (Exception ex)
            {
            }
        }
        
        throw new Exception("Rigid zone two wave collision had no solution");
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