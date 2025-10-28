using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings_SO", menuName = "Midnight Line SOs / Camera Settings SO")]
public class CameraSettingsSO : ScriptableObject
{
    [Range(-1f, 1f)] public float verticalOffset = 0f;
    [Range(0f, 20f)] public float horizontalOffset = 10f;

    public float damping = 5.0f;

    public float shakeTime = 0.5f;
    public float shakeIntensity = 0.4f;

    [Header("Airborne")]
    [Range(1f, 2f)] public float fallingSizeMultiplier = 1.5f;
    public float fallingOffset = 0.3f;
    [Range(0, 640)] public int fallThreshold;

    public bool turnOnGUI;
}