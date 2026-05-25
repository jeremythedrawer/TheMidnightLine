using UnityEngine;

[CreateAssetMenu(fileName = "SpySettings_SO", menuName = "Midnight Line SOs / Spy Settings SO")]
public class SpySettingsSO : ScriptableObject
{
    [Header("Running")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 14f;

    [Header("Collisions")]
    [Range(0f, 0.5f)] public float groundBufferVertical = 0.02f;
    [Range(0f, 1f)] public float groundBufferHorizontal = 0.5f;
    public float wallWidthBuffer = 0.5f;
    public float wasteBuffer = 0.5f;

    [Header("Train")]
    [Range(0f, 1f)] public int depthPositionInTrain = 1;
}
