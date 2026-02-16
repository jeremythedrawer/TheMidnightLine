using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerStatsSO", menuName = "Midnight Line SOs / Spawner Stats SO")]
public class SpawnerStatsSO : ScriptableObject
{    
    public int curBackgroundDataIndex;
    
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnSize;

    public float lodZPosition0;
    public float lodZPosition1;

    public int updateKernelID;
    public int initKernelID;
    public int computeGroups;

    public ComputeBuffer particleComputeBuffer;
    public ComputeBuffer backgroundParticleInputBuffer;
    public Bounds renderParamsBounds;
    public AtlasSpawn.BackgroundType curBackgroundTypes;

    public int backgroundMaskCount;
    public AtlasSpawn.BackgroundParticleInputs[] backgroundInputsArray;

    public void CreateBoundWideMesh()
    {
        Mesh boundWideMesh = new Mesh();
        boundWideMesh.name = "BoundWideMesh";


        Vector2 min = spawnMinPos;
        Vector2 max = spawnMaxPos;

        Vector3 offset = new Vector3(min.x, min.y, 0f);

        boundWideMesh.vertices = new Vector3[]
        {
        new Vector3(min.x, min.y, 0f) - offset, // bottom left  (0,0)
        new Vector3(max.x, min.y, 0f) - offset, // bottom right
        new Vector3(max.x, max.y, 0f) - offset, // top right
        new Vector3(min.x, max.y, 0f) - offset, // top left
        };

        boundWideMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        boundWideMesh.triangles = new int[]
        {
            0, 2, 1,
            0, 3, 2
        };

        boundWideMesh.RecalculateNormals();
        boundWideMesh.RecalculateBounds();

        AssetDatabase.CreateAsset(boundWideMesh, "Assets/FBXs/boundWideMesh.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

}


[CustomEditor(typeof(SpawnerStatsSO))]
public class SpriteNormUVCalculatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpawnerStatsSO spawnStats = (SpawnerStatsSO)target;

        if (GUILayout.Button("Regenerate Bound Mesh"))
        {
            spawnStats.CreateBoundWideMesh();
        }
    }
}
