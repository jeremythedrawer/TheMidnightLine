using UnityEngine;
using System.Collections;

public class WallState : State
{
    //parent states
    public AirborneState airborneState;

    //child states
    public HangState hangState;
    public ClimbState climbState;

    public bool pendingState { get; set; }
    public bool isClimbing { get; set; } = false;
    public bool droppingDown { get; set; } = false;
    public override void Enter()
    {

    }
    public override void Do()
    {
        SelectState();
        if (movementInputs.jumpInput)
        {
            isClimbing = true;
        }
        if (movementInputs.crouchInput)
        {
            isClimbing = false;
            droppingDown = true;
            airborneState.hanging = false;
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
        if (!pendingState)
        {
            StartCoroutine(DelayState());
        }
    }

    private IEnumerator DelayState()
    {

        yield return null; // one frame delay


        if (isClimbing)
        {
            Set(climbState, true);
        }
        else
        {
            Set(hangState, true);
        }

        pendingState = false; // reset bool
    }
}
