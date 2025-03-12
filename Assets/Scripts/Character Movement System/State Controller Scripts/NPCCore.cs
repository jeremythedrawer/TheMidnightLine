using UnityEngine;

public class NPCCore : StateCore
{
    [Header("NPC Core References")]
    public BehaviourParams behaviourParams;
    public PathData pathData;
    
    public NPCController controller;
    public StationData startingStation {  get; set; }
    public AnimatorOverrideController animatorOverride { get; set; }
    public Material material { get; set; }
    public bool isSitting {  get; set; }
    public bool isStanding { get; set; }

    private void OnEnable()
    {
        //controlled by npc pool
        animator.runtimeAnimatorController = animatorOverride;
        spriteRenderer.material = material;       
    }
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
