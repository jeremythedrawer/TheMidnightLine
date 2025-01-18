using System.Collections.Generic;
using UnityEngine;

public class AttackPath : PathFinder
{
    public bool pathIsSet { get; set; }
    public float distanceToNextPos {  get; private set; }
    public float closeEnoughToNextPos { get; set; } = 0.5f;
    public List<PathData.NamedPosition> pathToTarget => pathData.pathToTarget;

    private TrainBounds trainBounds => pathData.trainBounds;

    private ActivateCarriageBounds currentInsideBounds => pathData.currentInsideBounds;
    private GangwayBounds currentGangwayBounds => pathData.currentGangwayBounds;
    private ActivateCarriageBounds currentOutsideBounds => pathData.currentOutsideBounds;

    private GangwayBounds chosenGangway => pathData.chosenGangway;
    private CarriageClimbingBounds chosenClimbingBounds => pathData.chosenClimbingBounds;
    private ActivateCarriageBounds chosenInsideBounds => pathData.chosenInsideBounds;

    public void SetAttackPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        if (!pathIsSet)
        {
            FindAttackPath(currentPos, targetPos, colliderCentre);
        }

        distanceToNextPos = Vector2.Distance(currentPos, pathToTarget[0].value);

        if (distanceToNextPos < closeEnoughToNextPos || targetPos.x != pathToTarget[pathToTarget.Count - 1].value.x)
        {
            pathIsSet = false;
        }
        DrawDebugPath(currentPos);
    }

    private void FindAttackPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        pathToTarget.Clear();

        if (currentInsideBounds != null)
        {
            InsideBoundsPath(currentPos, targetPos);
        }
        else if (currentGangwayBounds != null)
        {
            GangwayBoundsPath(currentPos, targetPos);
        }
        else if (currentOutsideBounds != null)
        {
            OutsideBoundsPath(currentPos, targetPos, colliderCentre);
        }
        pathIsSet = true;
    }

    private void InsideBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        if (currentInsideBounds.playerInActiveArea)
        {
            AddToPath(targetPos, PathData.PosType.Target);
        }
        else
        {
            FindChosenGangway(currentInsideBounds, currentPos.x, targetPos.x);
            FindChosenClimbBounds(chosenGangway);

            AddToPath(new Vector2(chosenGangway.transform.position.x, currentPos.y), PathData.PosType.Gangway);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Target);
        }
    }

    private void OutsideBoundsPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        if (targetPos.y >= trainBounds.roofLevel)
        {
            if (!currentOutsideBounds.playerInActiveArea)
            {
                if (targetPos.x > currentPos.x)
                {
                    AddToPath(new Vector2(currentOutsideBounds.rightEdge, currentPos.y), PathData.PosType.RoofEdge);
                }
                else
                {
                    AddToPath(new Vector2(currentOutsideBounds.leftEdge, currentPos.y), PathData.PosType.RoofEdge);
                }
            }
            AddToPath(targetPos, PathData.PosType.Target);
        }
        else
        {
            FindChosenGangway(currentOutsideBounds, currentPos.x, targetPos.x);
            FindChosenClimbBounds(chosenGangway);

            AddToPath(new Vector2(chosenGangway.Bounds.center.x, chosenGangway.Bounds.min.y + colliderCentre), PathData.PosType.Gangway);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Target);
        }
    }

    private void GangwayBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        if (chosenGangway != null)
        {
            FindChosenClimbBounds(chosenGangway);
        }

        if (chosenClimbingBounds != null && currentPos.y < trainBounds.roofLevel)
        {
            AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
        }
        else if (chosenGangway != null)
        {
            FindChosenInsideBounds(chosenGangway, currentPos.x, targetPos.x);
            if (chosenInsideBounds != null)
            {
                AddToPath(new Vector2(chosenInsideBounds.transform.position.x, currentPos.y), PathData.PosType.InsideBound);
            }
        }

        AddToPath(targetPos, PathData.PosType.Target);
    }
}
