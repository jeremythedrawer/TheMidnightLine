using System;
using UnityEngine;

using static AtlasUI;

[CreateAssetMenu(fileName = "Colors", menuName = "Midnight Line SOs / Colors")]
public class ColorsSO : ScriptableObject
{
    public Texture2D diagonalTexture;

    public Color blackColor;
    public Color whiteColor;

    public Color[] selectableClueColors;

    [Range(0,1)]public float dayNight;
    [Range(0,1)]public float dayNightFactor;


    [Header("Generated")]
    public PickerState curState;
    public PickerState enteredState;
}
