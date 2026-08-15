using System;
using UnityEngine;

using static AtlasUI;

[CreateAssetMenu(fileName = "Options", menuName = "Midnight Line SOs / Options")]
public class OptionsSO : ScriptableObject
{
    public Texture2D diagonalTexture;
    public Texture2D stripesTexture;
    public Color meridiaColor;

    public Color[] selectableClueColors;
    public Color[] selectableDarkColors;
    public Color[] selectableLightColors;

    [Range(0,1)] public float dayNightFactor;

    [TextArea(3, 10)] public string traitorCountTutorialText;
    
    [TextArea(3, 10)] public string passengerRuleOutTutorialText1;
    [TextArea(3, 10)] public string passengerRuleOutTutorialText2;

    [TextArea(3, 10)] public string passengerColorMarkerTutorialText1;
    [TextArea(3, 10)] public string passengerColorMarkerTutorialText2;
    [TextArea(3, 10)] public string passengerColorMarkerTutorialText3;

    [TextArea(3, 10)] public string passengerMulticolorMarkerTutorialText1;
    [TextArea(3, 10)] public string passengerMulticolorMarkerTutorialText2;

    [Header("User Picked")]
    public Color blackColor;
    public Color whiteColor;
    public bool useBayer;

    [Header("Generated")]
    public PickerState curState;
}
