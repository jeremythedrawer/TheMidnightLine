using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class NPCCore : StateCore
{
    [Header("NPC Core Data")]
    public BehaviourParams behaviourParams;
    //agent
    [HideInInspector] public AgentController agentController;
    //bystander
    [HideInInspector] public BystanderController bystanderController;

    public bool isSitting {  get; set; }
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
