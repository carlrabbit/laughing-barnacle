namespace Statistics;

/// <summary>
/// Collects descriptive statistics using lock-free updates.
/// Use <see cref="GetSnapshot"/> when reading multiple statistics that must come from one consistent point in time.
/// </summary>
public sealed class OnlineDescriptiveStatistics
{
    private State state = State.Empty;
    /// <summary>
    /// Gets an immutable snapshot of the current statistics.
    /// </summary>
    /// <returns>A snapshot containing all computed metrics at one point in time.</returns>

    public StatisticsSnapshot GetSnapshot()
    {
        var snapshot = Volatile.Read(ref state);
        return new StatisticsSnapshot(
            snapshot.Count,
            snapshot.Mean,
            snapshot.Median,
            snapshot.Min,
            snapshot.Max,
            snapshot.Variance,
            snapshot.StandardDeviation,
            snapshot.Percentile05,
            snapshot.Percentile95);
    }
    /// <summary>
    /// Gets the number of values observed.
    /// </summary>
    /// <returns>The number of values observed.</returns>

    public long Count => Volatile.Read(ref state).Count;
    /// <summary>
    /// Gets the arithmetic mean of all observed values.
    /// </summary>
    /// <returns>The arithmetic mean of all observed values.</returns>
    public double Mean => Volatile.Read(ref state).Mean;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Median => Volatile.Read(ref state).Median;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Max => Volatile.Read(ref state).Max;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Min => Volatile.Read(ref state).Min;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Variance => Volatile.Read(ref state).Variance;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double StandardDeviation => Volatile.Read(ref state).StandardDeviation;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Percentile95 => Volatile.Read(ref state).Percentile95;
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public double Percentile05 => Volatile.Read(ref state).Percentile05;
    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="value">The value.</param>

    public void Update(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be finite.");
        }

        var spinWait = new SpinWait();

