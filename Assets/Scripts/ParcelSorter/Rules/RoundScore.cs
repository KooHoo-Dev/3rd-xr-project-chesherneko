using UnityEngine;

namespace ParcelSorter
{
    public enum RegionType { Seoul, Busan, Daegu, Gwangju }
    public enum Outcome { Correct, Wrong, Miss }

    public sealed class RoundScore
    {
        public const float Duration = 60f;
        public int Points { get; private set; }
        public int Correct { get; private set; }
        public int Attempts { get; private set; }
        public int Combo { get; private set; }
        public float Accuracy => Attempts == 0 ? 0f : 100f * Correct / Attempts;
        public float DisplayedAccuracy => Attempts == 0 ? 0f : (1000L * Correct / Attempts) / 10f;
        public string Grade => DisplayedAccuracy switch
        {
            100f => "PERFECT!!",
            >= 90f => "S",
            >= 80f => "A",
            >= 70f => "B",
            >= 50f => "C",
            _ => "F"
        };

        public void Record(Outcome outcome)
        {
            Attempts++;
            if (outcome == Outcome.Correct)
            {
                Points += 10;
                Correct++;
                Combo++;
            }
            else
            {
                Points -= outcome == Outcome.Wrong ? 5 : 3;
                Combo = 0;
            }
        }

        public static float Interval(float remaining) => Mathf.Lerp(1f, 2f, remaining / Duration);
        public static float Tick(float remaining, float delta) => Mathf.Max(0f, remaining - delta);
    }
}
