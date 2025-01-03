using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationSystem : MonoBehaviour
{
    [Header("Parameters")]
    public float updatePlayerPosTicRate = 1f;
    public float stopMovementBuffer = 1f;

    [Header("References")]
    public MovementInputs movementInputs;
    public Transform playerTransform;
    public LayerMask climbingLayer;
    public BoxCollider2D boxCollider;
    public BoxCollider2D playerCollider;

    public ActivateCarriageBounds currentInsideBounds { private get; set; }
    public GangwayBounds currentGangwayBounds { private get; set; }

    private Vector2 playerPos;
    private Vector2 targetPos;
    private Vector2 chosenGangwayPos;
    private Vector2 afterGangwayPos;
    private Vector2 startPos;

    void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }

    void Update()
    {
    }

    public void MoveToTarget()
    {
        startPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0,(boxCollider.size.y/2f));
        targetPos = new Vector2(playerPos.x, playerPos.y) + new Vector2(0, (playerCollider.size.y/2f));

        if (currentInsideBounds != null)
        {
            InsideCarriageToGangway();
        }
        /*
        currentPos = transform.position;
        if (playerPos.x - stopMovementBuffer > currentPos.x + stopMovementBuffer)
        {
            movementInputs.walkInput = 1;
        }
        else if (playerPos.x + stopMovementBuffer < currentPos.x - stopMovementBuffer)
        {
            movementInputs.walkInput = -1;
        }
        else
        {
            movementInputs.walkInput = 0;
        }

        if (foundClimbingBounds)
        {
            Debug.Log(movementInputs.jumpInput);
            //TODO: find outside carriage bounds
            movementInputs.jumpInput = true;
        }
        else
        {
            movementInputs.jumpInput = false;
        }
        */
    }
    private void GangwayToTarget()
    {
       // Debug.DrawLine(chosenGangwayPos, )
    }


    private void InsideCarriageToGangway()
    {

        Vector2 rightGangwayPos = currentInsideBounds.rightGangwayPos;
        Vector2 leftGangwayPos = currentInsideBounds.leftGangwayPos;
            
        if (currentInsideBounds.isBackCarriage) 
        { 
            chosenGangwayPos = rightGangwayPos;
        }
        else if (currentInsideBounds.isFrontCarriage) 
        { 
            chosenGangwayPos = leftGangwayPos;
        }
        else 
        {
            float leftDistance = startPos.x - leftGangwayPos.x;
            float rightDistance = rightGangwayPos.x - startPos.x;

            float targetLeftDistance = targetPos.x - leftGangwayPos.x;
            float targetRightDistance = rightGangwayPos.x - targetPos.x;

            bool bothClosestToLeft = leftDistance < rightDistance && targetLeftDistance < targetRightDistance;
            bool bothClosestToRight = rightDistance < leftDistance && targetRightDistance < targetLeftDistance;

            if (bothClosestToLeft || leftDistance < targetRightDistance)
            {
                chosenGangwayPos = leftGangwayPos;
            }
            else
            {
                chosenGangwayPos = rightGangwayPos;
            }
        }
        Debug.DrawLine(startPos, chosenGangwayPos, Color.magenta);
    }

    private IEnumerator UpdatePlayerPosition()
    {
        while (true)
        {
            playerPos = playerTransform.position;
            yield return new WaitForSeconds(updatePlayerPosTicRate);
        }
    }
}
