using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings_SO", menuName = "Midnight Line SOs / Camera Settings SO")]
public class CameraSettingsSO : ScriptableObject
{
    [Range(0f, 20f)] public float horizontalOffset = 10f;

    public float damping = 5.0f;

    public float shakeTime = 0.5f;
    public float shakeIntensity = 0.4f;

    [Range(1f, 2f)] public float fallingSizeMultiplier = 1.5f;
    public float fallingOffset = 0.3f;
    public int fallThreshold;
    public float maxProjectionSize = 10.0f;
    public bool turnOnGUI;
}