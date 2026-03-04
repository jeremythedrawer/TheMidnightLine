using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;

    public Appearence appearence;
    public Behaviours behaviours;

    [Header("Difficulty")]
    public Vector2 pickBehaviourDurationRange = new Vector2(10, 30);
}
