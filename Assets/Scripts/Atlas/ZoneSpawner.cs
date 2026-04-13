using UnityEngine;
using static AtlasSpawn;
using static Atlas;
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
    public ComputeBuffer deadCountBuffer;
    public ComputeBuffer outputBuffer;
    public ComputeBuffer inputBuffer;

    public ZoneInput[] zoneInput;
    public string zoneName;

    private void Awake()
    {

    }
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


        if (trip.ticketsCheckedSinceStart >= curZone.ticketCheckEnd && zoneSpawnerData.active)
        {
            
            spawner.atlasCompute.SetInt(zoneName + ACTIVE_STRING, 0);
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
        zoneName = ZONE_STRINGS[(int)area];

        zoneSpawnerData = trip.zoneSpawnerData[(int)area];
        if (zoneSpawnerData.zones.Length == 0) return;
        curZone = zoneSpawnerData.zones[curZoneIndex];

        zoneSpawnerData.mpb = new MaterialPropertyBlock();
        outputBuffer = new ComputeBuffer(PARTICLE_COUNTS[(int)area], ZONE_OUTPUT_STRIDE);
        deadCountBuffer = new ComputeBuffer(1, INT_SIZE);

        zoneSpawnerData.kernelID_init = spawner.atlasCompute.FindKernel(zoneName + INIT_STRING);
        zoneSpawnerData.kernelID_update = spawner.atlasCompute.FindKernel(zoneName + UPDATE_STRING);
        zoneSpawnerData.kernelID_initSlice = spawner.atlasCompute.FindKernel(zoneName + INIT_SLICE_STRING);
        zoneSpawnerData.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[(int)area] / (float)THREADS_PER_GROUP);
        zoneSpawnerData.particleCount = PARTICLE_COUNTS[(int)area];

        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_init, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_initSlice, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, zoneName + DEAD_COUNT_STRING, deadCountBuffer);

        zoneSpawnerData.mpb.SetBuffer(materialIDs.ids.particles, outputBuffer);
        
        ChangeZone();
    }
    private void ChangeZone()
    {
        if (trip.ticketsCheckedSinceStart < curZone.ticketCheckStart || zoneSpawnerData.active) return;

        spawner.atlasCompute.SetInt(zoneName + SPRITE_COUNT_STRING, zoneInput.Length);
        spawner.atlasCompute.SetInt(zoneName + ACTIVE_STRING, 1);

        deadCounter = new uint[1];
        deadCountBuffer.SetData(deadCounter);
        spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_update, zoneName + DEAD_COUNT_STRING, deadCountBuffer);
        
        zoneSpawnerData.mpb.SetTexture(materialIDs.ids.atlasTexture, curZone.atlas.texture);

        zoneInput = new ZoneInput[curZone.zoneUVSizeAndPosArray.Length];
        inputBuffer = new ComputeBuffer(zoneInput.Length, ZONE_INPUT_STRIDE);
        switch (curZone.atlas.zoneType)
        {
            case ZoneSpriteType.Simple:
            {
                for (int i = 0; i < zoneInput.Length; i++)
                {
                    ZoneInput input = new ZoneInput();
                    input.worldSizeAndPivot = curZone.zoneWorldPivotsAndSizesArray[i];
                    input.uvSizeAndPos = curZone.zoneUVSizeAndPosArray[i];
                    zoneInput[i] = input;
                    spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_init, zoneName + INPUT_STRING, inputBuffer);
                    spawner.atlasCompute.Dispatch(zoneSpawnerData.kernelID_init, zoneSpawnerData.computeGroupSize, 1, 1);
                }
            }
            break;

            case ZoneSpriteType.Sliced:
            {
                for (int i = 0; i < zoneInput.Length; i++)
                {
                    ZoneInput input = new ZoneInput();
                    input.worldSizeAndPivot = curZone.zoneWorldPivotsAndSizesArray[i];
                    input.uvSizeAndPos = curZone.zoneUVSizeAndPosArray[i];
                    input.sliceOffsetAndSize = curZone.zoneSliceOffsetsAndSizes[i];
                    zoneInput[i] = input;
                    spawner.atlasCompute.SetBuffer(zoneSpawnerData.kernelID_initSlice, zoneName + INPUT_STRING, inputBuffer);
                    spawner.atlasCompute.Dispatch(zoneSpawnerData.kernelID_initSlice, zoneSpawnerData.computeGroupSize, 1, 1);
                }
            }
            break;
        }


        inputBuffer.SetData(zoneInput);
        zoneSpawnerData.active = true;
    }

    public void Dispose()
    {
        inputBuffer?.Release();
        outputBuffer?.Release();
        deadCountBuffer?.Release();
        zoneSpawnerData.active = false;
    }
}
