using System.Collections;
using System.Threading;
using System.Threading.Tasks;
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

    public override void Enter()
    {
    }
    public override void Do()
    {
        StartCoroutine(IdleAnimSequences());
        SelectState();
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
        Set(groundState);
    }

    private IEnumerator IdleAnimSequences()
    {
        if (!playingAnimation)
        {
            if (breathCount < breathLoops)
            {
                playingAnimation = true;
                yield return StartCoroutine(PlayAnimationCoroutine(animStates.calmBreathingAnimState));
                breathCount++;
            }
            else
            {
                playingAnimation = true;
                yield return StartCoroutine(PlayAnimationCoroutine(animStates.calmBlinkingAnimState));
                breathCount = 0;
            }
        }
    }

    private IEnumerator PlayAnimationCoroutine(string animState)
    {

        while (core.currentAnimStateInfo.normalizedTime < 1)
        {
            yield return null;
        }
        animator.Play(animState, 0, 0);
        playingAnimation = false;
    }
}
