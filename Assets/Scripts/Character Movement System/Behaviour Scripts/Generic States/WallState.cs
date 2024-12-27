using System.Collections;
using Unity.VisualScripting;

public class WallState : State
{
    public HangState hangState;
    public ClimbState climbState;

    public bool pendingState { get; set; }
    public bool isClimbing { get; set; } = false;
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
        if (movementInputs.jumpInput)
        {
            isClimbing = true;
        }
        if (movementInputs.crouchInput)
        {
            isClimbing = false;
        }

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
