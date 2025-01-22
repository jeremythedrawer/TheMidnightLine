using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PathFinder : MonoBehaviour
{
    public PathData pathData;

    public bool pathIsSet { get; set; }
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
    protected CarriageClimbingBounds chosenClimbingBounds => pathData.chosenClimbingBounds;
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

    protected void FindChosenGangway(CarriageBounds currentCarriageBounds, float currentXPos, float targetXPos)
    {
        GangwayBounds rightGangway = currentCarriageBounds.rightGangwayBounds;
        Debug.Log(currentCarriageBounds);
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

    protected void FindChosenChairBounds(Vector2 currentPos)
    {
        SeatBounds[] setsOfSeats = currentInsideBounds.setsOfChairs.ToArray();

        float[] setOfSeatsDistances = new float[setsOfSeats.Length];

        for (int i = 0; i < setsOfSeats.Length; i++)
        {
            float distanceToSetOfSeats = Vector2.Distance(currentPos, setsOfSeats[i].transform.position);
            setOfSeatsDistances[i] = distanceToSetOfSeats;
        }

        float smallestDistance = float.MaxValue;
        float secondSmallestDistance = float.MaxValue;

        foreach (float distance in setOfSeatsDistances)
        {
            if(distance < smallestDistance) //iterating and comparing each index until the smallest value is found
            {
                secondSmallestDistance = smallestDistance;
                smallestDistance = distance;
            }
            else if (distance < secondSmallestDistance && distance != smallestDistance) //whatever is left is check one more time to find the second smallest distance
            {
                secondSmallestDistance = distance;
            }
        }

        int closestPosIndex = Array.IndexOf(setOfSeatsDistances, smallestDistance);
        int secondClosestPosIndex = Array.IndexOf(setOfSeatsDistances, secondSmallestDistance);

        if (!setsOfSeats[closestPosIndex].seats.All(seat => seat.filled)) // check if all seats in the found set of chairs are full
        {
            pathData.chosenSeatBounds = setsOfSeats[closestPosIndex];  
        }
        else if (!setsOfSeats[secondClosestPosIndex].seats.All(seat => seat.filled)) //check the second closest set of chairs
        {
            pathData.chosenSeatBounds = setsOfSeats[secondClosestPosIndex];
        }
        else // all chairs are already filled
        {
            pathData.chosenSeatBounds = null;
        }


    }
    protected void AddToPath(Vector2 position, PathData.PosType type)
    {
        pathData.pathToTarget.Add(new PathData.NamedPosition(position, type));
    }

    protected void DrawDebugPath(Vector2 currentPos)
    {
        Vector2 pos = currentPos;

        for (int i = 0; i < pathData.pathToTarget.ToArray().Length; i++)
        {
            Vector2 nextPos = pathData.pathToTarget[i].value;
            Debug.DrawLine(pos, nextPos, Color.magenta);
            Debug.DrawLine(nextPos, nextPos + (Vector2.up * 0.5f), Color.cyan);
            pos = nextPos;
        }
    }

    public virtual void SetPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        DrawDebugPath(currentPos);

        if (!pathIsSet)
        {
            FindPath(currentPos, targetPos, colliderCentre);
        }
        distanceToNextPos = Vector2.Distance(currentPos, pathToTarget[0].value);
        if (distanceToNextPos < closeEnoughToNextPos || targetPos.x != pathToTarget[pathToTarget.Count - 1].value.x)
        {
            pathIsSet = false;
        }
    }

    public virtual void FindPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        pathToTarget.Clear();
        pathIsSet = true;
    }
}
