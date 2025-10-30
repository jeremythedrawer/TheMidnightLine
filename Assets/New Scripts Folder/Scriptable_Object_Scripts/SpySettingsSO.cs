using UnityEngine;

[CreateAssetMenu(fileName = "SpySettings_SO", menuName = "Midnight Line SOs / Spy Settings SO")]
public class SpySettingsSO : ScriptableObject
{
    [Header("Running")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 14f;

    [Header("Jumping")]
    public float jumpVerticalForce = 4f;
    [Range(1f, 40f)] public float jumpHorizontalForce = 20f;
    [Range(0f, 1f)] public float jumpBufferTime = 0.2f;
    public float maxFallSpeed = 6f;
    [Range(0f, 0.25f)] public float coyoteTime = 0.125f;
    public float antiGravApexThreshold = 3f;
    public float gravityScale = 6f;
    [Range(0f, 1f)] public float antiGravMultiplier = 0.5f;

    [Header("Collisions")]
    [Range(0f, 0.5f)] public float groundBufferVertical = 0.02f;
    [Range(0f, 1f)] public float groundBufferHorizontal = 0.5f;

    [Header("Train")]
    public int depthPositionInTrain = 1;
}
