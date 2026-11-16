using ManhattanSwing.Core;
using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// Plays the sound effects. The sounds themselves are made in <see cref="SoundSynth"/>; to use a
    /// recorded sound instead, load an AudioClip here in place of the synthesised one.
    /// </summary>
    public sealed class GameAudio
    {
        /// <summary>Wind is silent below this speed (m/s) and at full volume at <see cref="WindFullSpeed"/>.</summary>
        public float WindStartSpeed = 8f;
        public float WindFullSpeed = 60f;
        public float WindVolume = 0.6f;

        private readonly AudioSource effects, wind;
        private readonly AudioClip webShoot, webAttach, ringPass, finish, countdownBeep, goBeep;

        public GameAudio(GameObject host)
        {
            effects = host.AddComponent<AudioSource>();
            effects.playOnAwake = false;

            wind = host.AddComponent<AudioSource>();
            wind.clip = Clip("Wind", SoundSynth.WindLoop());
            wind.loop = true;
            wind.volume = 0;
            wind.Play();

            webShoot = Clip("Web shoot", SoundSynth.WebShoot());
            webAttach = Clip("Web attach", SoundSynth.WebAttach());
            ringPass = Clip("Ring", SoundSynth.RingPass());
            finish = Clip("Finish", SoundSynth.Finish());
            countdownBeep = Clip("Countdown", SoundSynth.CountdownBeep());
            goBeep = Clip("Go", SoundSynth.GoBeep());
        }

        public void WebShoot() { effects.PlayOneShot(webShoot); }
        public void WebAttach() { effects.PlayOneShot(webAttach); }
        public void RingPass() { effects.PlayOneShot(ringPass); }
        public void Finish() { effects.PlayOneShot(finish); }
        public void CountdownBeep() { effects.PlayOneShot(countdownBeep); }
        public void GoBeep() { effects.PlayOneShot(goBeep); }

        /// <summary>Call every frame: the wind gets louder and higher as the player speeds up.</summary>
        public void SetSpeed(float metresPerSecond)
        {
            float amount = Mathf.InverseLerp(WindStartSpeed, WindFullSpeed, metresPerSecond);
            wind.volume = amount * WindVolume;
            wind.pitch = Mathf.Lerp(0.7f, 1.5f, amount);
        }

        private static AudioClip Clip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SoundSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
