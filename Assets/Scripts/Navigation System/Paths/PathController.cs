using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathController : MonoBehaviour
{
    public PathData pathData {  get; private set; }
    protected NPCCore npcCore;
    public float distanceToNextPos { get; protected set; }
    public float closeEnoughToNextPos { get; private set; } = 0.1f;
    public List<PathData.NamedPosition> pathToTarget => pathData.pathToTarget;

    protected TrainData trainData => pathData.trainData;
    public bool isFindingMovingPathPoint { get; private set; }

    public virtual void Start()
    {
        pathData = GetComponentInParent<PathData>();
        npcCore = GetComponentInParent<NPCCore>();    
    }
    public virtual void SetPath(Vector2 currentPos, float colliderCentre)
    {
    }

    protected void FindMovingPathPoint(int pathPointIndex, System.Func<Vector2, Vector2> PathPointPath, Vector2 currentPos)
    {
        if (!isFindingMovingPathPoint)
        {
            StartCoroutine(FindingMovingPathPoint(pathPointIndex, PathPointPath, currentPos));
            isFindingMovingPathPoint = true;
        }
    }
    private IEnumerator FindingMovingPathPoint(int pathPointIndex, System.Func<Vector2, Vector2> PathPointPath, Vector2 currentPos)
    {
        PathData.PosType posType = pathToTarget[pathPointIndex].type;
        while (pathPointIndex < pathToTarget.Count && pathToTarget[pathPointIndex].type == posType)
        {
            Vector2 pathPoint = PathPointPath(currentPos);

            PathData.NamedPosition newPathPoint = new PathData.NamedPosition(pathPoint, posType);
            pathToTarget[pathPointIndex] = newPathPoint;
            yield return new WaitForSeconds(3);
        }
        isFindingMovingPathPoint = false;
    }
    protected void AddToPath(Vector2 position, PathData.PosType type)
    {
        pathData.pathToTarget.Add(new PathData.NamedPosition(position, type));
    }
    protected void InsertToPath(Vector2 position, PathData.PosType type, int index)
    {
        pathData.pathToTarget.Insert(index, new PathData.NamedPosition(position, type));
    }
}
