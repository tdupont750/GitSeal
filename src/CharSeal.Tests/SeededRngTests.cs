namespace CharSeal.Tests;

public class SeededRngTests
{
    [Fact]
    public void SameSeedProducesSameSequence()
    {
        var rng1 = new SeededRng(12345L);
        var rng2 = new SeededRng(12345L);
        for (int i = 0; i < 100; i++)
            Assert.Equal(rng1.Next(1000), rng2.Next(1000));
    }

    [Fact]
    public void DifferentSeedsProduceDifferentFirstValues()
    {
        var rng1 = new SeededRng(1L);
        var rng2 = new SeededRng(2L);
        Assert.NotEqual(rng1.Next(int.MaxValue), rng2.Next(int.MaxValue));
    }

    [Fact]
    public void NextAdvancesState()
    {
        var rng = new SeededRng(42L);
        int first = rng.Next(1000);
        int second = rng.Next(1000);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void NextAlwaysInRange()
    {
        var rng = new SeededRng(99L);
        for (int i = 0; i < 1000; i++)
        {
            int value = rng.Next(7);
            Assert.InRange(value, 0, 6);
        }
    }

    [Fact]
    public void NextBoundOneAlwaysReturnsZero()
    {
        var rng = new SeededRng(7L);
        for (int i = 0; i < 50; i++)
            Assert.Equal(0, rng.Next(1));
    }

    [Fact]
    public void NextRejectsNonPositiveBound()
    {
        var rng = new SeededRng(1L);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(-1));
    }
}
