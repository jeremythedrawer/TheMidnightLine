using UnityEngine;

public class CalmPath : PathFinder
{
    public override void SetPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.SetPath(currentPos, targetPos, colliderCentre);

    }
    public override void FindPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.FindPath(currentPos, targetPos, colliderCentre);
        if (currentInsideBounds != null)
        {
            InsideBoundsPath(currentPos, targetPos);
        }
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
            FindChosenClimbBounds(chosenGangway, this, targetPos, currentPos);

            AddToPath(new Vector2(chosenGangway.transform.position.x, currentPos.y), PathData.PosType.Gangway);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Target);
        }
    }
}
