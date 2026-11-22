using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace ParcelSorter
{
    public enum Cue { Grab, Correct, Wrong, Miss, Button, Tick, Finish }

    public sealed class FeedbackManager : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip[] clips;

        public void Play(Cue cue) => source.PlayOneShot(clips[(int)cue]);
        public static void Pulse(HapticImpulsePlayer hand, float strength)
        {
            if (hand != null) hand.SendHapticImpulse(strength, 0.09f);
        }
    }
}
