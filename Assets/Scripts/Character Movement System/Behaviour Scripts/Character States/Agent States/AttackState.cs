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

        npcCore.agentController.AttackInputs();

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
        if ((core.currentClimbBounds?.hangActivated ?? false || stateList.wallState.isClimbing == true) && !movementInputs.crouchInput) // checks if climb bounds is not null, if it is hangActivated returns false using "??"
        {
            Set(stateList.wallState);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {

                Set(stateList.groundState);
            }
            else
            {
                Set(stateList.airborneState);
            }
        }   
    }
}
