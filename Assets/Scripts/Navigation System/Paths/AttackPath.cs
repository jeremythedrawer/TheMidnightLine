using System.Collections.Generic;
using UnityEngine;

public class AttackPath : ToPlayerPaths
{
    protected OutsideBounds currentOutsideBounds => pathData.currentOutsideBounds;
    public ClimbingBounds chosenClimbingBounds => pathData.chosenClimbingBounds;

    public override void InsideBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        base.InsideBoundsPath(currentPos, targetPos);
        if (currentInsideBounds.playerInActiveArea) return;

        FindChosenClimbBounds(pathData.chosenGangwayBounds, this, targetPos, currentPos);
        if (pathData.chosenClimbingBounds != null)
        {
            InsertToPath(pathData.chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound, pathToTarget.Count - 2);
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
            FindChosenClimbBounds(pathData.chosenGangwayBounds, this, targetPos, currentPos);

            AddToPath(new Vector2(pathData.chosenGangwayBounds.Bounds.center.x, pathData.chosenGangwayBounds.Bounds.min.y + colliderCentre), PathData.PosType.GangwayBound);

            if (chosenClimbingBounds != null)
            {
                AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
            }

            AddToPath(targetPos, PathData.PosType.Player);
        }
    }

    private void GangwayBoundsPath(Vector2 currentPos, Vector2 targetPos, float colliderCenter)
    {
        FindChosenClimbBounds(pathData.chosenGangwayBounds, this, targetPos, currentPos);

        if (chosenClimbingBounds != null) // player is on roof and agent is on gangway
        {
            AddToPath(chosenClimbingBounds.transform.position, PathData.PosType.ClimbingBound);
        }
        else // player is in train and agent is on gangway
        {
            FindChosenInsideBounds(pathData.chosenGangwayBounds, currentPos, targetPos.x);

            if (pathData.chosenInsideBounds != null)
            {
                AddToPath(new Vector2(pathData.chosenInsideBounds.transform.position.x, currentPos.y), PathData.PosType.InsideBound);
            }
        }

        FindChosenOutsideBounds(pathData.chosenGangwayBounds, targetPos, currentPos.x);

        if (pathData.chosenOutsideBounds != null) //player is on roof and agent is directly above gangway
        {
            AddToPath(pathData.chosenOutsideBounds.transform.position, PathData.PosType.OutsideBound);
        }

        AddToPath(targetPos, PathData.PosType.Player);
    }

    protected void FindChosenClimbBounds(GangwayBounds chosenGangway, PathController path, Vector2 targetPos, Vector2 currentPos)
    {
        if (currentPos.y > trainData.roofLevel)
        {
            pathData.chosenClimbingBounds = null;
            return;
        }

        if (path is AttackPath)
        {
            if (targetPos.y > trainData.roofLevel)
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
        if (targetPos.y < trainData.roofLevel)
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
}
