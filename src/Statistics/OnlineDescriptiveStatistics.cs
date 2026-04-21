namespace Statistics;

public sealed class OnlineDescriptiveStatistics
{
    private readonly P2QuantileEstimator percentile05Estimator = new(0.05);
    private readonly P2QuantileEstimator medianEstimator = new(0.5);
    private readonly P2QuantileEstimator percentile95Estimator = new(0.95);

    private long count;
    private double mean;
    private double m2;
    private double min = double.PositiveInfinity;
    private double max = double.NegativeInfinity;

    public long Count => count;
    public double Mean => count == 0 ? double.NaN : mean;
    public double Median => medianEstimator.GetEstimate();
    public double Max => count == 0 ? double.NaN : max;
    public double Min => count == 0 ? double.NaN : min;
    public double Variance => count == 0 ? double.NaN : m2 / count;
    public double StandardDeviation => count == 0 ? double.NaN : Math.Sqrt(Variance);
    public double Percentile95 => percentile95Estimator.GetEstimate();
    public double Percentile05 => percentile05Estimator.GetEstimate();

    public void Update(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be finite.");
        }

        count++;

        var delta = value - mean;
        mean += delta / count;
        var delta2 = value - mean;
        m2 += delta * delta2;

        min = Math.Min(min, value);
        max = Math.Max(max, value);

        percentile05Estimator.Add(value);
        medianEstimator.Add(value);
        percentile95Estimator.Add(value);
    }

    private sealed class P2QuantileEstimator
    {
        private readonly List<double> initialSamples = [];
        private readonly double[] markerHeights = new double[5];
        private readonly int[] markerPositions = new int[5];
        private readonly double[] desiredPositions = new double[5];
        private readonly double[] increments;
        private readonly double quantile;

        private bool initialized;

        public P2QuantileEstimator(double quantile)
        {
            if (quantile is <= 0 or >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantile), "Quantile must be between 0 and 1.");
            }

            this.quantile = quantile;
            increments = [0, quantile / 2, quantile, (1 + quantile) / 2, 1];
        }

        public void Add(double value)
        {
            if (!initialized)
            {
                initialSamples.Add(value);
                if (initialSamples.Count == 5)
                {
                    Initialize();
                }

                return;
            }

            int markerIndex;
            if (value < markerHeights[0])
            {
                markerIndex = 0;
                markerHeights[0] = value;
            }
            else if (value < markerHeights[1])
            {
                markerIndex = 0;
            }
            else if (value < markerHeights[2])
            {
                markerIndex = 1;
            }
            else if (value < markerHeights[3])
            {
                markerIndex = 2;
            }
            else
            {
                markerIndex = 3;

                if (value > markerHeights[4])
                {
                    markerHeights[4] = value;
                }
            }

            for (var i = markerIndex + 1; i < markerPositions.Length; i++)
            {
                markerPositions[i]++;
            }

            for (var i = 0; i < desiredPositions.Length; i++)
            {
                desiredPositions[i] += increments[i];
            }

            for (var i = 1; i <= 3; i++)
            {
                var delta = desiredPositions[i] - markerPositions[i];
                if ((delta >= 1 && markerPositions[i + 1] - markerPositions[i] > 1)
                    || (delta <= -1 && markerPositions[i - 1] - markerPositions[i] < -1))
                {
                    var direction = Math.Sign(delta);
                    var candidate = Parabolic(i, direction);
                    if (markerHeights[i - 1] < candidate && candidate < markerHeights[i + 1])
                    {
                        markerHeights[i] = candidate;
                    }
                    else
                    {
                        markerHeights[i] = Linear(i, direction);
                    }

                    markerPositions[i] += direction;
                }
            }
        }

        public double GetEstimate()
        {
            if (initialSamples.Count == 0 && !initialized)
            {
                return double.NaN;
            }

            if (!initialized)
            {
                initialSamples.Sort();
                return QuantileFromSorted(initialSamples, quantile);
            }

            return markerHeights[2];
        }

        private void Initialize()
        {
            initialSamples.Sort();
            for (var i = 0; i < markerHeights.Length; i++)
            {
                markerHeights[i] = initialSamples[i];
                markerPositions[i] = i + 1;
            }

            desiredPositions[0] = 1;
            desiredPositions[1] = 1 + (2 * quantile);
            desiredPositions[2] = 1 + (4 * quantile);
            desiredPositions[3] = 3 + (2 * quantile);
            desiredPositions[4] = 5;

            initialSamples.Clear();
            initialized = true;
        }

        private double Parabolic(int index, int direction)
        {
            var left = markerPositions[index - 1];
            var current = markerPositions[index];
            var right = markerPositions[index + 1];

            var leftHeight = markerHeights[index - 1];
            var currentHeight = markerHeights[index];
            var rightHeight = markerHeights[index + 1];

            return currentHeight + (direction / (double)(right - left))
                * (((current - left + direction) * (rightHeight - currentHeight) / (right - current))
                    + ((right - current - direction) * (currentHeight - leftHeight) / (current - left)));
        }

        private double Linear(int index, int direction)
        {
            var adjacentIndex = index + direction;
            return markerHeights[index]
                + (direction * (markerHeights[adjacentIndex] - markerHeights[index])
                    / (markerPositions[adjacentIndex] - markerPositions[index]));
        }

        private static double QuantileFromSorted(IReadOnlyList<double> sortedValues, double quantileValue)
        {
            if (sortedValues.Count == 1)
            {
                return sortedValues[0];
            }

            var rank = quantileValue * (sortedValues.Count - 1);
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
