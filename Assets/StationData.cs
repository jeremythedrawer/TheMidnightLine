using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StationData : MonoBehaviour
{
    [Header("Parameters")]
    public float accelerationThresholds;

    public float decelThreshold => transform.position.x - accelerationThresholds;
    public float accelThreshold => transform.position.x + accelerationThresholds;

    public List<StateCore> charactersList {  get; private set; }
    private int stationGroundLayer => LayerMask.NameToLayer("Station Ground");
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    private static TrainData trainData;
    private void Awake()
    {
        charactersList = new List<StateCore>(GetComponentsInChildren<StateCore>());
        trainData = GameObject.FindGameObjectWithTag("Train Object").GetComponent<TrainData>();
    }
    private void OnDrawGizmos()
    {
        DrawAccelThresholds(true);
    }

    private void Start()
    {
        DrawAccelThresholds(false);

        foreach (StateCore character in charactersList)
        {
            character.spriteRenderer.sortingOrder = 1;
            character.boxCollider2D.excludeLayers |= 1 << trainGroundLayer;
            character.boxCollider2D.excludeLayers &= ~(1 << stationGroundLayer);
            character.collisionChecker.activeGroundLayer = 1 << stationGroundLayer;
        }
    }
    private void DrawAccelThresholds(bool usingGizmos)
    {
#if UNITY_EDITOR
        float decelX = transform.position.x - accelerationThresholds;
        float accelX = transform.position.x + accelerationThresholds;
        float height = 10;
        Vector2 decelUpperOrigin = new Vector2(decelX , transform.position.y + height);
        Vector2 decelLowerOrigin = new Vector2(decelX , transform.position.y);

        Vector2 accelUpperOrigin = new Vector2(accelX, transform.position.y + height);
        Vector2 accelLowerOrigin = new Vector2(accelX, transform.position.y);

        if (usingGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(decelUpperOrigin, decelLowerOrigin);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine (accelUpperOrigin, accelLowerOrigin);
        }
        else
        {
            Debug.DrawLine(decelUpperOrigin , decelLowerOrigin, Color.red);
            Debug.DrawLine(accelLowerOrigin , accelUpperOrigin, Color.blue);
        }
#endif
    }
}
