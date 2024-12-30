using UnityEngine;

public class AttackState : State
{
    //parent state
    public AgentMovement agentMovement;
    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();
        movementInputs.JumpController();

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
        if (CarriageClimbingBounds.Instance != null && !core.stateList.wallState.isDropping)
        {
            Set(core.stateList.wallState, true);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {
                //reset wall bools
                core.stateList.wallState.isDropping = false;
                core.stateList.wallState.isHanging = true;

                Set(core.stateList.groundState, true);
            }
            else
            {
                Set(core.stateList.airborneState, true);
            }
        }
    }
}
