using UnityEngine;

public class ToPlayerPaths : PathController
{
    protected InsideBounds currentInsideBounds => pathData.currentInsideBounds;
    protected GangwayBounds currentGangwayBounds => pathData.currentGangwayBounds;

    public virtual void InsideBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        if (currentInsideBounds.playerInActiveArea)
        {
            AddToPath(targetPos, PathData.PosType.Player);
        }
        else
        {
            FindChosenGangway(currentInsideBounds, currentPos.x, targetPos.x);
            AddToPath(new Vector2(pathData.chosenGangwayBounds.transform.position.x, currentPos.y), PathData.PosType.GangwayBound);

            AddToPath(targetPos, PathData.PosType.Player);
        }
    }

    protected void FindChosenInsideBounds(GangwayBounds chosenGangway, Vector2 currentPos, float targetXPos)
    {
        if (currentPos.y >= trainData.roofLevel)
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

}
