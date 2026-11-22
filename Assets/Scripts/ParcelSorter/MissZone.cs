using UnityEngine;

namespace ParcelSorter
{
    public sealed class MissZone : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        private void OnTriggerStay(Collider other)
        {
            if (game.State != GameState.Playing || other.TryGetComponent(out ParcelItem item) == false) return;
            if (item.Consumed || item.Grab.isSelected) return;
            if (item.Rejected == false)
            {
                game.Record(Outcome.Miss);
                game.Feedback.Play(Cue.Miss);
            }
            item.Consume();
        }
    }
}
