using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ParcelSorter
{
    public sealed class ParcelItem : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private RegionType region;
        [SerializeField] private XRGrabInteractable grab;
        [SerializeField] private Rigidbody body;
        public RegionType Region => region;
        public XRGrabInteractable Grab => grab;
        public Rigidbody Body => body;
        public HapticImpulsePlayer Hand { get; private set; }
        public bool Consumed { get; private set; }
        public bool Rejected { get; private set; }
        public bool Available => Consumed == false && Time.time >= nextSocket;
        private float nextSocket;
        private GameManager game;
        private BoxCollider parcelCollider;
        private readonly List<Collider> separating = new List<Collider>();

        private void Awake() => parcelCollider = GetComponent<BoxCollider>();

        public void Initialize(GameManager manager) => game = manager;
        public bool canProcess => isActiveAndEnabled;
        public bool Process(IXRSelectInteractor hand, IXRSelectInteractable target) =>
            grab.firstInteractorSelecting is XRSocketInteractor socket ? ReferenceEquals(hand, socket) : true;

        private void OnEnable()
        {
            grab.selectFilters.Add(this);
            grab.selectEntered.AddListener(Selected);
            grab.selectExited.AddListener(Released);
        }

        private void OnDisable()
        {
            grab.selectFilters.Remove(this);
            grab.selectEntered.RemoveListener(Selected);
            grab.selectExited.RemoveListener(Released);
            parcelCollider.isTrigger = false;
            for (int i = separating.Count - 1; i >= 0; i--) RestoreContact(i);
        }

        private void Selected(SelectEnterEventArgs args)
        {
            grab.throwOnDetach = args.interactorObject is XRSocketInteractor == false;
            if (grab.throwOnDetach == false) return;
            parcelCollider.isTrigger = true;
            Hand = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
            game.Feedback.Play(Cue.Grab);
            FeedbackManager.Pulse(Hand, 0.2f);
        }

        private void Released(SelectExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor || grab.isSelected || Consumed) return;
            // A solid collider restored inside another parcel would push it away on the next physics step.
            Physics.SyncTransforms();
            Vector3 halfSize = Vector3.Scale(parcelCollider.size, transform.lossyScale) * 0.5f;
            Collider[] overlaps = Physics.OverlapBox
            (
                transform.TransformPoint(parcelCollider.center), halfSize, transform.rotation,
                Physics.AllLayers, QueryTriggerInteraction.Collide
            );
            foreach (Collider other in overlaps)
            {
                if (other == parcelCollider || other.TryGetComponent<ParcelItem>(out _) == false) continue;
                if (Physics.GetIgnoreCollision(parcelCollider, other)) continue;
                Physics.IgnoreCollision(parcelCollider, other, true);
                separating.Add(other);
            }
            parcelCollider.isTrigger = false;
        }

        private void FixedUpdate()
        {
            for (int i = separating.Count - 1; i >= 0; i--)
            {
                Collider other = separating[i];
                if (other != null && Physics.ComputePenetration
                    (
                        parcelCollider, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation, out _, out _
                    )) continue;
                RestoreContact(i);
            }
        }

        private void RestoreContact(int index)
        {
            Collider other = separating[index];
            if (other != null) Physics.IgnoreCollision(parcelCollider, other, false);
            separating.RemoveAt(index);
        }

        public void Reject()
        {
            Rejected = true;
            nextSocket = Time.time + 0.75f;
        }

        public void Consume()
        {
            if (Consumed) return;
            Consumed = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
