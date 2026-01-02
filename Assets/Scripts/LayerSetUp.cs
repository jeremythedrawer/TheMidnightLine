using System.Reflection;
using UnityEngine;

public class LayerSetUp : MonoBehaviour
{
    [SerializeField] LayerSettingsSO layerSettings;

    private void Awake()
    {
        layerSettings.stationMask = CombineLayerMasks(layerSettings.stationLayersStruct);
        layerSettings.trainMask = CombineLayerMasks(layerSettings.trainLayerStruct);
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
