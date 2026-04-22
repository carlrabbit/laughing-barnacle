using Statistics;

namespace Statistics.Tests;

/// <summary>
/// Documentation.
/// </summary>
public class OnlineDescriptiveStatisticsTests
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task Update_WithRangeOfValues_ComputesBasicStatistics()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();

        // Act
        for (var value = 1; value <= 100; value++)
        {
            sut.Update(value);
        }

        // Assert
        await Assert.That(sut.Count).IsEqualTo(100);
        await Assert.That(sut.Mean).IsEqualTo(50.5);
        await Assert.That(sut.Min).IsEqualTo(1d);
        await Assert.That(sut.Max).IsEqualTo(100d);
        await Assert.That(sut.Variance).IsEqualTo(833.25);
        await Assert.That(sut.StandardDeviation).IsEqualTo(Math.Sqrt(833.25));
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task Update_WithRangeOfValues_ApproximatesMedianAndPercentiles()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();

        // Act
        for (var value = 1; value <= 100; value++)
        {
            sut.Update(value);
        }

        // Assert
        await Assert.That(Math.Abs(sut.Median - 50.5)).IsLessThanOrEqualTo(1d);
        await Assert.That(Math.Abs(sut.Percentile95 - 95.05)).IsLessThanOrEqualTo(2d);
        await Assert.That(Math.Abs(sut.Percentile05 - 5.95)).IsLessThanOrEqualTo(2d);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task Update_WithNonFiniteValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();

        // Act
        Action nanUpdate = () => sut.Update(double.NaN);
        Action positiveInfinityUpdate = () => sut.Update(double.PositiveInfinity);
        Action negativeInfinityUpdate = () => sut.Update(double.NegativeInfinity);

        // Assert
        await Assert.That(nanUpdate).Throws<ArgumentOutOfRangeException>();
        await Assert.That(positiveInfinityUpdate).Throws<ArgumentOutOfRangeException>();
        await Assert.That(negativeInfinityUpdate).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task Update_WithConcurrentWriters_RemainsThreadSafeAndAccurate()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();
        const int workerCount = 8;
        const int valuesPerWorker = 1000;
        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() =>
            {
                for (var value = 1; value <= valuesPerWorker; value++)
                {
                    sut.Update(value);
                }
            }))
            .ToArray();

        // Act
        await Task.WhenAll(workers);

        // Assert
        await Assert.That(sut.Count).IsEqualTo(workerCount * valuesPerWorker);
        await Assert.That(Math.Abs(sut.Mean - 500.5)).IsLessThan(1e-9);
        await Assert.That(Math.Abs(sut.Variance - 83333.25)).IsLessThan(1e-6);
        await Assert.That(sut.Min).IsEqualTo(1d);
        await Assert.That(sut.Max).IsEqualTo(1000d);
        await Assert.That(sut.Median).IsGreaterThanOrEqualTo(480d);
        await Assert.That(sut.Median).IsLessThanOrEqualTo(520d);
        await Assert.That(sut.Percentile05).IsGreaterThanOrEqualTo(40d);
        await Assert.That(sut.Percentile95).IsLessThanOrEqualTo(960d);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetSnapshot_AfterUpdates_ReturnsConsistentStatisticsView()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();
        for (var value = 1; value <= 100; value++)
        {
            sut.Update(value);
        }

        // Act
        var snapshot = sut.GetSnapshot();

        // Assert
        await Assert.That(snapshot.Count).IsEqualTo(100);
        await Assert.That(snapshot.Mean).IsEqualTo(50.5);
        await Assert.That(snapshot.Min).IsEqualTo(1d);
        await Assert.That(snapshot.Max).IsEqualTo(100d);
        await Assert.That(snapshot.Variance).IsEqualTo(833.25);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetSnapshot_DuringConcurrentUpdates_ProducesConsistentInvariantValues()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();
        const int workerCount = 4;
        const int iterationsPerWorker = 5000;
        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < iterationsPerWorker; i++)
                {
                    sut.Update((i % 1000) + 1);
                }
            }))
            .ToArray();

        // Act
        while (workers.Any(worker => !worker.IsCompleted))
        {
            var snapshot = sut.GetSnapshot();
            if (snapshot.Count > 0)
            {
                await Assert.That(snapshot.Min <= snapshot.Max).IsTrue();
                await Assert.That(snapshot.Mean >= snapshot.Min).IsTrue();
                await Assert.That(snapshot.Mean <= snapshot.Max).IsTrue();
                await Assert.That(snapshot.Percentile05 >= snapshot.Min).IsTrue();
                await Assert.That(snapshot.Percentile95 <= snapshot.Max).IsTrue();
                await Assert.That(snapshot.StandardDeviation >= 0).IsTrue();
            }

            await Task.Yield();
        }

        await Task.WhenAll(workers);

        // Assert
        var finalSnapshot = sut.GetSnapshot();
        await Assert.That(finalSnapshot.Count).IsEqualTo(workerCount * iterationsPerWorker);
    }
}
