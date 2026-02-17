using System;
using UnityEngine;
using static AtlasSpawn;
using static Atlas;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "SpawnerSettingsSO", menuName = "Midnight Line SOs / Spawner Settings SO")]
public class AtlasSpawnerSettingsSO : ScriptableObject
{
    public float boundBufferAmount = 16f;
    public float maxSpawnDepth = 50;
    public float minSpawnDepth = 1;
    public float spawnHeight = 0;
    public ComputeShader backgroundParticleCompute;
    public Material backgroundMaterial;

    public ParticleData[] particleData;
    public Biomes[] biomes;
}
