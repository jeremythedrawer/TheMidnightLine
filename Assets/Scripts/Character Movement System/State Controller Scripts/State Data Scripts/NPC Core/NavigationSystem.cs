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
    public TrainBounds trainBounds;

    //where NPC is
    public ActivateCarriageBounds currentInsideBounds { private get; set; }
    public GangwayBounds currentGangwayBounds { private get; set; }
    public ActivateCarriageBounds currentOutsideBounds { private get; set; }


    private Queue<Vector2> pathToTarget = new Queue<Vector2>();
    public Vector2 nextPos { get; private set; }

    private Vector2 playerPos;
    private Vector2 targetPos;
    private GangwayBounds chosenGangway;
    private CarriageClimbingBounds chosenClimbingBounds;
    private Vector2 currentPos;

    private float closeEnoughToNextPos = 0.5f;
    private float distanceToNextPos;
    private bool hasJumped;

    void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }
    public void MoveToNextPos()
    {

        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, (boxCollider.size.y / 2f));
        targetPos = new Vector2(playerPos.x, playerPos.y) + new Vector2(0, (playerCollider.size.y / 2f));

        FindPathToTarget(currentPos, targetPos);

        //if (currentPos.x < nextPos.x)
        //{
        //    movementInputs.walkInput = 1;
        //}
        //else
        //{
        //    movementInputs.walkInput = -1;
        //}

        //if (currentPos.y < nextPos.y)
        //{
        //    if (!hasJumped)
        //    {
        //        movementInputs.jumpInput = true;
        //        hasJumped = true;
        //    }
        //}
        //else
        //{
        //    movementInputs.jumpInput = false;
        //    hasJumped = false;
        //}
    }
    public void FindPathToTarget(Vector2 currentPos, Vector2 targetPos)
    {

        if (currentInsideBounds != null)
        {
            if (currentInsideBounds.playerInActiveArea)
            {
                pathToTarget.Enqueue(FindTarget());
            }
            else
            {
                FindChosenGangway(currentInsideBounds);
                FindChosenClimbBounds(chosenGangway);
                FindTarget();

                pathToTarget.Enqueue(chosenGangway.transform.position);
                if (chosenClimbingBounds != null)
                {
                    pathToTarget.Enqueue(chosenClimbingBounds.transform.position);
                }
                //pathToTarget.Enqueue(targetPos);
            }
        }

        //if (currentGangwayBounds != null)
        //{
        //    pathToTarget.Enqueue(GangwayToAfterGangway());
        //}

        //if (currentOutsideBounds != null)
        //{
        //    if (targetPos.y >= trainBounds.roofLevel)
        //    {
        //        pathToTarget.Enqueue(FindTarget());
        //    }
        //    else
        //    {
        //        pathToTarget.Enqueue(FindChosenGangway(currentOutsideBounds));
        //    }
        //}

        DrawDebugPath();
    }

    private void DrawDebugPath()
    {
        Vector2 pos = currentPos;

        var pathList = pathToTarget.ToArray();

        for (int i = 0; i < pathList.Length; i++)
        {
            Vector2 nextPos = pathList[i];
            Debug.DrawLine(pos, nextPos, Color.magenta);
            pos = nextPos;
        }
    }
    private Vector2 FindTarget()
    {
        return targetPos;
    }

    private void FindChosenClimbBounds(GangwayBounds chosenGangway)
    {
        Vector2 boundsMin = chosenGangway.Bounds.min;
        Vector2 boundsMax = chosenGangway.Bounds.max;
        Vector2 boundsCenter = chosenGangway.Bounds.center;

        if (chosenGangway.playerOnLeftRoof)
        {
            chosenClimbingBounds = chosenGangway.foundsLeftClimbBounds;
        }
        else if (chosenGangway.playerOnRightRoof)
        {
            chosenClimbingBounds = chosenGangway.foundsRightClimbBounds;
        }
        else
        {
            chosenClimbingBounds = null;
        }

    }

    private void FindChosenGangway(ActivateCarriageBounds activateCarriageBounds)
    {
        GangwayBounds rightGangway = activateCarriageBounds.rightGangwayBounds;
        GangwayBounds leftGangway = activateCarriageBounds.leftGangwayBounds;
            
        if (activateCarriageBounds.isBackCarriage) 
        { 
            chosenGangway = rightGangway;
        }
        else if (activateCarriageBounds.isFrontCarriage) 
        { 
            chosenGangway = leftGangway;
        }
        else 
        {
            float leftDistance = currentPos.x - leftGangway.transform.position.x;
            float rightDistance = rightGangway.transform.position.x - currentPos.x;

            float targetLeftDistance = targetPos.x - leftGangway.transform.position.x;
            float targetRightDistance = rightGangway.transform.position.x - targetPos.x;

            bool bothClosestToLeft = leftDistance < rightDistance && targetLeftDistance < targetRightDistance;
            bool bothClosestToRight = rightDistance < leftDistance && targetRightDistance < targetLeftDistance;

            if (bothClosestToLeft || leftDistance < targetRightDistance)
            {
                chosenGangway = leftGangway;
            }
            else
            {
                chosenGangway = rightGangway;
            }
        }
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
