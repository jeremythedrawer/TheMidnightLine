using UnityEngine;

public abstract class NPCCore : StateCore
{
    [Header("NPC Core References")]
    public BehaviourParams behaviourParams;
    public PathData pathData;
    
    public NPCController controller;

    public bool isSitting {  get; set; }
    public bool isStanding { get; set; }
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
