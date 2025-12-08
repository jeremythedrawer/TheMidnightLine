using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AttackState : State
{
    private AgentBrain agentBrain => transform.parent.transform.parent.GetComponent<AgentBrain>();
    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();

        agentBrain.agentController.AttackInputs();
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
        base.Exit();
        StopAllCoroutines();
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
