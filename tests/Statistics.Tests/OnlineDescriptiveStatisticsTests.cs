using Statistics;

namespace Statistics.Tests;

public class OnlineDescriptiveStatisticsTests
{
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

    [Test]
    public async Task Update_WithNonFiniteValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var sut = new OnlineDescriptiveStatistics();

        // Act
        var nanUpdate = async () => await Task.Run(() => sut.Update(double.NaN));
        var positiveInfinityUpdate = async () => await Task.Run(() => sut.Update(double.PositiveInfinity));
        var negativeInfinityUpdate = async () => await Task.Run(() => sut.Update(double.NegativeInfinity));

        // Assert
        await Assert.That(nanUpdate).Throws<ArgumentOutOfRangeException>();
        await Assert.That(positiveInfinityUpdate).Throws<ArgumentOutOfRangeException>();
        await Assert.That(negativeInfinityUpdate).Throws<ArgumentOutOfRangeException>();
    }
}
