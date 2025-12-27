using Proselyte.Sigils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
        public GameEventDataSO gameEventData;
        public LayerSettingsSO layerSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct ComponentData
    {
        public Transform[] wheelTransforms;
        public BoxCollider2D exteriorWallsCollider;
    }
    [SerializeField] ComponentData componentData;
    [Serializable] public struct ChairData
    {
        internal float xPos;
        internal bool filled;
    }
    internal ChairData[] chairData;

    internal float chairZPos;

    private void OnEnable()
    {
        ResetStats();
        soData.gameEventData.OnReset.RegisterListener(ResetStats);
        soData.gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(GetData);

    }

    private void OnDisable()
    {
        soData.gameEventData.OnReset.UnregisterListener(ResetStats);
        soData.gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(GetData);

    }

    private void Update()
    {
        if (soData.trainStats.wheelCircumference <= 0f) return;

        float wheelRotation = (soData.trainStats.metersTravelled / soData.trainStats.wheelCircumference) * 360f;

        wheelRotation %= 360f;

        foreach (Transform wheel in componentData.wheelTransforms)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, -wheelRotation);
        }
    }
    private void GetData()
    {
        Bounds checkBounds = componentData.exteriorWallsCollider.bounds;
        RaycastHit2D[] chairsHits = Physics2D.BoxCastAll(checkBounds.center, checkBounds.size, 0, transform.right, checkBounds.size.x, soData.layerSettings.trainLayers.carriageChairs);

        List<ChairData> chairDataList = new List<ChairData>();

        for (int i = 0; i < chairsHits.Length; i++)
        {
            SpriteRenderer chairRenderer = chairsHits[i].collider.GetComponent<SpriteRenderer>();
            float chairLength = chairRenderer.sprite.border.x;
            int chairAmount = Mathf.RoundToInt(chairRenderer.sprite.bounds.size.x / chairLength);
            float firstChairPos = chairsHits[i].transform.position.x + (chairLength * 0.5f);
            for (int j = 0; j < chairAmount; j++)
            {
                chairDataList.Add(new ChairData { xPos = firstChairPos + (chairLength * i), filled = false } );
            }
        }
        chairData = chairDataList.ToArray();
        chairZPos = chairsHits[0].transform.position.z - 1;
    }
    private void ResetStats()
    {
    }
    private void OnApplicationQuit()
    {
        ResetStats();
    }

    private void OnDrawGizmos()
    {
        if (soData.trainSettings == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + 2, soData.trainSettings.entityDepthRange.x), new Vector3(transform.position.x, transform.position.y + 2, soData.trainSettings.entityDepthRange.y));
    }
}
