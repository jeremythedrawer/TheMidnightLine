using UnityEngine;

public abstract class NPCCore : StateCore
{
    [Header("NPC Core Data")]
    public BehaviourParams behaviourParams;
    public NavigationSystem navigationSystem;

}
