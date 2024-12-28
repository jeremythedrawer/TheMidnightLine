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

    private string breathingAnimState = "calmBreathing";
    private string blinkingAnimState = "calmBlinking";

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

        if (!isBlinking && bystanderMovement.currentAnimStateInfo.IsName(breathingAnimState))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                breathCount++;
                animator.Play(breathingAnimState, 0, 0);

                if (breathCount >= breathLoops)
                {
                    breathCount = 0;
                    isBlinking = true;
                    PlayAnimation(blinkingAnimState);
                }
            }
        }
        else if (isBlinking && bystanderMovement.currentAnimStateInfo.IsName(blinkingAnimState))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                isBlinking = false;
                PlayAnimation(breathingAnimState);
            }
        } 
    }
}
