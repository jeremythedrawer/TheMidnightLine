using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TextCore.Text;
using static SeatBounds;

public abstract class NPCController : MonoBehaviour
{
    [Header("Paths")]
    public CalmPath calmPath;

    [Header("References")]
    public NPCCore npcCore;
    public Transform playerTransform;
    public BoxCollider2D playerCollider;
    public MovementInputs movementInputs;
    protected PathData pathData;

    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    protected Vector2 currentPos;
    protected PathData.NamedPosition lastPos;
    protected float colliderCenter;

    private float distanceToTarget;
    private bool closeEnough;
    private bool finishedPath;
    private List<PathData.NamedPosition> calmPathToTarget => pathData.pathToTarget;
    private float targetPosX = float.MaxValue;

    private bool adjustPath = true;
    private bool isBystander;
    private BystanderBrain bystanderBrain;

    private int startingStationIndex;
    private int currentStationIndex => GlobalReferenceManager.Instance.stations.IndexOf(trainData.nextStation);

    private bool startInputs;
    private void Awake()
    {
        pathData = GetComponent<PathData>();
        isBystander = npcCore is BystanderBrain;
        if (isBystander) { bystanderBrain = npcCore as BystanderBrain; }
    }
    private void Start()
    {
        
        startingStationIndex = GlobalReferenceManager.Instance.stations.IndexOf(npcCore.startingStation);
    }
    public void CalmInputs()
    {
        if (currentStationIndex >= startingStationIndex && trainData.kmPerHour == 0) startInputs = true;

        if (!startInputs) return;

        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);
        if (calmPathToTarget.Count == 0)
        {
            FollowCalmPath(currentPos, colliderCenter);
            return;
        }
        targetPosX = calmPathToTarget[0].value.x;
        distanceToTarget = Mathf.Abs(currentPos.x - targetPosX);
        closeEnough = distanceToTarget < 0.1;

        if (!closeEnough)
        {
            FollowCalmPath(currentPos, colliderCenter);
            return;
        }

        PathData.PosType nextPosType = calmPathToTarget[0].type;
        switch (nextPosType)
        {
            case PathData.PosType.SlidingDoors: //TODO: seperate from entering and exiting
            {
                if (bystanderBrain?.departureStation != trainData.nextStation)
                {
                    if (trainData.kmPerHour == 0)
                    {
                        pathData.chosenSlideDoorBounds.OpenDoors();
                        movementInputs.walkInput = 0;
                    }
                    if (pathData.chosenSlideDoorBounds.boxCollider.bounds.Intersects(npcCore.boxCollider2D.bounds))
                    {
                        if (!pathData.chosenSlideDoorBounds.enteringcharacterQueue.Contains(npcCore))
                        {
                            pathData.chosenSlideDoorBounds.enteringcharacterQueue.Enqueue(npcCore);
                        }
                    }

                    if (pathData.chosenSlideDoorBounds.normMoveDoorTime >= 1)
                    {
                        if (npcCore.onTrain)
                        {
                            FollowCalmPath(currentPos, colliderCenter);
                        }
                    }
                }
                else
                {
                    movementInputs.walkInput = 0;
                    if (!trainData.bystandersDeparting.Contains(npcCore))
                    {
                        trainData.bystandersDeparting.Add(npcCore);
                    }
                    if (trainData.kmPerHour == 0)
                    {
                        pathData.chosenSlideDoorBounds.OpenDoors();
                        if (pathData.chosenSlideDoorBounds.normMoveDoorTime >= 1 && pathData.chosenExitBounds == null)
                        {
                            FollowCalmPath(currentPos, colliderCenter);
                        }
                        else if (pathData.chosenSlideDoorBounds.boxCollider.bounds.Intersects(npcCore.boxCollider2D.bounds))
                        {
                            if (!pathData.chosenSlideDoorBounds.exitingcharacterQueue.Contains(npcCore))
                            {
                                pathData.chosenSlideDoorBounds.exitingcharacterQueue.Enqueue(npcCore);
                            }
                        }
                    }
                }
            }
                break;

            case PathData.PosType.Seat:
            {
                //set sitting parameters
                movementInputs.walkInput = 0;
                npcCore.isSitting = true;
                npcCore.characterMaterial.SendCharToSeatLayer();
                //set chosen seat to filled
                if(pathData.chosenSeatBounds == null) return;
                SeatData seatData = pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex];
                seatData.filled = true;
                pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex] = seatData;

                if (isBystander)
                {
                    if (bystanderBrain.departureStation == trainData.nextStation)
                    {
                        seatData.filled = false;
                        pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex] = seatData;
                        npcCore.isSitting = false;
                        FollowCalmPath(currentPos, colliderCenter);
                    }
                }
            }
            break;

            case PathData.PosType.Stand:
            {
                movementInputs.walkInput = 0;
                npcCore.isStanding = true;
                npcCore.characterMaterial.SendCharToStandLayer();
                if (isBystander)
                {
                    if (bystanderBrain.departureStation == trainData.nextStation)
                    {
                        FollowCalmPath(currentPos, colliderCenter);
                    }
                }
            }
            break;

            case PathData.PosType.ExitBound:
            {
                npcCore.collisionChecker.activeGroundLayer = 1 << GlobalReferenceManager.Instance.exitGroundLayer;
                npcCore.spriteRenderer.sortingOrder = -1;
                npcCore.boxCollider2D.excludeLayers |= 1 << GlobalReferenceManager.Instance.stationGroundLayer;
                FollowCalmPath(currentPos, colliderCenter );
            }
            break;

            case PathData.PosType.DisableBound:
            {
                //insert bystander to object pool
            }
            break;

        }
    }

    private void FollowCalmPath(Vector2 currentPos, float colliderCenter)
    {
        CheckToAdjustPath();

        if (adjustPath)
        {
            calmPath.SetPath(currentPos, colliderCenter);
            adjustPath = false;
        }

        //handle direction
        if (calmPathToTarget.Count > 0 && !closeEnough)
        {
            if (currentPos.x < calmPathToTarget[0].value.x)
            {
                movementInputs.walkInput = 1;
            }
            else
            {
                movementInputs.walkInput = -1;
            }
        }
        else
        {
            movementInputs.walkInput = 0;
        }
        DrawDebugPath(currentPos);
    }

    private void CheckToAdjustPath()
    {
        if (closeEnough)
        {
            if (calmPathToTarget.Count > 0)
            {
                calmPathToTarget.RemoveAt(0);
            }
            adjustPath = true;
        }
    }

    private void DrawDebugPath(Vector2 currentPos)
    {
        Vector2 pos = currentPos;

        for (int i = 0; i < pathData.pathToTarget.Count; i++)
        {
            Vector2 nextPos = pathData.pathToTarget[i].value;
            Debug.DrawLine(pos, nextPos, Color.magenta);
            Debug.DrawLine(nextPos, nextPos + (Vector2.up * 0.5f), Color.cyan);
            pos = nextPos;
        }
    }
}
