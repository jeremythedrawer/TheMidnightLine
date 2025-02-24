using System.Collections.Generic;
using UnityEngine;

public class AttackPath : PathFinder
{
    public override void SetPath(Vector2 currentPos, PathData.NamedPosition targetPos, float colliderCentre)
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
        else if (currentGangwayBounds != null)
        {
            GangwayBoundsPath(currentPos, targetPos, colliderCentre);
        }
        else if (currentOutsideBounds != null)
        {
            OutsideBoundsPath(currentPos, targetPos, colliderCentre);
        }
    }

    private void InsideBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        if (currentInsideBounds.playerInActiveArea)
        {
            AddToPath(targetPos, PathData.PosType.Player);
        }
        else
        {
            FindChosenGangway(currentInsideBounds, currentPos.x, targetPos.x);
            FindChosenClimbBounds(chosenGangway, this, targetPos, currentPos);

            AddToPath(new Vector2(chosenGangway.transform.position.x, currentPos.y), PathData.PosType.GangwayBound);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Player);
        }
    }

    private void OutsideBoundsPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        if (targetPos.y >= trainData.roofLevel)
        {
            FindChosenGangway(currentOutsideBounds, currentPos.x, targetPos.x);

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
            AddToPath(targetPos, PathData.PosType.Player);
        }
        else
        {
            FindChosenGangway(currentOutsideBounds, currentPos.x, targetPos.x);
            FindChosenClimbBounds(chosenGangway, this, targetPos, currentPos);

            AddToPath(new Vector2(chosenGangway.Bounds.center.x, chosenGangway.Bounds.min.y + colliderCentre), PathData.PosType.GangwayBound);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Player);
        }
    }

    private void GangwayBoundsPath(Vector2 currentPos, Vector2 targetPos, float colliderCenter)
    {
        FindChosenClimbBounds(chosenGangway, this, targetPos, currentPos);

        if (chosenClimbingBounds != null) // player is on roof and agent is on gangway
        {
            AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
        }
        else // player is in train and agent is on gangway
        {
            FindChosenInsideBounds(chosenGangway, currentPos, targetPos.x);

            if (chosenInsideBounds != null)
            {
                AddToPath(new Vector2(chosenInsideBounds.transform.position.x, currentPos.y), PathData.PosType.InsideBound);
            }
        }

        FindChosenOutsideBounds(chosenGangway, targetPos, currentPos.x);

        if (chosenOutsideBounds != null) //player is on roof and agent is directly above gangway
        {
            AddToPath(chosenOutsideBounds.transform.position, PathData.PosType.OutsideBound);
        }

        AddToPath(targetPos, PathData.PosType.Player);
    }
}
