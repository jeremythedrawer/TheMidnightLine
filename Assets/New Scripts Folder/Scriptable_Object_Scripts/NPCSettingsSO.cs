using UnityEngine;

[CreateAssetMenu(fileName = "NPC_SO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSettingsSO : ScriptableObject
{
    [Header("Running")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 14f;

    [Header("Train")]
    public int depthPositionInTrain = 1;
}
