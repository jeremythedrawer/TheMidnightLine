using System;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "LayerSettings_SO", menuName = "Midnight Line SOs / Layer Settings SO")]
public class LayerSettingsSO : ScriptableObject
{
    [Serializable] public struct StationLayers
    {
        public LayerMask ground;
    }
    public StationLayers stationLayers;

    [Serializable] public struct TrainLayers
    {
        public LayerMask ground;
        public LayerMask slideDoors;
        public LayerMask carriageChairs;
        public LayerMask insideCarriageBounds;
        public LayerMask gangwayBounds;
        public LayerMask roofBounds;
        public LayerMask climbingBounds;
        public LayerMask gangwayDoor;
        public LayerMask carriageExteriorWalls;
    }
    public TrainLayers trainLayers;

    public LayerMask spy;

    internal LayerMask stationMask;
    internal LayerMask trainMask;
    private void OnValidate()
    {
        stationMask = CombineLayerMasks(stationLayers);
        trainMask = CombineLayerMasks(trainLayers);
    }

    private LayerMask CombineLayerMasks(object layers)
    {
        int mask = 0;

        FieldInfo[] fields = layers.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        for(int i = 0; i < fields.Length; i++)
        {
            if (fields[i].FieldType == typeof(LayerMask))
            {
                LayerMask layerMask = (LayerMask)fields[i].GetValue(layers);
                mask |= layerMask.value;
            }
        }

        return mask;
    }
}
