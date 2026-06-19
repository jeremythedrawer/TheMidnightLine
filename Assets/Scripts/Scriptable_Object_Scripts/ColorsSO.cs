using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Colors", menuName = "Midnight Line SOs / Colors")]
public class ColorsSO : ScriptableObject
{
    public Color blackColor;
    public Color whiteColor;

    public Color[] selectableClueColors;

    [Range(0,1)]public float dayNight;
    [Range(0,1)]public float dayNightFactor;
}
