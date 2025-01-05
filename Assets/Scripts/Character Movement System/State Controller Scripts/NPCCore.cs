using UnityEngine;

public abstract class NPCCore : StateCore
{
    [Header("NPC Core Data")]
    public BehaviourParams behaviourParams;
    public NavigationSystem navigationSystem;

    public void SetupNPCInstances()
    {
        State[] allChildStates = GetComponentsInChildren<State>();

        if (allChildStates.Length == 0)
        {
            Debug.LogError("No child states found under " + gameObject.name);
        }
        foreach (State state in allChildStates)
        {
            state.SetNpcCore(this);
        }
    }

}
