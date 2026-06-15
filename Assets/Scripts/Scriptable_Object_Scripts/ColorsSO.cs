using System;
using UnityEngine;
using static NPC;

[CreateAssetMenu(fileName = "Colors", menuName = "Midnight Line SOs / Colors")]
public class ColorsSO : ScriptableObject
{
    public Color blackColor;
    public Color whiteColor;

    public Color[] selectableColors;

    [Range(0,1)]public float dayNight;
    [Range(0,1)]public float dayNightFactor;

    [Header("Generated")]
    public Color[] behaviourColors;
}
