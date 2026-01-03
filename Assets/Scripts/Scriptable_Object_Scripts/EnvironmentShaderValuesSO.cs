using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentShaderValues_SO", menuName = "Midnight Line SOs / Environment Shader Values SO")]
public class EnvironmentShaderValuesSO : ScriptableObject
{
    internal int densityID;
    internal int scrollTimeID;

    public float fadeDensityTime = 1.0f;
    public float minScrollSpeed = 0.01f;
    public float maxDensity = 2;
    internal float curScrollSpeed;
    internal float curScrollTime;
    internal float curDensity;
    internal float targetDensity;
}
