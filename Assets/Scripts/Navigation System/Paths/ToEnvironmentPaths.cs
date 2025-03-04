using System.Collections.Generic;
using UnityEngine;

public abstract class ToEnvironmentPaths : PathController
{
    protected SlideDoorBounds chosenSlideDoorBounds => pathData.chosenSlideDoorBounds;
    protected Vector2 chosenSlidingDoorsPos;
    protected Vector2 chosenExitPos;
    protected int stationGroundLayer => GlobalReferenceManager.Instance.stationGroundLayer;

    protected Vector2 SlideDoorsPath(Vector2 currentPos)
    {
        pathData.chosenSlideDoorBounds = FindChosenSlidingDoors(currentPos);

        BoxCollider2D chosenSlideDoorCollider = pathData.chosenSlideDoorBounds.boxCollider;

        chosenSlidingDoorsPos = RandomBoundsPos(chosenSlideDoorCollider, currentPos);
        return chosenSlidingDoorsPos;
    }

    protected Vector2 ExitBoundsPath(Vector2 currentPos)
    {
        pathData.chosenExitBounds = FindChosenExitBounds(currentPos);

        BoxCollider2D chosenExitBoundsCollider = pathData.chosenExitBounds.boxCollider2D;

        chosenExitPos = RandomBoundsPos(chosenExitBoundsCollider, currentPos);

        return chosenExitPos;
    }
    protected SlideDoorBounds FindChosenSlidingDoors(Vector2 currentPos)
    {
        List<SlideDoorBounds> slideDoorsList = trainData.slideDoorsList;

        int chosenSlideDoorsIndex = FindShortestDistanceIndex(currentPos, slideDoorsList);
        return slideDoorsList[chosenSlideDoorsIndex];
    }

    protected ExitBounds FindChosenExitBounds(Vector2 currentPos)
    {
        BystanderBrain bystanderBrain = npcCore as BystanderBrain;
        List<ExitBounds> exitBoundsList = bystanderBrain.departureStation.exitBoundsList;
        int chosenExitBoundsIndex = FindShortestDistanceIndex(currentPos, exitBoundsList);
        return exitBoundsList[chosenExitBoundsIndex];
    }

    private Vector2 RandomBoundsPos(BoxCollider2D chosenBoxCollider, Vector2 currentPos)
    {
        float npcBoundsWidth = npcCore.boxCollider2D.size.x;
        float randomPosX = Random.Range(chosenBoxCollider.bounds.min.x + npcBoundsWidth, chosenBoxCollider.bounds.max.x - npcBoundsWidth);
        chosenSlidingDoorsPos = new Vector2(randomPosX, currentPos.y);
        return chosenSlidingDoorsPos;
    }
    private int FindShortestDistanceIndex<T>(Vector2 currentPos, List<T> boundsList) where T : Component
    {
        float shortestDistance = float.MaxValue;
        int chosenSlideDoorsIndex = -1;
        for (int i = 0; i < boundsList.Count; i++)
        {
            float distance = Mathf.Abs(currentPos.x - boundsList[i].transform.position.x);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                chosenSlideDoorsIndex = i;
            }
        }
        
        return chosenSlideDoorsIndex;
    }
}
