using UnityEngine;

public class CalmState : State
{
    //parent state
    public BystanderMovement bystanderMovement;

    //child state
    public GroundState groundState;

    public float runSpeedMultiplier;
    public float walkSpeedMultiplier;

    [Tooltip("number of breathes until blinking")]
    public int breathLoops = 5;

    private string breathingAnimation = "calmBreathing";
    private string blinkingAnimation = "calmBlinking";

    private int breathCount = 0;
    private bool isBlinking = false;
    public override void Enter()
    {
    }
    public override void Do()
    {
        IdleBehaviours();
        SelectState();

        if (bystanderMovement.isPanic)
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
        Set(groundState, true);
    }


    private void IdleBehaviours()
    {
        if (!groundState.idleState) return;

        if (!isBlinking && bystanderMovement.currentAnimStateInfo.IsName(breathingAnimation))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                breathCount++;
                animator.Play(breathingAnimation, 0, 0);

                if (breathCount >= breathLoops)
                {
                    breathCount = 0;
                    PlayBlinking();
                }
            }
        }
        else if (isBlinking && bystanderMovement.currentAnimStateInfo.IsName(blinkingAnimation))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                PlayBreathing();
            }
        } 
    }
    private void PlayBreathing()
    {
        isBlinking = false;
        animator.Play(breathingAnimation, 0, 0);
    }
    private void PlayBlinking()
    {
        isBlinking = true;
        animator.Play(blinkingAnimation, 0, 0);
    }
}
