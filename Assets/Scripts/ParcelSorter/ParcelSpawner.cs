using FantasyGarden.Utility;
using UnityEngine;

namespace ParcelSorter
{
    public sealed class ParcelSpawner : MonoBehaviour
    {
        [SerializeField] private ParcelItem[] parcels;
        [SerializeField] private Transform spawn;
        [SerializeField] private Transform inventory;
        private float untilSpawn;

        public void Advance(GameManager game, float remaining)
        {
            untilSpawn -= Time.deltaTime;
            if (untilSpawn > 0f) return;
            ParcelItem item = Instantiate(parcels.GetRandom(), spawn.position, spawn.rotation, inventory);
            item.Initialize(game);
            untilSpawn += RoundScore.Interval(remaining);
        }

        public void Clear()
        {
            untilSpawn = 0f;
            foreach (Transform parcel in inventory) parcel.GetComponent<ParcelItem>().Consume();
        }
    }
}
