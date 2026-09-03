using UnityEngine;
using System;
using static AtlasUI;



#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Options", menuName = "Data / Options")]
public class Options : ScriptableObject
{
    [Serializable] public struct Music
    {
        public AudioClip menu;
        public float volume;
    }

    [Serializable] public struct SoundEffects
    {
        public AudioClip cursorHover;
        public AudioClip cursorClick;
        public float volume;
    }

    public AtlasSO patternAtlas;

    public IconButton colorButtonPrefab;

    public Music music;
    public SoundEffects soundEffects;

    public Color meridiaColor;
    public Color vinroseColor;

    public Color[] selectableDarkColors;
    public Color[] selectableLightColors;

    public RegionData thirdPointRegion;
    public RegionData secondPointRegion;
    public RegionData firstPointRegion;
    public RegionData capitalRegion;


    public SpyData spyData;

    public float dayNightTransitionTime = 5f;
    [Header("User Picked")]
    public RegionData curRegion;
    public TripData curTrip;
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
    public int dayNightID;

    public int selectedPatternIndex;

    public TutorialState tutorialsCompleted;
    public TutorialState curTutorialState;

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
        dayNightID = Shader.PropertyToID("_DayNight");


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
    float dayNight = 0;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Options options = (Options)target;
        GUIContent setGlobalVariableContent = new GUIContent("Set Global Shader Variables");

        if (GUILayout.Button(setGlobalVariableContent))
        {
            options.SetGlobalShaderVariables();
        }

        EditorGUI.BeginChangeCheck();
        dayNight = EditorGUILayout.Slider(dayNight, 0, 1);

        if (EditorGUI.EndChangeCheck())
        {
            Shader.SetGlobalFloat(options.dayNightID, dayNight);
        }
    }
}
#endif