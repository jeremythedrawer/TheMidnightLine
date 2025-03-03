using System.Collections.Generic;
using UnityEngine;

public abstract class ToEnvironmentPaths : PathController
{
    protected SlideDoorBounds chosenSlideDoorBounds => pathData.chosenSlideDoorBounds;
    protected Vector2 chosenSlidingDoorsPos;
    protected int stationGroundLayer => GlobalReferenceManager.Instance.stationGroundLayer;

    protected Vector2 SlideDoorsPath(Vector2 currentPos)
    {
        pathData.chosenSlideDoorBounds = FindChosenSlidingDoors(currentPos);

        BoxCollider2D chosenSlideDoorCollider = pathData.chosenSlideDoorBounds.boxCollider;
        float npcBoundsWidth = npcCore.boxCollider2D.size.x;
        float randomPosX = Random.Range(chosenSlideDoorCollider.bounds.min.x + npcBoundsWidth, chosenSlideDoorCollider.bounds.max.x - npcBoundsWidth);
        chosenSlidingDoorsPos = new Vector2(randomPosX, currentPos.y);
        return chosenSlidingDoorsPos;
    }

    protected SlideDoorBounds FindChosenSlidingDoors(Vector2 currentPos)
    {
        List<SlideDoorBounds> slideDoorsList = pathData.trainData.slideDoorsList;

        float shortestDistance = float.MaxValue;
        int chosenSlideDoorsIndex = -1;
        for (int i = 0; i < slideDoorsList.Count - 1; i++)
        {
            float distance = Mathf.Abs(currentPos.x - slideDoorsList[i].transform.position.x);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                chosenSlideDoorsIndex = i;
            }
        }
        return slideDoorsList[chosenSlideDoorsIndex];
    }
}
