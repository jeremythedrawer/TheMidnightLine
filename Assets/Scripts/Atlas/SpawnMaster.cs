using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AtlasRendering;
using static AtlasSpawn;
using static Spy;
using static Train;
[ExecuteAlways]
public class SpawnMaster : MonoBehaviour
{
    const float DELAYED_PARTICLE_QUEUE_TICK = 1f;

    public SpawnData spawnData;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    
    public int nextSpawnIndex;
    public float delayParticleQueueClock;

    public Queue<DelayedParticleData> delayedParticlesQueue;
    private void OnEnable()
    {
        delayedParticlesQueue = new Queue<DelayedParticleData>();

        InitBoundParameters();

        AtlasSpawn.InitMPBPool();
        AtlasSpawn.InitArgsPool();

        InitZoneCompute();
        InitScrollCompute();
        
        InitParticles();
        ChangeParticles();
        gameEventData.OnTicketInspect.RegisterListener(ChangeParticles);

#if UNITY_EDITOR
        //EditorApplication.update += UpdateParticlesEditor;
#endif
    }
    private void OnDisable()
    {
        gameEventData.OnTicketInspect.UnregisterListener(ChangeParticles);
#if UNITY_EDITOR
       // EditorApplication.update -= UpdateParticlesEditor;
#endif
        Dispose();
    }
    private void Update()
    {
        UpdateSpawnCompute(ref spawnData.scrollData);
        UpdateSpawnCompute(ref spawnData.zoneData);
        //UpdateDelayedParticleQueue();
        //InitZoneCompute();
        //InitScrollCompute();
        //InitParticles();
        //ChangeParticles();
       // if (!Application.isPlaying) UpdateInitComputeEditor();
    }
    private void InitBoundParameters()
    {
        spawnData.bounds.center = new Vector3(TRAIN_WORLD_POS, 0, FAR_CLIP * 0.5f);
        spawnData.bounds.size = new Vector3(trip.stationsDataArray[0].station_prefab.frontPlatformRenderer.bounds.size.x + camStats.camBounds.size.x, trainStats.totalBounds.size.y + camStats.camBounds.size.y, FAR_CLIP);
        transform.position = spawnData.bounds.min;
    }
    private void UpdateSpawnCompute(ref SpawnComputeData computeData)
    {
        computeData.compute.SetFloat("_CamVelocity", (camStats.curVelocity.x * Time.deltaTime));
        if (spyStats.curLocationState != LocationState.Station)
        {
            computeData.compute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
        }
        computeData.compute.Dispatch(computeData.updateKernel, computeData.groupSize, 1, 1);
    }
    private void InitZoneCompute()
    {   
        spawnData.zoneData.compute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.zoneData.compute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.zoneData.compute.SetVector("_SpawnerSize", spawnData.bounds.size);
        spawnData.zoneData.compute.SetInt("_ParticleCount", ZONE_PARTICLE_COUNT);

        spawnData.zoneData.compute.SetInt("_Init", 0);

        spawnData.zoneData.moveInputs = new Vector2Int[ZONE_PARTICLE_COUNT];
        Array.Fill(spawnData.zoneData.moveInputs, Vector2Int.zero);

        spawnData.zoneData.moveInputBuffer?.Release();
        spawnData.zoneData.moveInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(uint) * 2);
        spawnData.zoneData.moveInputBuffer.SetData(spawnData.zoneData.moveInputs);

        spawnData.zoneData.depthInputs = new Vector4[(int)ZONE_PARTICLE_COUNT];
        spawnData.zoneData.depthInputBuffer?.Release();
        spawnData.zoneData.depthInputBuffer = new ComputeBuffer((int)ZONE_PARTICLE_COUNT, sizeof(uint) * 4);

