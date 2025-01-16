using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationSystem : MonoBehaviour
{
    [Header("References")]
    public TrainBounds trainBounds;

    public ActivateCarriageBounds currentInsideBounds { private get; set; }
    public GangwayBounds currentGangwayBounds { private get; set; }
    public ActivateCarriageBounds currentOutsideBounds { private get; set; }

    public List<NamedPosition> pathToTarget { get; set; } = new List<NamedPosition>();
    public enum PosType
    {
        InsideBound,
        OutsideBound,
        Gangway,
        ClimbingBound,
        RoofEdge,
        Target
    }

    [System.Serializable]
    public struct NamedPosition
    {  
        public Vector2 value;  
        public PosType type;
        
        public NamedPosition(Vector2 value, PosType type)
        {
            this.value = value;
            this.type = type;
        }
    }
    public bool pathIsSet { get; set; }

    private GangwayBounds chosenGangway;
    private CarriageClimbingBounds chosenClimbingBounds;
    private ActivateCarriageBounds chosenInsideBounds;

    public float closeEnoughToNextPos { get; private set; } = 0.5f;
    public float distanceToNextPos {  get; private set; }

    public void SetPath(Vector2 currentPos, Vector2 targetPos)
    {
        if (!pathIsSet)
        {
            Debug.Log("finding new path");
            FindPathToTarget(currentPos, targetPos);
        }

        distanceToNextPos = Vector2.Distance(currentPos, pathToTarget[0].value);

        if (distanceToNextPos < closeEnoughToNextPos || targetPos != pathToTarget[pathToTarget.Count - 1].value)
        {
            pathIsSet = false;
        }
        DrawDebugPath(currentPos);
    }
    public void FindPathToTarget(Vector2 currentPos, Vector2 targetPos)
    {
        pathToTarget.Clear();

        if (currentInsideBounds != null) // Agent in Inside Bounds
        {

            if (currentInsideBounds.playerInActiveArea)
            {
                AddToPath(targetPos, PosType.Target);
            }
            else
            {
                FindChosenGangway(currentInsideBounds, currentPos.x, targetPos.x);
                FindChosenClimbBounds(chosenGangway);

                AddToPath(new Vector2 (chosenGangway.transform.position.x, currentPos.y), PosType.Gangway);

                if (chosenClimbingBounds != null)
                {
                    AddToPath(chosenClimbingBounds.transform.position, PosType.ClimbingBound);
                }

                AddToPath(targetPos, PosType.Target);
            }
        }
        else if (currentGangwayBounds != null) // Agent in Gangway Bounds
        {
            if (chosenGangway != null)
            {
                FindChosenClimbBounds(chosenGangway);
                
            }

            if (chosenClimbingBounds != null && currentPos.y < trainBounds.roofLevel)
            {
                AddToPath(chosenClimbingBounds.transform.position, PosType.ClimbingBound);
            }
            else if (chosenGangway != null)
            {
                FindChosenInsideBounds(chosenGangway, currentPos.x, targetPos.x);
                AddToPath(new Vector2 (chosenInsideBounds.transform.position.x, currentPos.y), PosType.InsideBound);
            }

            AddToPath(targetPos, PosType.Target);
        }
        else if (currentOutsideBounds != null) // Agent in Outside Bounds
        {

            if (targetPos.y >= trainBounds.roofLevel)
            {
                if (!currentOutsideBounds.playerInActiveArea)
                {
                    if (targetPos.x > currentPos.x)
                    {
                        AddToPath(new Vector2(currentOutsideBounds.rightEdge, currentPos.y), PosType.RoofEdge);
                    }
                    else
                    {
                        AddToPath(new Vector2(currentOutsideBounds.leftEdge, currentPos.y), PosType.RoofEdge);
                    }
                }
                AddToPath(targetPos, PosType.Target);
            }
            else
            {
                FindChosenGangway(currentOutsideBounds, currentPos.x, targetPos.x);
                FindChosenClimbBounds(chosenGangway);
                AddToPath(new Vector2(chosenGangway.transform.position.x, currentPos.y), PosType.Gangway);

                if(chosenClimbingBounds != null)
                {
                    AddToPath(chosenClimbingBounds.transform.position, PosType.ClimbingBound);
                }
                AddToPath(targetPos, PosType.Target);
            }
        }
        pathIsSet = true;
    }


    private void FindChosenClimbBounds(GangwayBounds chosenGangway)
    {
        if (chosenGangway.playerOnLeftRoof)
        {
            chosenClimbingBounds = chosenGangway.foundsLeftClimbBounds;
        }
        else if (chosenGangway.playerOnRightRoof)
        {
            chosenClimbingBounds = chosenGangway.foundsRightClimbBounds;
        }
        else
        {
            chosenClimbingBounds = null;

        }
    }

    private void FindChosenInsideBounds(GangwayBounds chosenGangway, float currentXPos, float targetXPos)
    {
        if (currentXPos < targetXPos)
        {
            chosenInsideBounds = chosenGangway.foundRightInsideBounds;
        }
        else
        {
            chosenInsideBounds = chosenGangway.foundLeftInsideBounds;
        }
    }

    private void FindChosenGangway(ActivateCarriageBounds activateCarriageBounds, float currentXPos, float targetXPos)
    {
        GangwayBounds rightGangway = activateCarriageBounds.rightGangwayBounds;
        GangwayBounds leftGangway = activateCarriageBounds.leftGangwayBounds;
            
        if (activateCarriageBounds.isBackCarriage) 
        { 
            chosenGangway = rightGangway;
        }
        else if (activateCarriageBounds.isFrontCarriage) 
        { 
            chosenGangway = leftGangway;
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
                chosenGangway = leftGangway;
            }
            else
            {
                chosenGangway = rightGangway;
            }
        }
    }

    private void AddToPath(Vector2 position, PosType type)
    {
        pathToTarget.Add(new NamedPosition(position, type));
    }

    private void DrawDebugPath(Vector2 currentPos)
    {
        Vector2 pos = currentPos;

        for (int i = 0; i < pathToTarget.ToArray().Length; i++)
        {
            Vector2 nextPos = pathToTarget[i].value;
            Debug.DrawLine(pos, nextPos, Color.magenta);
            Debug.DrawLine(nextPos, nextPos + (Vector2.up * 0.5f), Color.cyan);
            pos = nextPos;
        }
    }
}
