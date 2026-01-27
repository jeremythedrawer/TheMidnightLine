using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerSettingsSO", menuName = "Midnight Line SOs / Spawner Settings SO")]
public class SpawnerSettingsSO : ScriptableObject
{
    public float bufferAmount = 16f;
    public float maxSpawnDepth = 50;
    public float minSpawnDepth = 1;
    public float spawnHeight = 0;
    public int maxParticleCount = 1024;
    public ComputeShader backgroundParticleCompute;
    public Material backgroundMaterial;
    public Spawn.ParticleData[] particleData;
    public Spawn.TimeStamp[] timeStamp;
}
