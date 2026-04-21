using UnityEngine;
using UnityEngine.VFX;
using static Atlas;
using static NPC;

[CreateAssetMenu(fileName = "name_BehaviourContext", menuName = "Midnight Line SOs / NPC Behaviour Context")]
public class NPCBehaviourContextSO : ScriptableObject
{
    public Behaviours behaviour;

    public VisualEffect glyphPrefab;

    public float minTime;
    public float maxTime;

    public NPCMotion sittingMotion;
    public NPCMotion standingMotion;

    public NPCPath pathToTake;
}
