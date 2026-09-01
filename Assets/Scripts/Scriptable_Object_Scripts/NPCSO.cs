using UnityEngine;
using static Passenger;
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    public PassengerBrain prefab;

    [TextArea(3,10)]public string offenceSentence;

    public Vector2 idleDurationRange = new Vector2(10, 30);

    public float moveSpeed = 5f;
    
    public int mugShotIndex;
    
    public Habits behaviours;
    public Gender gender;
    public Ethnicity ethnicity;
    
}
