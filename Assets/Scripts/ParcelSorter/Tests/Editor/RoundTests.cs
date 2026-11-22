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

    [TestCase(200, "S")]
    [TestCase(199, "A")]
    [TestCase(140, "A")]
    [TestCase(139, "B")]
    [TestCase(80, "B")]
    [TestCase(79, "C")]
    public void GradeBoundaries(int score, string grade) => Assert.AreEqual(grade, RoundScore.Grade(score));

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
