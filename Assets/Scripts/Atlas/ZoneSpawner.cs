using UnityEngine;
using static AtlasSpawn;

[ExecuteAlways]
public class ZoneSpawner : MonoBehaviour
{
    public ZoneArea area;

    public MaterialIDSO materialIDs;
    public ZoneSpawnerSO spawner;
    public TripSO trip;
    public TrainStatsSO trainStats;

    [Header("Generated")]
    public ZoneSpawnerData zoneSpawnerData;
    public Zone curZone;
    public int curZoneIndex;
    public uint[] deadCounter;
    public GraphicsBuffer uvSizeAndPositionBuffer;
    public GraphicsBuffer worldSizeBuffer;
    public ComputeBuffer deadCountBuffer;
    public ComputeBuffer particleBuffer;

    private void OnDisable()
    {
        Dispose();
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (zoneSpawnerData.zones.Length == 0) return;
        ChangeZone();


        if (trainStats.metersTravelled >= curZone.metersEnd && zoneSpawnerData.active)
        {
            spawner.atlasCompute.SetInt(ACTIVE_STRINGS[(int)area], 0);
            deadCountBuffer.GetData(deadCounter);
            if (deadCounter[0] == zoneSpawnerData.particleCount)
            {

                curZoneIndex++;
                zoneSpawnerData.active = false;
                if (curZoneIndex < zoneSpawnerData.zones.Length)
                {
                    curZone = zoneSpawnerData.zones[curZoneIndex];
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        spawner.atlasCompute.Dispatch(zoneSpawnerData.kernelID_update, zoneSpawnerData.computeGroupSize, 1, 1);
    }
    public void InitializeZoneSpawnData()
    {
        zoneSpawnerData = trip.zoneSpawnerData[(int)area];
        if (zoneSpawnerData.zones.Length == 0) return;
        curZone = zoneSpawnerData.zones[curZoneIndex];

        zoneSpawnerData.mpb = new MaterialPropertyBlock();
        particleBuffer = new ComputeBuffer(PARTICLE_COUNTS[(int)area], ZONE_STRIDE);
        deadCountBuffer = new ComputeBuffer(1, INT_SIZE);

        zoneSpawnerData.kernelID_init = spawner.atlasCompute.FindKernel(INIT_KERNEL_STRINGS[(int)area]);
        zoneSpawnerData.kernelID_update = spawner.atlasCompute.FindKernel(UPDATE_KERNEL_STRINGS[(int)area]);
        zoneSpawnerData.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[(int)area] / (float)THREADS_PER_GROUP);
        zoneSpawnerData.particleCount = PARTICLE_COUNTS[(int)area];

        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_init, BUFFER_STRINGS[(int)area], particleBuffer);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, BUFFER_STRINGS[(int)area], particleBuffer);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, DEAD_COUNT_STRINGS[(int)area], deadCountBuffer);
        zoneSpawnerData.mpb.SetBuffer(materialIDs.ids.particles, particleBuffer);
        ChangeZone();
    }
    private void ChangeZone()
    {
        if (trainStats.metersTravelled < curZone.metersStart || zoneSpawnerData.active) return;

        uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, curZone.atlas.simpleSprites.Length, FLOAT4_SIZE);
        uvSizeAndPositionBuffer.SetData(curZone.zoneUVSizeAndPosArray);

        worldSizeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, curZone.atlas.simpleSprites.Length, FLOAT2_SIZE);
        worldSizeBuffer.SetData(curZone.zoneWorldSizesArray);

        zoneSpawnerData.mpb.SetInt(materialIDs.ids.spriteCount, curZone.atlas.simpleSprites.Length);
        zoneSpawnerData.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, uvSizeAndPositionBuffer);
        zoneSpawnerData.mpb.SetBuffer("_WorldSize", worldSizeBuffer);
        zoneSpawnerData.mpb.SetTexture(materialIDs.ids.atlasTexture, curZone.atlas.texture);
        deadCounter = new uint[1];
        zoneSpawnerData.active = true;

        deadCountBuffer.SetData(deadCounter);

        spawner.atlasCompute.SetInt(ACTIVE_STRINGS[(int)area], 1);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, DEAD_COUNT_STRINGS[(int)area], deadCountBuffer);
        spawner.atlasCompute.Dispatch(zoneSpawnerData.kernelID_init, zoneSpawnerData.computeGroupSize, 1, 1);
    }
    public void Dispose()
    {
        uvSizeAndPositionBuffer?.Release();
        worldSizeBuffer?.Release();
        particleBuffer?.Release();
        deadCountBuffer?.Release();
        zoneSpawnerData.active = false;
    }
}
