using System;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "LayerSettings_SO", menuName = "Midnight Line SOs / Layer Settings SO")]
public class LayerData : ScriptableObject
{
    [Serializable] public struct StationLayers
    {
        public LayerMask ground;
    }
    [Serializable] public struct TrainLayers
    {
        public LayerMask ground;
        public LayerMask gangwayDoor;
    }

    public StationLayers stationLayers;
    public TrainLayers trainLayers;

    public LayerMask stationWallLayers;
    public LayerMask trainWallLayers;

    [Header("Generated")]
    public LayerMask stationMask;
    public LayerMask trainMask;

    private void OnEnable()
    {
        CombineAllLayerMasks();
    }
    public void CombineAllLayerMasks()
    {
        stationMask = CombineLayerMasks(stationLayers);
        trainMask = CombineLayerMasks(trainLayers);
    }
    private LayerMask CombineLayerMasks(object layers)
    {
        int mask = 0;

        FieldInfo[] fields = layers.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        for (int i = 0; i < fields.Length; i++)
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
