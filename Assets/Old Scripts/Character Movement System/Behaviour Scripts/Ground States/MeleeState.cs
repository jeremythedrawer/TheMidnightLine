using UnityEngine;

public class MeleeState : State
{
    public MeleeColliderData meleeColliderData;

    public bool landedHit {  get; private set; }

    private int hitCombos = 0;

    public override void Enter()
    {

    }
    public override void Do()
    {
        MeleeAnimationController();
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {
        base.Exit();
        core.movementInputs.meleeInput = false;
        movementInputs.canMove = true;
        hitCombos = 0;
    }


    public void DealDamage(int attackIndex)
    {
        if (core is SpyBrain)
        {
            foreach (Collider2D agentCollider in meleeColliderData.agentColliders)
            {
                agentCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }  
        }
        if (core is AgentBrain)
        {
            if (meleeColliderData.hitPlayerCollider)
            {
                hitCombos++;
                meleeColliderData.playerCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }
        }
    }

    private void MeleeAnimationController()
    {
        if (core is SpyBrain)
        {
            if (!playingAnimation)
            {
                PlayAnimation(animStates.meleeAnimState);
                movementInputs.canMove = false;
            }
            if (core.currentAnimStateInfo.normalizedTime >= 1f)
            {
                Exit();
            }
        }
        else if (core is AgentBrain)
        {
            PlayComboAnimation();
        }
    }

    private void PlayComboAnimation()
    {
        if (!playingAnimation)
        {
            switch (hitCombos)
            {
                case 0:

                    PlayAnimation(animStates.stanceToPrimaryAnimState);

                    if (core.currentAnimStateInfo.IsName(animStates.stanceToPrimaryAnimState) &&
                        core.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 0) // missed hit
                    {
                        PlayAnimation(animStates.primaryHitToStanceAnimState);
                        hitCombos = -1;
                    }

                    break;

                case 1:
                    PlayAnimation(animStates.primaryHitToSecondaryAnimState);

                    if (core.currentAnimStateInfo.IsName(animStates.primaryHitToSecondaryAnimState) && 
                        core.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 1) // missed hit
                    {
                        PlayAnimation(animStates.secondaryHitToStanceAnimState);
                        hitCombos= -1;
                    }
                    break;

                case 2:
                    PlayAnimation(animStates.secondaryHitToFinalAnimState);
                    if (core.currentAnimStateInfo.normalizedTime >= 1f)
                    {
                        hitCombos++;
                    }
                    break;

                case 3:
                    PlayAnimation(animStates.finalHitToStanceAnimState);
                        hitCombos = -1;
                    break;

                case -1:
                    if (core.currentAnimStateInfo.normalizedTime >= 1f) // wait to get back to stance before leaving
                    {
                        Exit();
                    }

                    break;
            }

        }

        if (core.currentAnimStateInfo.normalizedTime >= 1f)
        {
            playingAnimation = false;
        }
    }
}
