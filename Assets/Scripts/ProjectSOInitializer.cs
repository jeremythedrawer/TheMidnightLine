using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class ProjectSOInitializer : MonoBehaviour
{
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] MaterialIDSO materialIDs;

    private void Awake()
    {
        layerSettings.CombineAllLayerMasks();
        materialIDs.SetMaterialPropertyIDs();
    }
}
