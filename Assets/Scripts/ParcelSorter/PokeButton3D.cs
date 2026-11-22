using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace ParcelSorter
{
    public sealed class PokeButton3D : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable button;
        [SerializeField] private XRPokeFilter filter;
        [SerializeField] private Transform cap;
        [SerializeField] private TextMeshProUGUI caption;
        public event Action Clicked = delegate { };
        public void Label(string value) => caption.text = value;
        private void OnEnable() => button.selectEntered.AddListener(Press);
        private void OnDisable() => button.selectEntered.RemoveListener(Press);
        private void Update()
        {
            cap.localPosition = Vector3.forward * (0.022f * filter.pokeStateData.Value.interactionStrength);
        }
        private void Press(SelectEnterEventArgs args)
        {
            FeedbackManager.Pulse(args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>(), 0.35f);
            Clicked();
        }
    }
}
