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


    public List<Vector2> pathToTarget { get; set; } = new List<Vector2>();
    private bool pathIsSet;
    public Vector2 nextPos { get; private set; }

    private Vector2 playerPos;
    private Vector2 targetPos;
    private GangwayBounds chosenGangway;
    private CarriageClimbingBounds chosenClimbingBounds;
    private ActivateCarriageBounds chosenInsideBounds;
    private Vector2 currentPos;

    private float closeEnoughToNextPos = 0.5f;
    private float distanceToNextPos;
    private bool hasJumped;
    private bool nextPosIsClimbBounds;

    void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }

    public void MoveToNextPos()
    {
        SetPath();

        if (currentPos.x < pathToTarget[0].x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }

        if (chosenClimbingBounds != null)
        {
            if (pathToTarget[0] == (Vector2)chosenClimbingBounds.transform.position && !hasJumped)
            {
                nextPosIsClimbBounds = true;
            }
        }

        if (nextPosIsClimbBounds)
        {
            if (!hasJumped)
            {
                movementInputs.runInput = true;
                movementInputs.jumpInput = true;
                hasJumped = true;
            }
            nextPosIsClimbBounds = false;
        }
        else
        {
            movementInputs.runInput = false;
            movementInputs.jumpInput = false;
            hasJumped = false;
        }
    }
    private void SetPath()
    {
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, (boxCollider.size.y / 2f));
        targetPos = new Vector2(playerPos.x, playerPos.y) + new Vector2(0, (playerCollider.size.y / 2f));

        if (!pathIsSet)
        {
            Debug.Log("finding new path");
            FindPathToTarget(currentPos, targetPos);
        }

        distanceToNextPos = Vector2.Distance(currentPos, pathToTarget[0]);

        if (distanceToNextPos < closeEnoughToNextPos || targetPos != pathToTarget[pathToTarget.Count - 1])
        {
            pathIsSet = false;
        }
        DrawDebugPath();
    }
    public void FindPathToTarget(Vector2 currentPos, Vector2 targetPos)
    {
        pathToTarget.Clear();

        float currentYPos = currentPos.y;

        if (currentInsideBounds != null) // Agent in Inside Bounds
        {

            if (currentInsideBounds.playerInActiveArea)
            {
                pathToTarget.Add(targetPos);
            }
            else
            {
                FindChosenGangway(currentInsideBounds);
                FindChosenClimbBounds(chosenGangway);

                pathToTarget.Add(new Vector2 (chosenGangway.transform.position.x, currentYPos));

                if (chosenClimbingBounds != null)
                {
                    pathToTarget.Add(chosenClimbingBounds.transform.position);
                }

                pathToTarget.Add(targetPos);
            }
        }
        else if (currentGangwayBounds != null) // Agent in Gangway Bounds
        {
            FindChosenClimbBounds(chosenGangway);
            if (chosenClimbingBounds != null)
            {
                pathToTarget.Add(chosenClimbingBounds.transform.position);
            }
            else
            {
                FindChosenInsideBounds(chosenGangway);
                pathToTarget.Add(new Vector2 (chosenInsideBounds.transform.position.x, currentYPos));
            }
            pathToTarget.Add (targetPos);
        }
        else if (currentOutsideBounds != null) // Agent in Outside Bounds
        {

            if (targetPos.y >= trainBounds.roofLevel)
            {
                pathToTarget.Add(targetPos);
            }
            else
            {
                FindChosenGangway(currentOutsideBounds);
                FindChosenClimbBounds(chosenGangway);
                pathToTarget.Add(new Vector2 (chosenGangway.transform.position.x, currentYPos));

                if(chosenClimbingBounds != null)
                {
                    pathToTarget.Add(chosenClimbingBounds.transform.position);
                }
                pathToTarget.Add(targetPos);
            }
        }
        pathIsSet = true;
    }

    private void DrawDebugPath()
    {
        Vector2 pos = currentPos;

        for (int i = 0; i < pathToTarget.ToArray().Length; i++)
        {
            Vector2 nextPos = pathToTarget[i];
            Debug.DrawLine(pos, nextPos, Color.magenta);
            pos = nextPos;
        }
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

    private void FindChosenInsideBounds(GangwayBounds chosenGangway)
    {
        if (currentPos.x < targetPos.x)
        {
            chosenInsideBounds = chosenGangway.foundRightInsideBounds;
        }
        else
        {
            chosenInsideBounds = chosenGangway.foundLeftInsideBounds;
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
