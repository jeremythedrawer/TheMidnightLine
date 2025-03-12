using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StationData))]
public class StationDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StationData stationData = (StationData)target;

        if (GUILayout.Button("Create Spawn Area List"))
        {
            stationData.SetSpawnAreas();
            EditorUtility.SetDirty(stationData);
        }
    }
}
