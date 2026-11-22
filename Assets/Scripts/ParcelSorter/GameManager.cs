using UnityEngine;
using Sirenix.OdinInspector;

namespace ParcelSorter
{
    public enum GameState { Title, Playing, Result }

    public sealed class GameManager : MonoBehaviour
    {
        [Title("Round")]
        [SerializeField] private GameUIManager ui;
        [SerializeField] private ParcelSpawner spawner;
        [SerializeField] private SortingSocket[] sockets;
        [SerializeField] private FeedbackManager feedback;
        [ShowInInspector, ReadOnly] public GameState State { get; private set; }
        public FeedbackManager Feedback => feedback;
        public RoundScore Score { get; private set; } = new RoundScore();
        private float remaining;
        private const string BestKey = "ParcelSorter.Best";

        private void Start() => ui.ShowTitle();

        public void Begin()
        {
            if (State == GameState.Playing) return;
            Score = new RoundScore();
            remaining = RoundScore.Duration;
            State = GameState.Playing;
            feedback.Play(Cue.Button);
            ui.ShowPlay();
            ui.Refresh(60, Score);
        }

        public void Record(Outcome outcome)
        {
            if (State != GameState.Playing) return;
            Score.Record(outcome);
            ui.Refresh(Mathf.CeilToInt(remaining), Score);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;
            int previous = Mathf.CeilToInt(remaining);
            remaining = RoundScore.Tick(remaining, Time.deltaTime);
            if (remaining == 0f)
            {
                State = GameState.Result;
                foreach (SortingSocket socket in sockets) socket.Clear();
                spawner.Clear();
                int best = Mathf.Max(PlayerPrefs.GetInt(BestKey, Score.Points), Score.Points);
                PlayerPrefs.SetInt(BestKey, best);
                PlayerPrefs.Save();
                feedback.Play(Cue.Finish);
                ui.ShowResult(Score, best);
                return;
            }
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds != previous)
            {
                if (seconds <= 10) feedback.Play(Cue.Tick);
                ui.Refresh(seconds, Score);
            }
            spawner.Advance(this, remaining);
        }
    }
}
