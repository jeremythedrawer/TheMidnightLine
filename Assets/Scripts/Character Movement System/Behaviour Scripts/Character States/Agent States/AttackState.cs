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
        if (core.currentClimbBounds != null && !movementInputs.crouchInput)
        {
            Set(stateList.wallState, false);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {

                Set(stateList.groundState, false);
            }
            else
            {
                Set(stateList.airborneState, false);
            }
        }   
    }
}
