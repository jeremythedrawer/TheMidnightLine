using System;
using UnityEngine;
using static AtlasRendering;
using static AtlasSpawn;
using static Spy;
using static Train;
[ExecuteAlways]
public class SpawnMaster : MonoBehaviour
{
    public SpawnData spawnData;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    
    public int nextSpawnIndex;
    
    private void OnEnable()
    {
        InitBoundParameters();

        AtlasSpawn.InitMPBPool();

        InitZoneCompute();
        InitScrollCompute();
        
        InitParticles();
        ChangeParticles();
    }
    private void OnDisable()
    {
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

        UpdateInitComputeEditor();

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

        spawnData.zoneMoveInputs = new Vector2Int[ZONE_PARTICLE_COUNT];
        spawnData.zoneMoveInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(uint) * 2);

        spawnData.zoneDepthInputs = new Vector4[(int)ZONE_PARTICLE_COUNT];
        spawnData.zoneDepthInputBuffer = new ComputeBuffer((int)ZONE_PARTICLE_COUNT, sizeof(uint) * 4);


        spawnData.zoneOutputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.zoneComputeGroupSize = Mathf.CeilToInt((float)ZONE_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.zoneKernelInit = spawnData.zoneCompute.FindKernel("_ZoneInit");
        spawnData.zoneKernelUpdate = spawnData.zoneCompute.FindKernel("_ZoneUpdate");

        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelInit, "_ZoneOutput", spawnData.zoneOutputBuffer);

        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelInit, "_DepthInput", spawnData.zoneDepthInputBuffer);

        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelUpdate, "_ZoneOutput", spawnData.zoneOutputBuffer);
        spawnData.zoneCompute.SetBuffer(spawnData.zoneKernelUpdate, "_MoveInput", spawnData.zoneMoveInputBuffer);

        spawnData.zoneCompute.Dispatch(spawnData.zoneKernelInit, spawnData.zoneComputeGroupSize, 1, 1);
    }
    private void UpdateInitComputeEditor()
    {
        spawnData.zoneDepthInputs = new Vector4[(int)ZONE_PARTICLE_COUNT];
        int minParticle = 0;
        int maxParticle = 0;

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas  = trip.particleAtlasArray[i];

            for (int j = 0; j < particleAtlas.posData.Length; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];
                if (spyStats.ticketsCheckedTotal < posData.ticketCheckStart) break;
                maxParticle += (int)posData.particleCount;
                posData.mpb.SetInt("_ParticleOffset", (int)minParticle);

                for (int k = minParticle; k <= maxParticle; k++)
                {
                    spawnData.zoneDepthInputs[k] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, minParticle);
                }
                minParticle = maxParticle;
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

        spawnData.scrollMoveInputs = new uint[SCROLL_PARTICLE_COUNT];
        spawnData.scrollMoveInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint));

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
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            for(int j = 0; j < particleAtlas.posDataIndexOffset; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];
                if (posData.ticketCheckEnd < spyStats.ticketsCheckedTotal)
                {
                    ReturnMPB(posData.mpb);
                }
            }

            for (int j = particleAtlas.posDataIndexOffset; j < particleAtlas.posData.Length; j++)
            {
                ParticlePosData posData = particleAtlas.posData[j];

                if (spyStats.ticketsCheckedTotal < posData.ticketCheckStart)
                {
                    particleAtlas.posDataIndexOffset = j;
                    break;
                }
                posData.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
                posData.mpb = GetMPB();

                posData.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
                posData.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
                posData.mpb.SetInt("_SpriteCount", particleAtlas.spriteCount);

                if (particleAtlas.particleType == ParticleType.Zone)
                {
                    posData.mpb.SetBuffer("_Particles", spawnData.zoneOutputBuffer);
                }
                else
                {
                    posData.mpb.SetBuffer("_Particles", spawnData.scrollOutputBuffer);
                }

                particleAtlas.posData[j] = posData;
            }
        }
    }

    private void Dispose()
    {
        spawnData.scrollMoveInputBuffer?.Release();
        spawnData.zoneMoveInputBuffer?.Release();
        spawnData.scrollOutputBuffer?.Release();
        spawnData.zoneOutputBuffer?.Release();

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            for (int j = 0; j < particleAtlas.posData.Length; j++)
            {
                particleAtlas.posData[j].argsBuffer?.Release();
            }
        }

        spawnData.active = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnData.bounds.center, spawnData.bounds.size);
    }
}
