using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NPCController : MonoBehaviour
{
    [Header("Parameters")]
    public float updateTargetTickRate = 1f;

    [Header("References")]
    public NPCCore npcCore;
    public Transform playerTransform;
    public BoxCollider2D playerCollider;
    public MovementInputs movementInputs;

    protected Vector2 currentPos;
    protected PathData.NamedPosition lastPos;
    protected float colliderCenter;

    protected IEnumerator TargetPosIsPlayer() // TODO: lastPOs.value cannot be updated
    {
        while (true)
        {
            lastPos.value = new Vector2(playerTransform.position.x, playerTransform.position.y) + new Vector2(0, (playerCollider.size.y / 2f));
            yield return new WaitForSeconds(updateTargetTickRate);
        }
    }

    protected IEnumerator TargetPosIsSeat()
    {
        while(true)
        {
            //TODO: targetPos = new Vector2() set position to the next closest seat
            yield return new WaitForSeconds(updateTargetTickRate);
        }
    }

    protected IEnumerator TargetPosIsStandArea()
    {
        while(true)
        {
            //TODO: targetPos = new Vector2() set position to the next larget area
            yield return new WaitForSeconds(updateTargetTickRate);
        }
    }
}
