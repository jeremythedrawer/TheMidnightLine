using UnityEngine;

public class AttackState : State
{
    //parent state
    public AgentMovement agentMovement;

    //child states
    public GroundState groundState;
    public AirborneState airborneState;
    public WallState wallState;
    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();
        movementInputs.JumpController(airborneState);

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
        if (CarriageClimbingBounds.Instance != null && !wallState.isDropping)
        {
            Set(wallState, true);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {
                //reset wall bools
                wallState.isDropping = false;
                wallState.isHanging = true;

                Set(groundState, true);
            }
            else
            {
                Set(airborneState, true);
            }
        }
    }
}
