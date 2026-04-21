using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;

    public Behaviours behaviours;
    public Gender gender;
    public Ethnicity ethnicity;
    public int uncoveredMugshotIndex;
    public int coveredMugshotIndex;
    [Header("Difficulty")]
    public Vector2 idleDurationRange = new Vector2(10, 30);
}