        spawnData.zoneData.outputBuffer?.Release();
        spawnData.zoneData.outputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.zoneData.groupSize = Mathf.CeilToInt((float)ZONE_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.zoneData.initKernel = spawnData.zoneData.compute.FindKernel("_ZoneInit");
        spawnData.zoneData.updateKernel = spawnData.zoneData.compute.FindKernel("_ZoneUpdate");

        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_ZoneOutput", spawnData.zoneData.outputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_DepthInput", spawnData.zoneData.depthInputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_MoveInput", spawnData.zoneData.moveInputBuffer);

        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.updateKernel, "_ZoneOutput", spawnData.zoneData.outputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.updateKernel, "_MoveInput", spawnData.zoneData.moveInputBuffer);
    }
    private void InitScrollCompute()
    {
        spawnData.scrollData.compute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.scrollData.compute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.scrollData.compute.SetVector("_SpawnerSize", spawnData.bounds.size);

        spawnData.scrollData.compute.SetInt("_ParticleCount", SCROLL_PARTICLE_COUNT);

        spawnData.scrollData.moveInputs = new Vector2Int[SCROLL_PARTICLE_COUNT];
        Array.Fill(spawnData.scrollData.moveInputs, Vector2Int.zero);
        spawnData.scrollData.moveInputBuffer?.Release();
        spawnData.scrollData.moveInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint) * 2);
        spawnData.scrollData.moveInputBuffer.SetData(spawnData.scrollData.moveInputs);

        spawnData.scrollData.depthInputs = new Vector4[(int)SCROLL_PARTICLE_COUNT];
        spawnData.scrollData.depthInputBuffer?.Release();
        spawnData.scrollData.depthInputBuffer = new ComputeBuffer((int)SCROLL_PARTICLE_COUNT, sizeof(uint) * 4);

        spawnData.scrollData.outputBuffer?.Release();
        spawnData.scrollData.outputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.scrollData.groupSize = Mathf.CeilToInt((float)SCROLL_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.scrollData.initKernel = spawnData.scrollData.compute.FindKernel("_ScrollInit");
        spawnData.scrollData.updateKernel = spawnData.scrollData.compute.FindKernel("_ScrollUpdate");

        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_ScrollOutput", spawnData.scrollData.outputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_MoveInput", spawnData.scrollData.moveInputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_DepthInput", spawnData.scrollData.depthInputBuffer);

        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.updateKernel, "_ScrollOutput", spawnData.scrollData.outputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.updateKernel, "_MoveInput", spawnData.scrollData.moveInputBuffer);
    }
    private void InitParticles()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            particleAtlas.spriteDataBuffer?.Release();
            particleAtlas.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_PARTICLE_SPRITE_DATA_COUNT, PARTICLE_SPRITE_DATA_STRIDE);
            particleAtlas.spriteDataBuffer.SetData(particleAtlas.spriteData, 0, 0, particleAtlas.spriteData.Length);
            particleAtlas.posDataIndexOffset = 0;

            //for (int j = 0; j < particleAtlas.posData.Length; j++)
            //{
            //    ParticlePosData posData = particleAtlas.posData[j];
            //    posData.mpb = null;
            //    particleAtlas.posData[j] = posData;
            //}
        }

        spawnData.active = true;
    }
    private void ChangeParticles()
    {
        spawnData.zoneData.moveInputBuffer.GetData(spawnData.zoneData.moveInputs);
        spawnData.scrollData.moveInputBuffer.GetData(spawnData.scrollData.moveInputs);

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            switch(particleAtlas.particleType)
            {
                case ParticleType.Zone:
                {
                    SwapParticles(particleAtlas, ref spawnData.zoneData);
                }
                break;
                
                case ParticleType.Scroll:
                {
                    SwapParticles(particleAtlas, ref spawnData.scrollData);
                }
                break;
            }
        }

        ReinitSpawnCompute(ref spawnData.zoneData);
        ReinitSpawnCompute(ref spawnData.scrollData);
    }
    private void SwapParticles(ParticleAtlas particleAtlas, ref SpawnComputeData spawnComputeData)
    {
        for (int i = 0; i < particleAtlas.posDataIndexOffset; i++)
        {
            ParticlePosData posData = particleAtlas.posData[i];

            if (posData.ticketCheckEnd > spyStats.ticketsCheckedTotal) continue;

            if (!posData.isDying)
            {
                for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
                {
                    Vector2Int newMoveInput = spawnComputeData.moveInputs[j];
                    newMoveInput.x = 0;
                    spawnComputeData.moveInputs[j] = newMoveInput;
                }
                posData.isDying = true;
                particleAtlas.posData[i] = posData;
            }
            else
            {
                for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                {
                    if (spawnComputeData.moveInputs[k].y == 1) break;
                }

                ReturnMPB(posData.mpb);

                if (particleAtlas.posDataIndexOffset == particleAtlas.posData.Length) particleAtlas.isCompleted = true;
            }
        }

        if (particleAtlas.posDataIndexOffset == particleAtlas.posData.Length) return;

        int newOffset = particleAtlas.posDataIndexOffset;
        for (int j = particleAtlas.posDataIndexOffset; j < particleAtlas.posData.Length; j++)
        {
            ParticlePosData posData = particleAtlas.posData[j];

            if (spyStats.ticketsCheckedTotal < posData.ticketCheckStart)
            {
                newOffset = j;
                break;
            }

            bool particlesAvailable = true;

            for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
            {
                if (spawnComputeData.moveInputs[k] != Vector2Int.zero)
                {
                    particlesAvailable = false;
                    break;
                }
            }

            if (!particlesAvailable)
            {
                DelayedParticleData delayedData = new DelayedParticleData()
                { 
                    posData = posData,
                    particleAtlas = particleAtlas,
                    index = j,
                    spawnComputeData = spawnComputeData,
                };

                delayedParticlesQueue.Enqueue(delayedData);
                continue;
            }

            posData.argsBuffer = GetArgsBuffer();
            posData.mpb = GetMPB();

            posData.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
            posData.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
            posData.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);
            posData.mpb.SetBuffer("_Particles", spawnComputeData.outputBuffer);

            posData.mpb.SetInt("_SpriteCount", posData.spritesPerParticle);
            posData.mpb.SetInt("_SpriteIndex", posData.spriteIndex);

            for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
            {
                spawnComputeData.depthInputs[k] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);
                spawnComputeData.moveInputs[k] = new Vector2Int(1, 0);
            }

            particleAtlas.posData[j] = posData;

            if (j == particleAtlas.posData.Length - 1)
            {
                newOffset = j + 1;
            }
        }

        particleAtlas.posDataIndexOffset = newOffset;
    }
    private void ReinitSpawnCompute(ref SpawnComputeData spawnComputeData)
    {
        spawnComputeData.depthInputBuffer.SetData(spawnComputeData.depthInputs);
        spawnComputeData.moveInputBuffer.SetData(spawnComputeData.moveInputs);
        spawnComputeData.compute.Dispatch(spawnComputeData.initKernel, spawnComputeData.groupSize, 1, 1);
        spawnComputeData.compute.SetInt("_Init", 1);
    }
    private void UpdateDelayedParticleQueue()
    {
        if (delayedParticlesQueue.Count == 0) return;

        delayParticleQueueClock += Time.deltaTime;

        if (delayParticleQueueClock < DELAYED_PARTICLE_QUEUE_TICK) return;

        DelayedParticleData delayedParticleData = delayedParticlesQueue.Peek();

        delayedParticleData.spawnComputeData.moveInputBuffer.GetData(spawnData.zoneData.moveInputs);

        for (int i = delayedParticleData.posData.minParticleIndex; i <= delayedParticleData.posData.maxParticleIndex; i++)
        {
            if (delayedParticleData.spawnComputeData.moveInputs[i] != Vector2Int.zero)
            {
                delayParticleQueueClock = 0;
                return;
            }
        }

        delayedParticleData.spawnComputeData.depthInputBuffer.GetData(spawnData.zoneData.depthInputs);

        delayedParticleData.posData.argsBuffer = GetArgsBuffer();
        delayedParticleData.posData.mpb = GetMPB();

        delayedParticleData.posData.mpb.SetTexture("_AtlasTexture", delayedParticleData.particleAtlas.atlas.texture);
        delayedParticleData.posData.mpb.SetBuffer("_SpriteData", delayedParticleData.particleAtlas.spriteDataBuffer);
        delayedParticleData.posData.mpb.SetInt("_SpriteCount", delayedParticleData.particleAtlas.spriteCount);
        delayedParticleData.posData.mpb.SetInt("_ParticleOffset", delayedParticleData.posData.minParticleIndex);

        delayedParticleData.posData.mpb.SetBuffer("_Particles", delayedParticleData.spawnComputeData.outputBuffer);

        for (int k = delayedParticleData.posData.minParticleIndex; k <= delayedParticleData.posData.maxParticleIndex; k++)
        {
            delayedParticleData.spawnComputeData.depthInputs[k] = new Vector4(delayedParticleData.posData.depth, delayedParticleData.posData.particleCount, delayedParticleData.posData.depthSize, delayedParticleData.posData.minParticleIndex);

            delayedParticleData.spawnComputeData.moveInputs[k] = new Vector2Int(1, 0);
        }

        delayedParticleData.particleAtlas.posData[delayedParticleData.index] = delayedParticleData.posData;

        delayedParticleData.spawnComputeData.moveInputBuffer.SetData(delayedParticleData.spawnComputeData.moveInputs);
        delayedParticleData.spawnComputeData.depthInputBuffer.SetData(delayedParticleData.spawnComputeData.depthInputs);
        delayedParticleData.spawnComputeData.compute.Dispatch(delayedParticleData.spawnComputeData.initKernel, delayedParticleData.spawnComputeData.groupSize, 1, 1);

        delayedParticlesQueue.Dequeue();
        delayParticleQueueClock = 0;
    }
    private void Dispose()
    {
        DisposeCompute(ref spawnData.zoneData);
        DisposeCompute(ref spawnData.scrollData);

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            for (int j = 0; j < particleAtlas.posData.Length; j++)
            {
                particleAtlas.posData[j].argsBuffer?.Release();
                particleAtlas.posData[j].isDying = false;
            }

            particleAtlas.isCompleted = false;
            particleAtlas.posDataIndexOffset = 0;
        }

        spawnData.active = false;

    }
    private void DisposeCompute(ref SpawnComputeData spawnComputeData)
    {
        spawnComputeData.compute.SetFloat("_TrainVelocity", 0);
        spawnComputeData.moveInputBuffer?.Release();
        spawnComputeData.depthInputBuffer?.Release();
        spawnComputeData.outputBuffer?.Release();
    }
#if UNITY_EDITOR
    private void UpdateParticlesEditor()
    {
        Debug.Log("Updating particles");
        UpdateSpawnCompute(ref spawnData.scrollData);
        UpdateSpawnCompute(ref spawnData.zoneData);

        InitZoneCompute();
        InitScrollCompute();
        InitParticles();
        ChangeParticles();
        SceneView.RepaintAll();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnData.bounds.center, spawnData.bounds.size);
    }
#endif
}
