using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LoopingTileSpawner))]
public class LoopingTileSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LoopingTileSpawner loopingTileSpawner = (LoopingTileSpawner)target;

        if (loopingTileSpawner.startSpawnDistance == 0 )
        {
            if(GUILayout.Button("Preset Tile Positions"))
            {
                loopingTileSpawner.PresetTilePositions();   
            }
        }
    }
}
