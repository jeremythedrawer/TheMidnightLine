using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static AtlasRendering;
using static AtlasUI;

[CreateAssetMenu(fileName = "Options", menuName = "Data / Options")]
public class Options : ScriptableObject
{
    public AtlasSO patternAtlas;

    public Color meridiaColor;
    public Color vinroseColor;

    public Color[] selectableDarkColors;
    public Color[] selectableLightColors;

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
    public int blackColorID;
    public int whiteColorID;
    public int meridiaColorID;
    public int vinroseColorID;
    public int patternTextureID;

    public int selectedPatternIndex;
    private void OnEnable()
    {
        SetGlobalShaderVariables();
    }
    public void SetGlobalShaderVariables()
    {
        blackColorID = Shader.PropertyToID("_BlackColor");
        whiteColorID = Shader.PropertyToID("_WhiteColor");
        meridiaColorID = Shader.PropertyToID("_MeridiaColor");
        patternTextureID = Shader.PropertyToID("_PatternTexture");
        vinroseColorID = Shader.PropertyToID("_VinroseColor");

        Shader.SetGlobalColor(blackColorID, blackColor.linear);
        Shader.SetGlobalColor(whiteColorID, whiteColor.linear);
        Shader.SetGlobalColor(meridiaColorID, meridiaColor.linear);
        Shader.SetGlobalColor(vinroseColorID, vinroseColor.linear);

        Shader.SetGlobalTexture(patternTextureID, patternAtlas.texture);
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(Options))]
public class OptionsSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Options options = (Options)target;
        GUIContent setGlobalVariableContent = new GUIContent("Set Global Shader Variables");

        if (GUILayout.Button(setGlobalVariableContent))
        {
            options.SetGlobalShaderVariables();
        }
    }
}
#endif