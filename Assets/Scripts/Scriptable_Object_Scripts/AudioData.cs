using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "Data / Audio")]

public class AudioData : ScriptableObject
{
    [Header("Music")]
    public AudioClip menu;

    [Header("Sound Effects")]
    [Header("UI")]
    public AudioClip cursorHover;
    public AudioClip cursorClick;
    public AudioClip gong;
    public AudioClip unlock;
    [Header("Ambience")]
    public AudioClip wind;
    public AudioClip stationAmbience;
    public AudioClip suitcaseClick;
    public AudioClip sweep;
    public AudioClip pageFlipUp;
    public AudioClip[] footStepsConcrete;
    [Header("Voices")]
    public AudioClip[] wuh;

    [Header("User Generated")]
    public float soundEffectsVolume;
    public float musicVolume;
}
