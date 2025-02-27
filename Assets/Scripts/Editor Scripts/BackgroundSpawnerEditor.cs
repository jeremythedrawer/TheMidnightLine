using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BackgroundSpawner))]
public class BackgroundSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BackgroundSpawner backgroundSpawner = (BackgroundSpawner)target;

        if (GUILayout.Button("Increase Preset Count"))
        {
            backgroundSpawner.IncreasePresetPrefabs();
        }
        if (GUILayout.Button("Decrease Preset Count"))
        {
            backgroundSpawner.DecreasePresetPrefabs();
        }
    }
}
