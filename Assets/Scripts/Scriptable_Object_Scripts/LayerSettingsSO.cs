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
    public StationLayers stationLayersStruct;

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
        public LayerMask smokingRoom;
        public LayerMask carriage;
    }
    public TrainLayers trainLayerStruct;

    public LayerMask spy;
    public LayerMask npc;
    public LayerMask phone;
    internal LayerMask stationMask;
    internal LayerMask trainMask;
    private void OnValidate()
    {
        stationMask = CombineLayerMasks(stationLayersStruct);
        trainMask = CombineLayerMasks(trainLayerStruct);
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
