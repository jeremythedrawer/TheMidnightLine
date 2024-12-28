using UnityEngine;

public class AttackState : State
{
    //parent state
    public AgentMovement agentMovement;

    //child states
    public GroundState groundState;
    public AirborneState airborneState;
    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();

        if (agentMovement.isHiding)
        {
            isComplete = true;
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }

    private void SelectState()
    {

        if (collisionChecker.grounded)
        {
            Set(groundState, true);
        }
        else
        {
            Set(airborneState, true);
        }
    }
}
