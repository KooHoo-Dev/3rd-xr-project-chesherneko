using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ParcelSorter
{
    public sealed class SortingSocket : XRSocketInteractor
    {
        [SerializeField] private GameManager game;
        [SerializeField] private RegionType region;
        [SerializeField] private Renderer rim;
        [SerializeField] private ParticleSystem burst;
        private ParcelItem pending;
        private float until;
        private bool correct;
        private Color idle;

        protected override void Awake()
        {
            base.Awake();
            idle = rim.sharedMaterial.color;
        }

        private bool Eligible(Transform target) =>
            game.State == GameState.Playing && target.TryGetComponent(out ParcelItem item) && item.Available;

        public override bool CanHover(IXRHoverInteractable target) =>
            Eligible(target.transform) && base.CanHover(target);

        public override bool CanSelect(IXRSelectInteractable target) =>
            Eligible(target.transform) && base.CanSelect(target);

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            pending = args.interactableObject.transform.GetComponent<ParcelItem>();
            correct = pending.Region == region;
            until = Time.time + (correct ? 0.18f : 0.5f);
            rim.material.color = correct ? Color.green : Color.red;
            burst.Emit(8);
            game.Record(correct ? Outcome.Correct : Outcome.Wrong);
            game.Feedback.Play(correct ? Cue.Correct : Cue.Wrong);
            FeedbackManager.Pulse(pending.Hand, correct ? 0.45f : 0.85f);
        }

        private void Update()
        {
            if (pending == null) return;
            if (correct) pending.transform.localScale = Vector3.one * Mathf.Clamp01((until - Time.time) / 0.18f);
            if (Time.time < until) return;
            ParcelItem item = pending;
            if (correct == false) item.Reject();
            Clear();
            if (correct) item.Consume();
            else item.Body.AddForce(new Vector3(0f, 1.7f, -1.1f), ForceMode.VelocityChange);
        }

        public void Clear()
        {
            if (pending != null && IsSelecting(pending.Grab))
                interactionManager.SelectExit((IXRSelectInteractor)this, (IXRSelectInteractable)pending.Grab);
            pending = null;
            rim.material.color = idle;
            burst.Clear();
        }
    }
}
