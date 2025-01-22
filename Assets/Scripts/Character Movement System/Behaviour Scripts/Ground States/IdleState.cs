using System.Collections;
using UnityEngine;

public class IdleState : State
{
    [Tooltip("number of breathes until blinking")]
    public int breathLoops = 5;

    private int breathCount = 0;

    public override void Enter()
    {
    }
    public override void Do()
    {
        IdleAnimationController();
        if (!collisionChecker.grounded || movementInputs.walkInput != 0)
        {
            Exit();
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
        base.Exit();
    }

    private void IdleAnimationController()
    {
        if (core is not PlayerMovement) // idle animation is handeled in the behavioural states
        {
            StartCoroutine(IdleAnimSequences());
            return;
        }
        else
        {
            PlayAnimation(animStates.breathingAnimState);
        }
    }

    private IEnumerator IdleAnimSequences()
    {
        if (breathCount < breathLoops)
        {
            PlayAnimation(animStates.calmBreathingAnimState);
            yield return null;
            breathCount++;
        }
        else
        {
            playingAnimation = true;
            PlayAnimation(animStates.calmBlinkingAnimState);
            yield return null;
            breathCount = 0;
        }
    }

}
