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
        if (core is NPCCore)
        {
            if (!playingAnimation)
            {
                if (!npcCore.isSitting)
                {
                    StopAllCoroutines();
                    StartCoroutine(IdleAnimSequences(animStates.calmBreathingAnimState, animStates.calmBlinkingAnimState));
                }
                else
                {
                    StopAllCoroutines();
                    StartCoroutine(IdleAnimSequences(animStates.sitBreathingAnimState, animStates.sitBlinkingAnimState));
                }

            }
        }
        else
        {
            PlayAnimation(animStates.breathingAnimState);
        }
    }

    private IEnumerator IdleAnimSequences(string breathingState, string blinkingState)
    {
        if (breathCount < breathLoops)
        {
            PlayAnimation(breathingState);
            yield return null;
            breathCount++;
        }
        else
        {
            PlayAnimation(blinkingState);
            yield return null;
            breathCount = 0;
        }
    }

}
