using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PathFinder : MonoBehaviour
{
    public PathData pathData;
    public float distanceToNextPos { get; protected set; }
    public float closeEnoughToNextPos { get; set; } = 0.5f;
    public List<PathData.NamedPosition> pathToTarget => pathData.pathToTarget;

    protected TrainBounds trainBounds => pathData.trainBounds;

    protected InsideBounds currentInsideBounds => pathData.currentInsideBounds;
    protected OutsideBounds currentOutsideBounds => pathData.currentOutsideBounds;
    protected GangwayBounds currentGangwayBounds => pathData.currentGangwayBounds;

    protected InsideBounds chosenInsideBounds => pathData.chosenInsideBounds;
    protected OutsideBounds chosenOutsideBounds => pathData.chosenOutsideBounds;
    protected GangwayBounds chosenGangway => pathData.chosenGangwayBounds;
    public ClimbingBounds chosenClimbingBounds => pathData.chosenClimbingBounds;
    protected SeatBounds chosenChairBounds => pathData.chosenSeatBounds;

    protected void FindChosenClimbBounds(GangwayBounds chosenGangway, PathFinder path, Vector2 targetPos, Vector2 currentPos)
    {
        if (currentPos.y > trainBounds.roofLevel)
        {
            pathData.chosenClimbingBounds = null;
            return;
        }

        if (path is AttackPath)
        {
            if (targetPos.y > trainBounds.roofLevel)
            {
                pathData.chosenClimbingBounds = chosenGangway.transform.position.x < targetPos.x ? chosenGangway.foundsRightClimbBounds : chosenGangway.foundsLeftClimbBounds;
            }
            else
            {
                pathData.chosenClimbingBounds = null;
            }
        }

        if (path is StalkPath)
        {
            if (pathData.chosenGangwayBounds.playerOnLeftRoof)
            {
                pathData.chosenClimbingBounds = chosenGangway.foundsLeftClimbBounds;
            }
            else if (chosenGangway.playerOnRightRoof)
            {
                pathData.chosenClimbingBounds = chosenGangway.foundsRightClimbBounds;
            }
            else
            {
                pathData.chosenClimbingBounds = null;
            }
        }

    }

    protected void FindChosenOutsideBounds(GangwayBounds chosenGangway, Vector2 targetPos, float currentXPos)
    {
        if (targetPos.y < trainBounds.roofLevel)
        {
            pathData.chosenOutsideBounds = null;
            return;
        }

        if (currentXPos < targetPos.x)
        {
            if (!chosenGangway.foundRightOutsideBounds.playerInActiveArea)
            {
                pathData.chosenOutsideBounds = chosenGangway.foundRightOutsideBounds;
            }
            else
            {
                pathData.chosenOutsideBounds = null;
            }
        }
        else
        {
            if (!chosenGangway.foundLeftOutsideBounds.playerInActiveArea)
            {
                pathData.chosenOutsideBounds = chosenGangway.foundRightOutsideBounds;

            }
            else
            {
                pathData.chosenOutsideBounds = null;
            }
        }
    }

    protected void FindChosenInsideBounds(GangwayBounds chosenGangway, Vector2 currentPos, float targetXPos)
    {
        if (currentPos.y >= trainBounds.roofLevel)
        {
            pathData.chosenInsideBounds = null;
            return;
        }

        if (currentPos.x < targetXPos)
        {
            if (!chosenGangway.foundRightInsideBounds.playerInActiveArea)
            {
                pathData.chosenInsideBounds = chosenGangway.foundRightInsideBounds;
            }
            else
            {
                pathData.chosenInsideBounds = null;
            }
        }
        else
        {
            if (!chosenGangway.foundLeftInsideBounds.playerInActiveArea)
            {
                pathData.chosenInsideBounds = chosenGangway.foundLeftInsideBounds;
            }
            else
            {
                pathData.chosenInsideBounds = null;
            }
        }

    }

    protected void FindChosenGangway(InsideOutsideBounds currentCarriageBounds, float currentXPos, float targetXPos)
    {
        GangwayBounds rightGangway = currentCarriageBounds.rightGangwayBounds;
        GangwayBounds leftGangway = currentCarriageBounds.leftGangwayBounds;

        if (currentCarriageBounds.isBackCarriage) 
        {
            pathData.chosenGangwayBounds = rightGangway;
        }
        else if (currentCarriageBounds.isFrontCarriage) 
        {
            pathData.chosenGangwayBounds = leftGangway;
        }
        else 
        {
            float leftDistance = currentXPos - leftGangway.transform.position.x;
            float rightDistance = rightGangway.transform.position.x - currentXPos;

            float targetLeftDistance = targetXPos - leftGangway.transform.position.x;
            float targetRightDistance = rightGangway.transform.position.x - targetXPos;

            bool bothClosestToLeft = leftDistance < rightDistance && targetLeftDistance < targetRightDistance;
            bool bothClosestToRight = rightDistance < leftDistance && targetRightDistance < targetLeftDistance;

            if (bothClosestToLeft || leftDistance < targetRightDistance)
            {
                pathData.chosenGangwayBounds = leftGangway;
            }
            else
            {
                pathData.chosenGangwayBounds = rightGangway;
            }
        }
    }
    protected void AddToPath(Vector2 position, PathData.PosType type)
    {
        pathData.pathToTarget.Add(new PathData.NamedPosition(position, type));
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

    public virtual void SetPath(Vector2 currentPos, PathData.NamedPosition lastPos, float colliderCentre)
    {
        DrawDebugPath(currentPos);

        if (!pathData.pathIsSet)
        {
            FindPath(currentPos, lastPos.value, colliderCentre);
        }
        if (pathToTarget.Count > 0)
        {
            distanceToNextPos = Vector2.Distance(currentPos, pathToTarget[0].value);

            if (distanceToNextPos < closeEnoughToNextPos || (lastPos.type == PathData.PosType.Player && lastPos.value.x != pathToTarget[pathToTarget.Count - 1].value.x))
            {
                pathData.pathIsSet = false;
            }
        }

    }

    public virtual void FindPath(Vector2 currentPos, Vector2 lastPos, float colliderCentre)
    {
        pathToTarget.Clear();
        pathData.pathIsSet = true;
    }
}
