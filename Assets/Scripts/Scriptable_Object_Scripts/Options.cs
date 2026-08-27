using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Options", menuName = "Data / Options")]
public class Options : ScriptableObject
{
    public AtlasSO patternAtlas;

    public IconButton colorButtonPrefab;

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
    public Color darkColor;
    public Color lightColor;

    public int darkColorIndex;
    public int lightColorIndex;

    public bool useBayer;


    [Header("Generated")]
    public int darkColorID;
    public int lightColorID;
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
        darkColorID = Shader.PropertyToID("_BlackColor");
        lightColorID = Shader.PropertyToID("_WhiteColor");
        meridiaColorID = Shader.PropertyToID("_MeridiaColor");
        patternTextureID = Shader.PropertyToID("_PatternTexture");
        vinroseColorID = Shader.PropertyToID("_VinroseColor");

        Shader.SetGlobalColor(darkColorID, darkColor.linear);
        Shader.SetGlobalColor(lightColorID, lightColor.linear);
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