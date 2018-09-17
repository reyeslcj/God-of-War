using UnityEngine;

namespace CJ.GodOfWar
{
    public class AxeAudio : MonoBehaviour
    {
        public RandomAudio axeSpin;
        public RandomAudio axeHit;
        public AudioSource axeRumble;

        public void PlayRandomSpin()
        {
            axeSpin.Play();
        }

        public void PlayRandomHit()
        {
            axeHit.Play();
        }
    }
}