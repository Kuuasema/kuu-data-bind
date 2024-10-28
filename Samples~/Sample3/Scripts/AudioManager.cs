using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

using Kuuasema.Utils;
using Kuuasema.DataBinding;

public class AudioManager : MonoBehaviour {

    [SerializeField] private AudioSource sourceMusic;
    [SerializeField] private AudioSource sourceEffect;

    private void Start() {
        Demo.DataModel.Settings.MusicVolume.OnValueUpdated += this.OnMusicVolumeChange;
        Demo.DataModel.Settings.EffectVolume.OnValueUpdated += this.OnEffectVolumeChange;

        this.sourceMusic.Play();
    }

    private void OnMusicVolumeChange(float volume) {
        this.sourceMusic.volume = volume;
    }

    private void OnEffectVolumeChange(float volume) {
        this.sourceEffect.volume = volume;
    }
}
