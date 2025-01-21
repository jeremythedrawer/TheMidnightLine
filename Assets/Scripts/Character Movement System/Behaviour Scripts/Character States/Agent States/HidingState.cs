using UnityEngine;

public class HidingState : State
{
    //parent state
    public AgentMovement agentMovement;
    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
        base.Exit();
    }

    private void SelectState()
    {
        Set(stateList.groundState);
    }
}

