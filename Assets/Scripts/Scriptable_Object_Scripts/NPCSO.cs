using UnityEngine;
using static NPCTraits;
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;

    [Header("Station")]
    public float maxDistanceDetection = 6.0f;

    public Appearence appearence;
    public Behaviours behaviours;

    [Header("Difficulty")]
    public Vector2 pickBehaviourDurationRange = new Vector2(10, 30);
}