        while (true)
        {
            var snapshot = Volatile.Read(ref state);
            var updated = snapshot.Add(value);

            if (ReferenceEquals(Interlocked.CompareExchange(ref state, updated, snapshot), snapshot))
            {
                return;
            }

            spinWait.SpinOnce();
        }
    }

    private sealed class State
    {
        /// <summary>
        /// Performs the new operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public static readonly State Empty = new(
            count: 0,
            mean: double.NaN,
            m2: 0,
            min: double.NaN,
            max: double.NaN,
            percentile05: P2QuantileEstimatorState.Create(0.05),
            median: P2QuantileEstimatorState.Create(0.5),
            percentile95: P2QuantileEstimatorState.Create(0.95));

        private readonly P2QuantileEstimatorState percentile05;
        private readonly P2QuantileEstimatorState median;
        private readonly P2QuantileEstimatorState percentile95;
        private readonly double mean;
        private readonly double m2;
        private readonly double min;
        private readonly double max;
        /// <summary>
        /// Performs the state operation.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="mean">The mean.</param>
        /// <param name="m2">The sum of squared differences from the mean.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="percentile05">The percentile 05.</param>
        /// <param name="median">The median.</param>
        /// <param name="percentile95">The percentile 95.</param>
        /// <returns>The operation result.</returns>

        public State(
            long count,
            double mean,
            double m2,
            double min,
            double max,
            P2QuantileEstimatorState percentile05,
            P2QuantileEstimatorState median,
            P2QuantileEstimatorState percentile95)
        {
            Count = count;
            this.mean = mean;
            this.m2 = m2;
            this.min = min;
            this.max = max;
            this.percentile05 = percentile05;
            this.median = median;
            this.percentile95 = percentile95;
        }
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>

        public long Count { get; }
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Mean => Count == 0 ? double.NaN : mean;
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Median => median.GetEstimate();
        /// <summary>
        /// Performs the sqrt operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Max => Count == 0 ? double.NaN : max;
        /// <summary>
        /// Performs the sqrt operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Min => Count == 0 ? double.NaN : min;
        /// <summary>
        /// Performs the sqrt operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Variance => Count == 0 ? double.NaN : m2 / Count;
        /// <summary>
        /// Performs the sqrt operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double StandardDeviation => Count == 0 ? double.NaN : Math.Sqrt(Variance);
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Percentile95 => percentile95.GetEstimate();
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public double Percentile05 => percentile05.GetEstimate();
        /// <summary>
        /// Performs the add operation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The operation result.</returns>

        public State Add(double value)
        {
            if (Count == 0)
            {
                return new State(
                    count: 1,
                    mean: value,
                    m2: 0,
                    min: value,
                    max: value,
                    percentile05: percentile05.Add(value),
                    median: median.Add(value),
                    percentile95: percentile95.Add(value));
            }

            var nextCount = Count + 1;
            var delta = value - mean;
            var nextMean = mean + (delta / nextCount);
            var delta2 = value - nextMean;
            var nextM2 = m2 + (delta * delta2);

            return new State(
                count: nextCount,
                mean: nextMean,
                m2: nextM2,
                min: Math.Min(min, value),
                max: Math.Max(max, value),
                percentile05: percentile05.Add(value),
                median: median.Add(value),
                percentile95: percentile95.Add(value));
        }
    }

    private sealed class P2QuantileEstimatorState
    {
        private readonly double quantile;
        private readonly double[] increments;
        private readonly double[] initialSamples;
        private readonly int initialSampleCount;
        private readonly double[] markerHeights;
        private readonly int[] markerPositions;
        private readonly double[] desiredPositions;

        private P2QuantileEstimatorState(
            double quantile,
            double[] increments,
            bool initialized,
            double[] initialSamples,
            int initialSampleCount,
            double[] markerHeights,
            int[] markerPositions,
            double[] desiredPositions)
        {
            this.quantile = quantile;
            this.increments = increments;
            Initialized = initialized;
            this.initialSamples = initialSamples;
            this.initialSampleCount = initialSampleCount;
            this.markerHeights = markerHeights;
            this.markerPositions = markerPositions;
            this.desiredPositions = desiredPositions;
        }

        private bool Initialized { get; }
        /// <summary>
        /// Performs the create operation.
        /// </summary>
        /// <param name="quantile">The quantile.</param>
        /// <returns>The operation result.</returns>

        public static P2QuantileEstimatorState Create(double quantile)
        {
            if (quantile is <= 0 or >= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantile),
                    "Quantile must be greater than 0 and less than 1.");
            }

            return new P2QuantileEstimatorState(
                quantile,
                increments: [0, quantile / 2, quantile, (1 + quantile) / 2, 1],
                initialized: false,
                initialSamples: [],
                initialSampleCount: 0,
                markerHeights: [],
                markerPositions: [],
                desiredPositions: []);
        }
        /// <summary>
        /// Performs the add operation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The operation result.</returns>

        public P2QuantileEstimatorState Add(double value)
        {
            if (!Initialized)
            {
                var nextInitialSamples = initialSamples.Length == 0
                    ? new double[5]
                    : (double[])initialSamples.Clone();
                nextInitialSamples[initialSampleCount] = value;
                var nextInitialSampleCount = initialSampleCount + 1;

                if (nextInitialSampleCount < 5)
                {
                    return new P2QuantileEstimatorState(
                        quantile,
                        increments,
                        initialized: false,
                        initialSamples: nextInitialSamples,
                        initialSampleCount: nextInitialSampleCount,
                        markerHeights: [],
                        markerPositions: [],
                        desiredPositions: []);
                }

                Array.Sort(nextInitialSamples, 0, nextInitialSampleCount);
                var initializedMarkerHeights = (double[])nextInitialSamples.Clone();
                var initializedMarkerPositions = new[] { 1, 2, 3, 4, 5 };
                var initializedDesiredPositions = new[]
                {
                    1d,
                    1 + (2 * quantile),
                    1 + (4 * quantile),
                    3 + (2 * quantile),
                    5d
                };

                return new P2QuantileEstimatorState(
                    quantile,
                    increments,
                    initialized: true,
                    initialSamples: [],
                    initialSampleCount: 0,
                    markerHeights: initializedMarkerHeights,
                    markerPositions: initializedMarkerPositions,
                    desiredPositions: initializedDesiredPositions);
            }

            var nextMarkerHeights = (double[])markerHeights.Clone();
            var nextMarkerPositions = (int[])markerPositions.Clone();
            var nextDesiredPositions = (double[])desiredPositions.Clone();

            int markerIndex;
            if (value < nextMarkerHeights[0])
            {
                markerIndex = 0;
                nextMarkerHeights[0] = value;
            }
            else if (value < nextMarkerHeights[1])
            {
                markerIndex = 0;
            }
            else if (value < nextMarkerHeights[2])
            {
                markerIndex = 1;
            }
            else if (value < nextMarkerHeights[3])
            {
                markerIndex = 2;
            }
            else
            {
                markerIndex = 3;
                if (value > nextMarkerHeights[4])
                {
                    nextMarkerHeights[4] = value;
                }
            }

            for (var i = markerIndex + 1; i < nextMarkerPositions.Length; i++)
            {
                nextMarkerPositions[i]++;
            }

            for (var i = 0; i < nextDesiredPositions.Length; i++)
            {
                nextDesiredPositions[i] += increments[i];
            }

            for (var i = 1; i <= 3; i++)
            {
                var delta = nextDesiredPositions[i] - nextMarkerPositions[i];
                if ((delta >= 1 && nextMarkerPositions[i + 1] - nextMarkerPositions[i] > 1)
                    || (delta <= -1 && nextMarkerPositions[i - 1] - nextMarkerPositions[i] < -1))
                {
                    var direction = Math.Sign(delta);
                    var candidate = Parabolic(nextMarkerHeights, nextMarkerPositions, i, direction);
                    if (nextMarkerHeights[i - 1] < candidate && candidate < nextMarkerHeights[i + 1])
                    {
                        nextMarkerHeights[i] = candidate;
                    }
                    else
                    {
                        nextMarkerHeights[i] = Linear(nextMarkerHeights, nextMarkerPositions, i, direction);
                    }

                    nextMarkerPositions[i] += direction;
                }
            }

            return new P2QuantileEstimatorState(
                quantile,
                increments,
                initialized: true,
                initialSamples: [],
                initialSampleCount: 0,
                markerHeights: nextMarkerHeights,
                markerPositions: nextMarkerPositions,
                desiredPositions: nextDesiredPositions);
        }
        /// <summary>
        /// Performs the get estimate operation.
        /// </summary>
        /// <returns>The operation result.</returns>

        public double GetEstimate()
        {
            if (!Initialized)
            {
                if (initialSampleCount == 0)
                {
                    return double.NaN;
                }

                var sortedValues = (double[])initialSamples.Clone();
                Array.Sort(sortedValues, 0, initialSampleCount);
                return QuantileFromSorted(sortedValues, initialSampleCount, quantile);
            }

            return markerHeights[2];
        }

        private static double Parabolic(double[] heights, int[] positions, int index, int direction)
        {
            var left = positions[index - 1];
            var current = positions[index];
            var right = positions[index + 1];

            var leftHeight = heights[index - 1];
            var currentHeight = heights[index];
            var rightHeight = heights[index + 1];

            return currentHeight + (direction / (double)(right - left))
                * (((current - left + direction) * (rightHeight - currentHeight) / (right - current))
                    + ((right - current - direction) * (currentHeight - leftHeight) / (current - left)));
        }

        private static double Linear(double[] heights, int[] positions, int index, int direction)
        {
            var adjacentIndex = index + direction;
            return heights[index]
                + (direction * (heights[adjacentIndex] - heights[index])
                    / (positions[adjacentIndex] - positions[index]));
        }

        private static double QuantileFromSorted(IReadOnlyList<double> sortedValues, int count, double quantileValue)
        {
            if (count == 1)
            {
                return sortedValues[0];
            }

            var rank = quantileValue * (count - 1);
            var lowerIndex = (int)Math.Floor(rank);
            var upperIndex = (int)Math.Ceiling(rank);

            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }

            var weight = rank - lowerIndex;
            return sortedValues[lowerIndex] + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight);
        }
    }
}
/// <summary>
/// Represents an immutable snapshot of descriptive statistics.
/// </summary>

public readonly record struct StatisticsSnapshot(
    long Count,
    double Mean,
    double Median,
    double Min,
    double Max,
    double Variance,
    double StandardDeviation,
    double Percentile05,
    double Percentile95);
