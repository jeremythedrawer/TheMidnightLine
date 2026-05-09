using System;
using System.Collections.Generic;
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

    public Queue<(ParticlePosData posData, ParticleAtlas atlas, int atlasIndex)> delayedParticlesQueue;
    private void OnEnable()
    {
        delayedParticlesQueue = new Queue<(ParticlePosData posData, ParticleAtlas atlas, int atlasIndex)>();

        InitBoundParameters();

        AtlasSpawn.InitMPBPool();

        InitZoneCompute();
        InitScrollCompute();
        
        InitParticles();
        ChangeParticles();


        gameEventData.OnTicketInspect.RegisterListener(ChangeParticles);
    }
    private void OnDisable()
    {
        gameEventData.OnTicketInspect.UnregisterListener(ChangeParticles);
        Dispose();
    }
    private void Update()
    {
        spawnData.zoneCompute.SetFloat("_CamVelocity", camStats.curVelocity.x);
        spawnData.scrollCompute.SetFloat("_CamVelocity", camStats.curVelocity.x);
        if (spyStats.curLocationState != LocationState.Station)
        {
            spawnData.zoneCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
            spawnData.scrollCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
        }

        spawnData.scrollCompute.Dispatch(spawnData.scrollKernelUpdate, spawnData.scrollComputeGroupSize, 1, 1);
        spawnData.zoneCompute.Dispatch(spawnData.zoneKernelUpdate, spawnData.zoneComputeGroupSize, 1, 1);

       UpdateDelayedParticleQueue();

#if UNITY_EDITOR
       // if (!Application.isPlaying) UpdateInitComputeEditor();
#endif

    }
    private void InitBoundParameters()
    {
        spawnData.bounds.center = new Vector3(TRAIN_WORLD_POS, 0, FAR_CLIP * 0.5f);
        spawnData.bounds.size = new Vector3(trip.stationsDataArray[0].station_prefab.frontPlatformRenderer.bounds.size.x + camStats.camBounds.size.x, trainStats.totalBounds.size.y + camStats.camBounds.size.y, FAR_CLIP);
        transform.position = spawnData.bounds.min;
    }
    private void InitZoneCompute()
    {   
        spawnData.zoneCompute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.zoneCompute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.zoneCompute.SetVector("_SpawnerSize", spawnData.bounds.size);
        spawnData.zoneCompute.SetInt("_ParticleCount", ZONE_PARTICLE_COUNT);
        
        spawnData.zoneCompute.SetInt("_Init", 0);

        spawnData.zoneMoveInputs = new Vector2Int[ZONE_PARTICLE_COUNT];
        Array.Fill(spawnData.zoneMoveInputs, Vector2Int.zero);
        spawnData.zoneMoveInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(uint) * 2);
        spawnData.zoneMoveInputBuffer.SetData(spawnData.zoneMoveInputs);

        spawnData.zoneDepthInputs = new Vector4[(int)ZONE_PARTICLE_COUNT];
        spawnData.zoneDepthInputBuffer = new ComputeBuffer((int)ZONE_PARTICLE_COUNT, sizeof(uint) * 4);


        spawnData.zoneOutputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.zoneComputeGroupSize = Mathf.CeilToInt((float)ZONE_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.zoneKernelInit = spawnData.zoneCompute.FindKernel("_ZoneInit");
        spawnData.zoneKernelUpdate = spawnData.zoneCompute.FindKernel("_ZoneUpdate");

        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelInit, "_ZoneOutput", spawnData.zoneOutputBuffer);
        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelInit, "_DepthInput", spawnData.zoneDepthInputBuffer);
        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelInit, "_MoveInput", spawnData.zoneMoveInputBuffer);

        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelUpdate, "_ZoneOutput", spawnData.zoneOutputBuffer);
        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelUpdate, "_MoveInput", spawnData.zoneMoveInputBuffer);
    }
    private void UpdateInitComputeEditor()
    {
        spawnData.zoneDepthInputs = new Vector4[(int)ZONE_PARTICLE_COUNT];

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas  = trip.particleAtlasArray[i];

            for (int j = 0; j < particleAtlas.posData.Length; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];
                if (spyStats.ticketsCheckedTotal < posData.ticketCheckStart) break;
                posData.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);

                for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                {
                    spawnData.zoneDepthInputs[k] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);
                }
            }
        }

        spawnData.zoneDepthInputBuffer.SetData(spawnData.zoneDepthInputs);
        spawnData.zoneCompute.Dispatch(spawnData.zoneKernelInit, spawnData.zoneComputeGroupSize, 1, 1);
    }
    private void InitScrollCompute()
    {
        spawnData.scrollCompute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.scrollCompute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.scrollCompute.SetVector("_SpawnerSize", spawnData.bounds.size);
        spawnData.scrollCompute.SetInt("_ParticleCount", SCROLL_PARTICLE_COUNT);

        spawnData.scrollMoveInputs = new Vector2Int[SCROLL_PARTICLE_COUNT];
        spawnData.scrollMoveInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint) * 2);

        spawnData.scrollOutputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.scrollComputeGroupSize = Mathf.CeilToInt((float)SCROLL_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.scrollKernelInit = spawnData.scrollCompute.FindKernel("_ScrollInit");
        spawnData.scrollKernelUpdate = spawnData.scrollCompute.FindKernel("_ScrollUpdate");

        spawnData.scrollCompute.SetBuffer(spawnData.scrollKernelInit, "_ScrollOutput", spawnData.scrollOutputBuffer);

        spawnData.scrollCompute.SetBuffer(spawnData.scrollKernelUpdate, "_ScrollOutput", spawnData.scrollOutputBuffer);
        spawnData.scrollCompute.SetBuffer(spawnData.scrollKernelUpdate, "_MoveInput", spawnData.scrollMoveInputBuffer);

        spawnData.scrollCompute.Dispatch(spawnData.scrollKernelInit, spawnData.scrollComputeGroupSize, 1, 1);
    }
    private void InitParticles()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            particleAtlas.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_PARTICLE_SPRITE_DATA_COUNT, PARTICLE_SPRITE_DATA_STRIDE);
            particleAtlas.spriteDataBuffer.SetData(particleAtlas.spriteData, 0, 0, particleAtlas.spriteCount);
            particleAtlas.posDataIndexOffset = 0;

            for (int j = 0; j < particleAtlas.posDataIndexOffset; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];
                posData.mpb = null;
                particleAtlas.posData[j] = posData;
            }
        }

        spawnData.active = true;
    }
    private void ChangeParticles()
    {
        spawnData.zoneMoveInputBuffer.GetData(spawnData.zoneMoveInputs);

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            for(int j = 0; j < particleAtlas.posDataIndexOffset; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];

                if (posData.ticketCheckEnd > spyStats.ticketsCheckedTotal) continue;

                if (!posData.isDying)
                {
                    for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                    {
                        spawnData.zoneMoveInputs[k].x = 0;
                    }
                    posData.isDying = true;
                    particleAtlas.posData[j] = posData;

                }
                else
                {
                    for(int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                    {
                        if (spawnData.zoneMoveInputs[k].y == 1) break;
                    }

                    ReturnMPB(posData.mpb);
                    if (particleAtlas.posDataIndexOffset == particleAtlas.posData.Length)
                    {
                        particleAtlas.isCompleted = true;
                    }
                }
            }

            if (particleAtlas.posDataIndexOffset != particleAtlas.posData.Length)
            {
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
                        if (spawnData.zoneMoveInputs[k] != Vector2Int.zero)
                        {
                            particlesAvailable = false;
                            break;
                        }
                    }

                    if (!particlesAvailable)
                    {
                        delayedParticlesQueue.Enqueue((posData, particleAtlas, j));
                        continue;
                    }

                    posData.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
                    posData.mpb = GetMPB();

                    posData.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
                    posData.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
                    posData.mpb.SetInt("_SpriteCount", particleAtlas.spriteCount);
                    posData.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);

                    if (particleAtlas.particleType == ParticleType.Zone)
                    {
                        posData.mpb.SetBuffer("_Particles", spawnData.zoneOutputBuffer);
                    }
                    else
                    {
                        posData.mpb.SetBuffer("_Particles", spawnData.scrollOutputBuffer);
                    }
                    for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                    {
                        spawnData.zoneDepthInputs[k] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);
                        spawnData.zoneMoveInputs[k] = new Vector2Int(1, 0);
                    }

                    particleAtlas.posData[j] = posData;

                    if (j == particleAtlas.posData.Length - 1)
                    {
                        newOffset = j + 1;
                    }
                }
                particleAtlas.posDataIndexOffset = newOffset;
            }
        }

        spawnData.zoneDepthInputBuffer.SetData(spawnData.zoneDepthInputs);
        spawnData.zoneMoveInputBuffer.SetData(spawnData.zoneMoveInputs);
        spawnData.zoneCompute.Dispatch(spawnData.zoneKernelInit, spawnData.zoneComputeGroupSize, 1, 1);
        spawnData.zoneCompute.SetInt("_Init", 1);
    }


    private void UpdateDelayedParticleQueue()
    {
        if (delayedParticlesQueue.Count == 0) return;

        delayParticleQueueClock += Time.deltaTime;

        if (delayParticleQueueClock < DELAYED_PARTICLE_QUEUE_TICK) return;

        (ParticlePosData posData, ParticleAtlas atlas, int atlasIndex) posDataAndAtlas = delayedParticlesQueue.Peek();
        ParticlePosData posData = posDataAndAtlas.posData;
        ParticleAtlas particleAtlas = posDataAndAtlas.atlas;
        int atlasIndex = posDataAndAtlas.atlasIndex;

        spawnData.zoneMoveInputBuffer.GetData(spawnData.zoneMoveInputs);
        spawnData.zoneDepthInputBuffer.GetData(spawnData.zoneDepthInputs);

        for (int i = posData.minParticleIndex; i <= posData.maxParticleIndex; i++)
        {
            if (spawnData.zoneMoveInputs[i] != Vector2Int.zero)
            {
                delayParticleQueueClock = 0;
                return;
            }
        }

        posData.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
        posData.mpb = GetMPB();

        posData.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
        posData.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
        posData.mpb.SetInt("_SpriteCount", particleAtlas.spriteCount);
        posData.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);

        if (particleAtlas.particleType == ParticleType.Zone)
        {
            posData.mpb.SetBuffer("_Particles", spawnData.zoneOutputBuffer);
        }
        else
        {
            posData.mpb.SetBuffer("_Particles", spawnData.scrollOutputBuffer);
        }
        for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
        {
            spawnData.zoneDepthInputs[k] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);
            spawnData.zoneMoveInputs[k] = new Vector2Int(1, 0);
        }

        particleAtlas.posData[atlasIndex] = posData;


        spawnData.zoneMoveInputBuffer.SetData(spawnData.zoneMoveInputs);
        spawnData.zoneDepthInputBuffer.SetData(spawnData.zoneDepthInputs);
        spawnData.zoneCompute.Dispatch(spawnData.zoneKernelInit, spawnData.zoneComputeGroupSize, 1, 1);

        delayedParticlesQueue.Dequeue();
        delayParticleQueueClock = 0;
    }
    private void Dispose()
    {
        spawnData.zoneMoveInputBuffer?.Release();
        spawnData.zoneDepthInputBuffer?.Release();
        spawnData.zoneOutputBuffer?.Release();

        spawnData.scrollMoveInputBuffer?.Release();
        spawnData.scrollOutputBuffer?.Release();


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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnData.bounds.center, spawnData.bounds.size);
    }
}
