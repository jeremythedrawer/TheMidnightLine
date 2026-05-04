using UnityEngine;
using static AtlasSpawn;
using static Atlas;

[ExecuteAlways]
public class ZoneSpawner : MonoBehaviour
{
    public ZoneLabel area;

    public SpawnSO spawner;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;

    [Header("Generated")]
    public ZoneAreaSO zoneArea;
    public ZoneAtlas curZoneAtlas;
    public int curZoneIndex;
    public uint[] deadCounter;

    public ComputeBuffer outputBuffer;
    public ComputeBuffer inputBuffer;
    public ComputeBuffer deadCountBuffer;

    public ZoneInput[] zoneInput;
    public string zoneName;
    public int curInitKernelID;
    private void OnDisable()
    {
        Dispose();
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    public void ChooseState()
    {
        if (spyStats.ticketsCheckedTotal >= curZoneAtlas.ticketCheckEnd)
        {
            if (curZoneAtlas.ticketCheckEnd == 0 || deadCounter[0] == zoneArea.particleCount)
            {
                SetState(ZoneState.Dead);
            }
            else
            {
                SetState(ZoneState.Dying);
            }
        }
        else if (spyStats.ticketsCheckedTotal >= curZoneAtlas.ticketCheckStart)
        {
            SetState(ZoneState.Alive);
        }
    }
    public void SetState(ZoneState newState)
    {
        if (newState == zoneArea.state) return;
        ExitState();
        zoneArea.state = newState;
        EnterState();

    }
    public void UpdateState()
    {
        switch(zoneArea.state)
        {
            case ZoneState.Alive:
            {
                spawner.zoneCompute.Dispatch(zoneArea.kernelID_update, zoneArea.computeGroupSize, 1, 1);
            }
            break;
            case ZoneState.Dying:
            {
                deadCountBuffer.GetData(deadCounter);
                spawner.zoneCompute.Dispatch(zoneArea.kernelID_update, zoneArea.computeGroupSize, 1, 1);
            }
            break;
        }
    }
    public void EnterState()
    {
        switch(zoneArea.state)
        {
            case ZoneState.Alive:
            {
                
            }
            break;
            case ZoneState.Dying:
            {
                spawner.zoneCompute.SetInt(zoneName + ACTIVE_STRING, 0);
            }
            break;
            case ZoneState.Dead:
            {
                if (curZoneIndex + 1 < zoneArea.zoneAtlases.Length)
                {
                    ChangeZone();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            break;
        }
    }
    public void ExitState()
    {

    }
    public void InitializeZoneSpawnData()
    {
        SetState(ZoneState.None);
        zoneName = ZONE_STRINGS[(int)area];
        zoneArea = trip.zoneAreas[(int)area];
        if (zoneArea.zoneAtlases.Length == 0) return;

        zoneArea.mpb = new MaterialPropertyBlock();
        outputBuffer = new ComputeBuffer(PARTICLE_COUNTS[(int)area], ZONE_OUTPUT_STRIDE);
        deadCountBuffer = new ComputeBuffer(1, INT_SIZE);

        zoneArea.kernelID_init = spawner.zoneCompute.FindKernel(zoneName + INIT_STRING);
        zoneArea.kernelID_update = spawner.zoneCompute.FindKernel(zoneName + UPDATE_STRING);
        zoneArea.kernelID_initSlice = spawner.zoneCompute.FindKernel(zoneName + INIT_SLICE_STRING);
        zoneArea.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[(int)area] / (float)THREADS_PER_GROUP);
        zoneArea.particleCount = PARTICLE_COUNTS[(int)area];

        spawner.zoneCompute.SetBuffer(zoneArea.kernelID_init, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.zoneCompute.SetBuffer(zoneArea.kernelID_initSlice, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.zoneCompute.SetBuffer(zoneArea.kernelID_update, zoneName + OUTPUT_STRING, outputBuffer);
        spawner.zoneCompute.SetBuffer(zoneArea.kernelID_update, zoneName + DEAD_COUNT_STRING, deadCountBuffer);

        zoneArea.mpb.SetBuffer("_Particles", outputBuffer);

        curZoneIndex = -1;
        SetState(ZoneState.Dead);
    }
    public void ChangeZone()
    {
        curZoneIndex++;
        curZoneAtlas = zoneArea.zoneAtlases[curZoneIndex];
        spawner.zoneCompute.SetInt(zoneName + SPRITE_COUNT_STRING, zoneInput.Length);
        spawner.zoneCompute.SetInt(zoneName + ACTIVE_STRING, 1);

        deadCounter[0] = 0;

        deadCountBuffer.SetData(deadCounter);
        spawner.zoneCompute.SetBuffer(zoneArea.kernelID_update, zoneName + DEAD_COUNT_STRING, deadCountBuffer);
        
        zoneArea.mpb.SetTexture("_AtlasTexture", curZoneAtlas.atlas.texture);

        zoneInput = new ZoneInput[curZoneAtlas.uvSizeAndPosArray.Length];
        inputBuffer = new ComputeBuffer(zoneInput.Length, ZONE_INPUT_STRIDE);
        switch (curZoneAtlas.zoneType)
        {
            case ZoneSpriteType.Simple:
            {
                for (int i = 0; i < zoneInput.Length; i++)
                {
                    ZoneInput input = new ZoneInput();
                    input.worldPivotAndSize = curZoneAtlas.worldPivotAndSizeArray[i];
                    input.uvSizeAndPos = curZoneAtlas.uvSizeAndPosArray[i];
                    zoneInput[i] = input;
                }
                curInitKernelID = zoneArea.kernelID_init;
            }
            break;

            case ZoneSpriteType.Sliced:
            {
                for (int i = 0; i < zoneInput.Length; i++)
                {
                    ZoneInput input = new ZoneInput();
                    input.worldPivotAndSize = curZoneAtlas.worldPivotAndSizeArray[i];
                    input.uvSizeAndPos = curZoneAtlas.uvSizeAndPosArray[i];
                    input.sliceOffsetAndSize = curZoneAtlas.sliceOffsetsAndSizes[i];
                    zoneInput[i] = input;
                }
                curInitKernelID = zoneArea.kernelID_initSlice;
            }
            break;
        }


        inputBuffer.SetData(zoneInput);
        spawner.zoneCompute.SetBuffer(curInitKernelID, zoneName + INPUT_STRING, inputBuffer);
        spawner.zoneCompute.Dispatch(curInitKernelID, zoneArea.computeGroupSize, 1, 1);
    }
    public void Dispose()
    {
        inputBuffer?.Release();
        outputBuffer?.Release();
        deadCountBuffer?.Release();
    }
}
