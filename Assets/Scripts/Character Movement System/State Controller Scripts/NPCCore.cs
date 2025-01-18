using UnityEngine;

public abstract class NPCCore : StateCore
{
    [Header("NPC Core Data")]
    public BehaviourParams behaviourParams;
    //agent
    [HideInInspector] public AgentController agentController;
    //bystander
    [HideInInspector] public BystanderController bystanderController;
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
