using System.Collections;
using UnityEngine;

public class GroundState : State
{
    public bool canMelee;
    public bool canShoot;

    [Tooltip("How sticky the floor is")]
    [Range(0f, 1f)]
    public float groundDrag;


    public override void Enter()
    {

    }
    public override void Do()
    {
        BHCCorrection();
        SelectState();
        EnterTrain();
    }

    public override void FixedDo()
    {
        if (state == core.stateList.idleState || state == core.stateList.meleeState || state == core.stateList.shootState)
        {
            body.linearVelocityX *= groundDrag;
        }
    }
    public override void Exit()
    {
        base.Exit();
    }

    void SelectState()
    {
        if (movementInputs.meleeInput)
        {
            if (canMelee && movementInputs.meleeInput)
            {
                Set(core.stateList.meleeState);
            }

            if (movementInputs.shootInput && canShoot)
            {
                Set(core.stateList.shootState);
            }
        }
        else if (movementInputs.walkInput != 0 && movementInputs.canMove)
        {
            Set(core.stateList.runState);
        }
        else
        {
            Set(core.stateList.idleState);
        }
    }

    private void BHCCorrection()
    {
        //bumped head correction (corrects box collider when grounded and jumped state failed to move the collider in time
        if (Mathf.Abs(collisionChecker.characterBoxCollider.offset.x) > 0)
        {
            Vector2 currentOffset = collisionChecker.characterBoxCollider.offset;
            currentOffset.x = 0;
            collisionChecker.characterBoxCollider.offset = currentOffset;
        }
    }

    private void EnterTrain()
    {
        if (SlideDoorBounds.Instance != null)
        {
            //TODO: Enable UI to encourage player to press "E"
            if (movementInputs.enterTrainInput)
            {
                StartCoroutine(EnteringTrain());
            }
        }    
    }

    private IEnumerator EnteringTrain()
    {
        movementInputs.canMove = false;;
        SlideDoorBounds.Instance.OpenDoors();
        yield return new WaitUntil(() => SlideDoorBounds.Instance.normMoveDoorTime >= 1);

        SlideDoorBounds.Instance.enteringcharacterQueue.Enqueue(core);
        yield return new WaitUntil(() => core.onTrain);
        movementInputs.canMove = true;
    }
}
