using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;


    public Appearance appearence;
    public Behaviours behaviours;
    public Gender gender;
    public Ethnicity ethnicity;
    public int mugshotIndex;
    [Header("Difficulty")]
    public Vector2 pickBehaviourDurationRange = new Vector2(10, 30);
}
