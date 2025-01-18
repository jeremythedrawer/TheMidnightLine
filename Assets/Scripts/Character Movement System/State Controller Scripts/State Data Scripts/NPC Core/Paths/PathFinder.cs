using System.Collections.Generic;
using UnityEngine;

public abstract class PathFinder : MonoBehaviour
{
    public PathData pathData;
    protected void FindChosenClimbBounds(GangwayBounds chosenGangway)
    {
        if (pathData.chosenGangway.playerOnLeftRoof)
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

    protected void FindChosenInsideBounds(GangwayBounds chosenGangway, float currentXPos, float targetXPos)
    {
        if (chosenGangway.CompareTag("Inside Bounds"))
        {
            if (currentXPos < targetXPos)
            {
                pathData.chosenInsideBounds = chosenGangway.foundRightInsideBounds;
            }
            else
            {
                pathData.chosenInsideBounds = chosenGangway.foundLeftInsideBounds;
            }
        }
        else
        {
            pathData.chosenInsideBounds = null;
        }
    }

    protected void FindChosenGangway(ActivateCarriageBounds activateCarriageBounds, float currentXPos, float targetXPos)
    {
        GangwayBounds rightGangway = activateCarriageBounds.rightGangwayBounds;
        GangwayBounds leftGangway = activateCarriageBounds.leftGangwayBounds;

        if (activateCarriageBounds.isBackCarriage) 
        {
            pathData.chosenGangway = rightGangway;
        }
        else if (activateCarriageBounds.isFrontCarriage) 
        {
            pathData.chosenGangway = leftGangway;
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
                pathData.chosenGangway = leftGangway;
            }
            else
            {
                pathData.chosenGangway = rightGangway;
            }
        }
    }


    public void AddToPath(Vector2 position, PathData.PosType type)
    {
        pathData.pathToTarget.Add(new PathData.NamedPosition(position, type));
    }


    public void DrawDebugPath(Vector2 currentPos)
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
}
