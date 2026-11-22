using UnityEngine;

namespace ParcelSorter
{
    public sealed class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Renderer surface;
        private Material belt;
        private void Awake() => belt = surface.material;
        private void Update()
        {
            if (game.State == GameState.Playing)
                belt.mainTextureOffset += Vector2.right * (Time.deltaTime * 0.8f * game.SpeedMultiplier);
        }
        private void OnCollisionStay(Collision collision)
        {
            if (game.State != GameState.Playing || collision.rigidbody == null) return;
            if (collision.rigidbody.TryGetComponent(out ParcelItem item) == false || item.Grab.isSelected) return;
            Vector3 velocity = item.Body.linearVelocity;
            velocity.x = 0.36f * game.SpeedMultiplier;
            item.Body.linearVelocity = velocity;
        }
    }
}
