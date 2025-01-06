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

        npcCore.npcController.FollowAttackPath();

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
        if (core.currentClimbBounds != null && !stateList.wallState.isDropping)
        {
            Set(stateList.wallState, true);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {
                //reset wall bools
                stateList.wallState.isDropping = false;
                stateList.wallState.isHanging = true;

                Set(stateList.groundState, true);
            }
            else
            {
                Set(stateList.airborneState, true);
            }
        }
    }
}
