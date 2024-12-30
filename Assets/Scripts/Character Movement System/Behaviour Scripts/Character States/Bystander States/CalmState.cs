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
        if (!core.stateList.idleState) return;

        if (!isBlinking && bystanderMovement.currentAnimStateInfo.IsName(animStates.calmBreathingAnimState))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                breathCount++;
                animator.Play(animStates.calmBreathingAnimState, 0, 0);

                if (breathCount >= breathLoops)
                {
                    breathCount = 0;
                    isBlinking = true;
                    PlayAnimation(animStates.calmBlinkingAnimState);
                }
            }
        }
        else if (isBlinking && bystanderMovement.currentAnimStateInfo.IsName(animStates.calmBlinkingAnimState))
        {
            if (bystanderMovement.currentAnimStateInfo.normalizedTime >= 1f)
            {
                isBlinking = false;
                PlayAnimation(animStates.calmBreathingAnimState);
            }
        } 
    }
}
