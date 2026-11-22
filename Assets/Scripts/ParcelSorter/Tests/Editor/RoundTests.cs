using NUnit.Framework;
using ParcelSorter;

public class RoundTests
{
    [Test]
    public void RoundTracksEveryOutcome()
    {
        RoundScore score = new RoundScore();
        Assert.AreEqual(0f, score.Accuracy);
        score.Record(Outcome.Correct);
        Assert.AreEqual(10, score.Points);
        Assert.AreEqual(1, score.Combo);
        score.Record(Outcome.Wrong);
        Assert.AreEqual(5, score.Points);
        Assert.AreEqual(0, score.Combo);
        score.Record(Outcome.Miss);
        Assert.AreEqual(2, score.Points);
        Assert.AreEqual(100f / 3, score.Accuracy, 0.001f);
    }

    [TestCase(0, 0, "F")]
    [TestCase(499, 1000, "F")]
    [TestCase(500, 1000, "C")]
    [TestCase(699, 1000, "C")]
    [TestCase(700, 1000, "B")]
    [TestCase(799, 1000, "B")]
    [TestCase(800, 1000, "A")]
    [TestCase(899, 1000, "A")]
    [TestCase(900, 1000, "S")]
    [TestCase(999, 1000, "S")]
    [TestCase(1999, 2000, "S")]
    [TestCase(1, 1, "PERFECT!!")]
    public void GradeBoundaries(int correct, int attempts, string grade)
    {
        RoundScore score = new RoundScore();
        for (int i = 0; i < correct; i++) score.Record(Outcome.Correct);
        for (int i = correct; i < attempts; i++) score.Record(Outcome.Wrong);
        Assert.AreEqual(grade, score.Grade);
    }

    [TestCase(60f, 2f)]
    [TestCase(30f, 1.5f)]
    [TestCase(0f, 1f)]
    public void SpawnCadence(float time, float expected) => Assert.AreEqual(expected, RoundScore.Interval(time));

    [Test]
    public void TimerStopsAtZero()
    {
        Assert.AreEqual(59.5f, RoundScore.Tick(60f, 0.5f));
        Assert.AreEqual(0f, RoundScore.Tick(0.1f, 0.2f));
    }
}
