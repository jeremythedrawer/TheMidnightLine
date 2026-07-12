using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using static AtlasSpawn;
using static Spy;
using static Train;
[ExecuteAlways]
public class SpawnMaster : MonoBehaviour
{
    const float DELAYED_PARTICLE_QUEUE_TICK = 1f;
    const float DAY_NIGHT_TRANSITION_TIME = 5;

    public SpawnData spawnData;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public ColorsSO colorSO;

    [Header("Generated")]
    public int nextSpawnIndex;
    public float delayParticleQueueClock;
    public float curDayNight;
    public CancellationTokenSource ctsDayNight;
    public Queue<DelayedParticleData> delayedParticlesQueue;
    private void OnEnable()
    {
#if UNITY_EDITOR
        Init();
#endif
        SpyBrain.OnTicketInspect += ChangeParticles;
        gameEventData.OnMetersAtSpawnBounds.RegisterListener(DespawnEdgeScrollers);
        Scenes.OnLoadTrip2 += Init;
    }
    private void OnDisable()
    {
        SpyBrain.OnTicketInspect -= ChangeParticles;
        gameEventData.OnMetersAtSpawnBounds.UnregisterListener(DespawnEdgeScrollers);
        Scenes.OnLoadTrip2 -= Init;
        Dispose();
    }
    private void Update()
    {
        UpdateSpawnCompute(ref spawnData.scrollData);
        UpdateSpawnCompute(ref spawnData.zoneData);
        UpdateDelayedParticleQueue();

    }
    private void Init()
    {
        Dispose();
        delayedParticlesQueue = new Queue<DelayedParticleData>();

        InitBoundParameters();

        AtlasSpawn.InitMPBPool();
        AtlasSpawn.InitArgsPool();
        AtlasSpawn.InitQuadScalePool();
        AtlasSpawn.InitEdgeSpritePool();

        InitZoneCompute();
        InitScrollCompute();

        InitParticles();
        ChangeParticles();
    }
    private void InitBoundParameters()
    {
        spawnData.bounds.center = new Vector3(TRAIN_WORLD_POS_X, 0, FAR_CLIP * 0.5f);
        spawnData.bounds.size = new Vector3(trip.stationsDataArray[0].station_prefab.frontPlatformRenderer.bounds.size.x + camStats.camBounds.size.x, trainStats.totalBounds.size.y + camStats.camBounds.size.y, FAR_CLIP);
        transform.position = spawnData.bounds.min;

    }
    private void UpdateSpawnCompute(ref SpawnComputeData computeData)
    {
        computeData.compute.SetVector("_CamVelocity", camStats.curVelocity);
        if (spyStats.curLocationState != LocationState.Station)
        {
            computeData.compute.SetVector("_TrainVelocity", trainStats.curVelocity);
        }
        computeData.compute.SetFloat("_DeltaTime", Time.deltaTime);
        computeData.compute.Dispatch(computeData.updateKernel, computeData.groupSize, 1, 1);
    }
    private void InitZoneCompute()
    {   
        spawnData.zoneData.compute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.zoneData.compute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.zoneData.compute.SetVector("_SpawnerSize", spawnData.bounds.size);
        spawnData.zoneData.compute.SetInt("_ParticleCount", ZONE_PARTICLE_COUNT);

        spawnData.zoneData.compute.SetInt("_Init", 0);

        spawnData.zoneData.moveInputs = new uint[ZONE_PARTICLE_COUNT];
        Array.Fill(spawnData.zoneData.moveInputs, (uint)0);
        spawnData.zoneData.moveInputBuffer?.Release();
        spawnData.zoneData.moveInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(uint));
        spawnData.zoneData.moveInputBuffer.SetData(spawnData.zoneData.moveInputs);

        spawnData.zoneData.depthInputs = new Vector4[ZONE_PARTICLE_COUNT];
        spawnData.zoneData.depthInputBuffer?.Release();
        spawnData.zoneData.depthInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(uint) * 4);

        spawnData.zoneData.offsetInputs = new Vector4[ZONE_PARTICLE_COUNT];
        spawnData.zoneData.offsetInputBuffer?.Release();
        spawnData.zoneData.offsetInputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.zoneData.outputBuffer?.Release();
        spawnData.zoneData.outputBuffer = new ComputeBuffer(ZONE_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.zoneData.groupSize = Mathf.CeilToInt((float)ZONE_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.zoneData.initKernel = spawnData.zoneData.compute.FindKernel("_ZoneInit");
        spawnData.zoneData.updateKernel = spawnData.zoneData.compute.FindKernel("_ZoneUpdate");

        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_ZoneOutput", spawnData.zoneData.outputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_DepthInput", spawnData.zoneData.depthInputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_MoveInput", spawnData.zoneData.moveInputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.initKernel, "_OffsetInput", spawnData.zoneData.offsetInputBuffer);

        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.updateKernel, "_ZoneOutput", spawnData.zoneData.outputBuffer);
        spawnData.zoneData.compute.SetBuffer(spawnData.zoneData.updateKernel, "_MoveInput", spawnData.zoneData.moveInputBuffer);
    }
    private void InitScrollCompute()
    {
        spawnData.scrollData.compute.SetVector("_SpawnerMinPos", spawnData.bounds.min);
        spawnData.scrollData.compute.SetVector("_SpawnerMaxPos", spawnData.bounds.max);
        spawnData.scrollData.compute.SetVector("_SpawnerSize", spawnData.bounds.size);
        spawnData.scrollData.compute.SetInt("_ParticleCount", SCROLL_PARTICLE_COUNT);

        spawnData.scrollData.compute.SetInt("_Init", 0);

        spawnData.scrollData.moveInputs = new uint[SCROLL_PARTICLE_COUNT];
        Array.Fill(spawnData.scrollData.moveInputs, (uint)0);
        spawnData.scrollData.moveInputBuffer?.Release();
        spawnData.scrollData.moveInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint));
        spawnData.scrollData.moveInputBuffer.SetData(spawnData.scrollData.moveInputs);

        spawnData.scrollData.depthInputs = new Vector4[SCROLL_PARTICLE_COUNT];
        spawnData.scrollData.depthInputBuffer?.Release();
        spawnData.scrollData.depthInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint) * 4);

        spawnData.scrollData.offsetInputs = new Vector4[SCROLL_PARTICLE_COUNT];
        spawnData.scrollData.offsetInputBuffer?.Release();
        spawnData.scrollData.offsetInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.scrollData.prevIndicesInputs = new Vector2Int[SCROLL_PARTICLE_COUNT];
        spawnData.scrollData.prevIndicesInputsBuffer?.Release();
        spawnData.scrollData.prevIndicesInputsBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint) * 2);
        

        spawnData.scrollData.outputBuffer?.Release();
        spawnData.scrollData.outputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(float) * 4);

        spawnData.scrollData.groupSize = Mathf.CeilToInt((float)SCROLL_PARTICLE_COUNT / THREADS_PER_GROUP);

        spawnData.scrollData.initKernel = spawnData.scrollData.compute.FindKernel("_ScrollInit");
        spawnData.scrollData.updateKernel = spawnData.scrollData.compute.FindKernel("_ScrollUpdate");

        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_ScrollOutput", spawnData.scrollData.outputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_MoveInput", spawnData.scrollData.moveInputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_DepthInput", spawnData.scrollData.depthInputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_OffsetInput", spawnData.scrollData.offsetInputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.initKernel, "_PrevIndicesInput", spawnData.scrollData.prevIndicesInputsBuffer);

        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.updateKernel, "_ScrollOutput", spawnData.scrollData.outputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.updateKernel, "_MoveInput", spawnData.scrollData.moveInputBuffer);
        spawnData.scrollData.compute.SetBuffer(spawnData.scrollData.updateKernel, "_OffsetInput", spawnData.scrollData.offsetInputBuffer);
    }
    private void InitParticles()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            particleAtlas.spriteDataBuffer?.Release();

            particleAtlas.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleAtlas.spriteData.Length, PARTICLE_SPRITE_DATA_STRIDE);
            particleAtlas.spriteDataBuffer.SetData(particleAtlas.spriteData);

            particleAtlas.posDataIndexOffset = 0;
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

        UpdateSky();
    }
    private void SwapParticles(ParticleAtlas particleAtlas, ref SpawnComputeData spawnComputeData)
    {
        for (int i = 0; i < particleAtlas.posDataIndexOffset; i++)
        {
            ParticlePosData posData = particleAtlas.posData[i];
            if (posData.ticketCheckEnd > trip.ticketsCheckedTotal) continue;

            if (posData.spawnState != SpawnState.MovingOut)
            {
                if (posData.postScrollers != null)
                {
                    int particleIndex = (spawnComputeData.moveInputs[posData.maxParticleIndex] & (int)ParticleMoveInputs.PostAtMinBit) != 0 ? posData.minParticleIndex : posData.maxParticleIndex;
                    for (int j = 0; j < posData.postScrollers.Length; j++)
                    {
                        EdgeScroller postScroller = posData.postScrollers[j];

                        postScroller.mpb = GetMPB();
                        postScroller.argsBuffer = GetArgsBuffer();
                        postScroller.edgeSpriteDataBuffer = GetEdgeSpriteBuffer();
                        postScroller.edgeSpriteDataBuffer.SetData(postScroller.spriteData);

                        postScroller.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);

                        postScroller.mpb.SetBuffer("_Particles", spawnComputeData.outputBuffer);
                        postScroller.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
                        postScroller.mpb.SetBuffer("_EdgeSpriteData", postScroller.edgeSpriteDataBuffer);

                        postScroller.mpb.SetInt("_ParticleOffset", particleIndex);

                        posData.postScrollers[j] = postScroller;
                    }
                }

                for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
                {
                    uint newMoveInput = spawnComputeData.moveInputs[j];
                    newMoveInput |= (int)ParticleMoveInputs.Dying;
                    spawnComputeData.moveInputs[j] = newMoveInput;
                }
                posData.spawnState = SpawnState.MovingOut;
                particleAtlas.posData[i] = posData;
            }
            else
            {
                bool isDead = true;
                for (int k = posData.minParticleIndex; k <= posData.maxParticleIndex; k++)
                {
                    if ((spawnComputeData.moveInputs[k] & (uint)ParticleMoveInputs.Dead) == 0)
                    {
                        isDead = false;
                        break;
                    }
                }

                if (!isDead) continue;

                if (posData.mpb != null)
                {
                    ReturnMPB(posData.mpb);
                    posData.mpb = null;

                }
                if (posData.quadScaleBuffer != null)
                {
                    ReturnQuadScaleBuffer(posData.quadScaleBuffer);
                    posData.quadScaleBuffer = null;
                }

                if (i == particleAtlas.posDataIndexOffset - 1 && particleAtlas.posDataIndexOffset == particleAtlas.posData.Length) particleAtlas.isCompleted = true;
            }

            particleAtlas.posData[i] = posData;
        }

        if (particleAtlas.posDataIndexOffset == particleAtlas.posData.Length) return;

        int newOffset = particleAtlas.posDataIndexOffset;

        for (int i = particleAtlas.posDataIndexOffset; i < particleAtlas.posData.Length; i++)
        {
            ParticlePosData posData = particleAtlas.posData[i];

            if (trip.ticketsCheckedTotal < posData.ticketCheckStart)
            {
                newOffset = i;
                break;
            }

            bool particlesAvailable = true;

            for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
            {
                uint moveInputs = spawnComputeData.moveInputs[j];
                if ((moveInputs & (uint)ParticleMoveInputs.Born) != 0)
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
                    index = i,
                    spawnComputeData = spawnComputeData,
                };

                delayedParticlesQueue.Enqueue(delayedData);
                continue;
            }

            posData.argsBuffer = GetArgsBuffer();

            posData.quadScaleBuffer = GetQuadScaleBuffer();
            posData.quadScaleBuffer.SetData(posData.quadScales);
            
            posData.mpb = GetMPB();

            posData.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
            
            posData.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);

            posData.mpb.SetInt("_QuadScaleCount", posData.quadScales.Length);
            posData.mpb.SetBuffer("_QuadAndPivotScales", posData.quadScaleBuffer);

            posData.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);
            posData.mpb.SetBuffer("_Particles", spawnComputeData.outputBuffer);
            posData.mpb.SetInt("_SpriteCount", particleAtlas.spriteData.Length);
            posData.mpb.SetInt("_SpritesPerParticle", posData.spritesPerParticle);

            posData.mpb.SetInt("_SpriteIndex", posData.spriteIndex);

            posData.spawnState = SpawnState.MovingIn;

            if (posData.preScrollers != null)
            {
                for (int j = 0; j < posData.preScrollers.Length; j++)
                {
                    EdgeScroller preScroller = posData.preScrollers[j];
                
                    preScroller.mpb = GetMPB();
                    preScroller.argsBuffer = GetArgsBuffer();
                    preScroller.edgeSpriteDataBuffer = GetEdgeSpriteBuffer();
                    preScroller.edgeSpriteDataBuffer.SetData(preScroller.spriteData);
                
                    preScroller.mpb.SetTexture("_AtlasTexture", particleAtlas.atlas.texture);
                
                    preScroller.mpb.SetBuffer("_Particles", spawnComputeData.outputBuffer);
                    preScroller.mpb.SetBuffer("_SpriteData", particleAtlas.spriteDataBuffer);
                    preScroller.mpb.SetBuffer("_EdgeSpriteData", preScroller.edgeSpriteDataBuffer);

                    preScroller.mpb.SetInt("_ParticleOffset", posData.minParticleIndex);

                    posData.preScrollers[j] = preScroller;

                }
            }

            switch (spawnComputeData.particleType)
            {

                case ParticleType.Zone:
                {
                    for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
                    {
                        spawnComputeData.depthInputs[j] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);

                        spawnComputeData.offsetInputs[j].x = posData.posX;
                        spawnComputeData.offsetInputs[j].y = posData.posY;
                        spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Born;

                        if (posData.elevate)
                        {
                            spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Elevation;
                        }
                        else
                        {
                            spawnComputeData.moveInputs[j] &= ~((uint)ParticleMoveInputs.Elevation);
                        }
                    }
                }
                break;


                case ParticleType.Scroll:
                {
                    if (posData.widthType == ParticleWidthType.Sliced)
                    {
                        for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
                        {
                            spawnComputeData.depthInputs[j] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);

                            Vector4 offsetInput = new Vector4();
                            offsetInput.x = posData.posX;
                            offsetInput.y = posData.posY;

                            float widthOffset = particleAtlas.spriteData[posData.spriteIndex].worldPivotAndSize.z;
                            widthOffset += posData.scaleX * particleAtlas.spriteData[posData.spriteIndex + 1].worldPivotAndSize.z;
                            widthOffset += particleAtlas.spriteData[posData.spriteIndex + 2].worldPivotAndSize.z;

                            offsetInput.z = widthOffset;

                            spawnComputeData.offsetInputs[j] = offsetInput;

                            if (posData.elevate)
                            {
                                spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Elevation;
                            }
                            else
                            {
                                spawnComputeData.moveInputs[j] &= ~((uint)ParticleMoveInputs.Elevation);
                            }
                            spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Born;

                            spawnComputeData.prevIndicesInputs[j] = posData.prevDepthIndices;
                        }

                    }
                    else
                    {
                        for (int j = posData.minParticleIndex; j <= posData.maxParticleIndex; j++)
                        {
                            spawnComputeData.depthInputs[j] = new Vector4(posData.depth, posData.particleCount, posData.depthSize, posData.minParticleIndex);

                            Vector4 offsetInput = new Vector4();
                            offsetInput.x = posData.posX;
                            offsetInput.y = posData.posY;
                            offsetInput.z = posData.scaleX * particleAtlas.spriteData[posData.spriteIndex].worldPivotAndSize.z;

                            spawnComputeData.offsetInputs[j] = offsetInput;

                            if (posData.elevate)
                            {
                                spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Elevation;
                            }
                            else
                            {
                                spawnComputeData.moveInputs[j] &= ~((uint)ParticleMoveInputs.Elevation);
                            }
                            spawnComputeData.moveInputs[j] |= (uint)ParticleMoveInputs.Born;

                            spawnComputeData.prevIndicesInputs[j] = posData.prevDepthIndices;
                        }
                    }
                }
                break;
            }

            particleAtlas.posData[i] = posData;

            if (i == particleAtlas.posData.Length - 1)
            {
                newOffset = i + 1;
            }
        }

        particleAtlas.posDataIndexOffset = newOffset;
    }
    private void ReinitSpawnCompute(ref SpawnComputeData spawnComputeData)
    {
        spawnComputeData.depthInputBuffer.SetData(spawnComputeData.depthInputs);
        spawnComputeData.offsetInputBuffer.SetData(spawnComputeData.offsetInputs);
        spawnComputeData.moveInputBuffer.SetData(spawnComputeData.moveInputs);

        switch(spawnComputeData.particleType)
        {
            case ParticleType.Zone:
            {

            }
            break;

            case ParticleType.Scroll:
            {
                spawnComputeData.prevIndicesInputsBuffer.SetData(spawnComputeData.prevIndicesInputs);
            }
            break;
        }

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
            if ((delayedParticleData.spawnComputeData.moveInputs[i] & (uint)ParticleMoveInputs.Dead) != 0)
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

        switch(delayedParticleData.particleAtlas.particleType)
        {
            case ParticleType.Zone:
            {
                delayedParticleData.posData.mpb.SetInt("_SpriteCount", delayedParticleData.particleAtlas.spriteCount);
            }
            break;

            case ParticleType.Scroll:
            {
                delayedParticleData.posData.mpb.SetInt("_SpriteCount", delayedParticleData.posData.spritesPerParticle);
            }
            break;
        }        
        delayedParticleData.posData.mpb.SetInt("_ParticleOffset", delayedParticleData.posData.minParticleIndex);

        delayedParticleData.posData.mpb.SetBuffer("_Particles", delayedParticleData.spawnComputeData.outputBuffer);

        for (int k = delayedParticleData.posData.minParticleIndex; k <= delayedParticleData.posData.maxParticleIndex; k++)
        {
            delayedParticleData.spawnComputeData.depthInputs[k] = new Vector4(delayedParticleData.posData.depth, delayedParticleData.posData.particleCount, delayedParticleData.posData.depthSize, delayedParticleData.posData.minParticleIndex);

            delayedParticleData.spawnComputeData.moveInputs[k] = 0;
        }

        delayedParticleData.particleAtlas.posData[delayedParticleData.index] = delayedParticleData.posData;

        delayedParticleData.spawnComputeData.moveInputBuffer.SetData(delayedParticleData.spawnComputeData.moveInputs);
        delayedParticleData.spawnComputeData.depthInputBuffer.SetData(delayedParticleData.spawnComputeData.depthInputs);
        delayedParticleData.spawnComputeData.compute.Dispatch(delayedParticleData.spawnComputeData.initKernel, delayedParticleData.spawnComputeData.groupSize, 1, 1);

        delayedParticlesQueue.Dequeue();
        delayParticleQueueClock = 0;
    }    
    private void DespawnEdgeScrollers()
    {
        DespawningEdgeScrollers().Forget();
    }
    private void UpdateSky()
    {
        ctsDayNight?.Cancel();
        ctsDayNight = new CancellationTokenSource();
        UpdatingSky().Forget();
    }
    private async UniTask DespawningEdgeScrollers()
    {
        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(spawnData.scrollData.moveInputBuffer);
        while (!request.done) await UniTask.Yield();
        if (request.hasError)
        {
            Debug.LogWarning("Requesting scrollData resulted in error");
        }
        else
        {
            spawnData.scrollData.moveInputs = request.GetData<uint>().ToArray();

            for(int i = 0; i < trip.particleAtlasArray.Length; i++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

                if (particleAtlas.particleType != ParticleType.Scroll) continue;

                for (int j = 0; j < particleAtlas.posDataIndexOffset; j++)
                {
                    ParticlePosData posData = particleAtlas.posData[j];

                    if (posData.spawnState != SpawnState.MovingIn) continue;

                    if ((spawnData.scrollData.moveInputs[posData.minParticleIndex] & (uint)ParticleMoveInputs.FirstOutOfBounds) == 0) continue;

                    posData.spawnState = SpawnState.Alive;
                    particleAtlas.posData[j] = posData;
                }

            }
        }

    }
    private async UniTask UpdatingSky()
    {
        float nextDayNight = trip.dayNightValues[trip.ticketsCheckedTotal];
        float elapsedTime = 0;
        float dayNight = curDayNight;
        try
        {
            while(elapsedTime < DAY_NIGHT_TRANSITION_TIME)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / DAY_NIGHT_TRANSITION_TIME;
               // t = Curves.EaseInOutCubic(t); 

                dayNight = Mathf.Lerp(curDayNight, nextDayNight, t);

                Shader.SetGlobalFloat("_DayNight", dayNight);
                await UniTask.Yield(ctsDayNight.Token);
            }
            curDayNight = nextDayNight;
            Shader.SetGlobalFloat("_DayNight", curDayNight);
        }
        catch (OperationCanceledException)
        {
            curDayNight = dayNight;
        }
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
                ParticlePosData posData = particleAtlas.posData[j];
                posData.argsBuffer?.Release();
                posData.argsBuffer = null;
                posData.quadScaleBuffer?.Release();
                posData.quadScaleBuffer = null;
                posData.spawnState = SpawnState.None;

                if (posData.preScrollers != null)
                {
                    for (int k = 0; k < posData.preScrollers.Length; k++)
                    {
                        EdgeScroller preScroller = posData.preScrollers[k];
                        preScroller.argsBuffer?.Release();
                        preScroller.argsBuffer = null;
                        preScroller.edgeSpriteDataBuffer?.Release();
                        preScroller.edgeSpriteDataBuffer = null;
                    }
                }

                if (posData.postScrollers != null)
                {
                    for (int k = 0; k < posData.postScrollers.Length; k++)
                    {
                        EdgeScroller postScroller = posData.postScrollers[k];
                        postScroller.argsBuffer?.Release();
                        postScroller.argsBuffer = null;
                        postScroller.edgeSpriteDataBuffer?.Release();
                        postScroller.edgeSpriteDataBuffer = null;
                    }
                }
            }

            particleAtlas.spriteDataBuffer?.Release();
            particleAtlas.spriteDataBuffer = null;
            particleAtlas.isCompleted = false;
            particleAtlas.posDataIndexOffset = 0;
        }

        spawnData.active = false;
    }
    private void DisposeCompute(ref SpawnComputeData computeData)
    {
        computeData.moveInputBuffer?.Release();
        computeData.moveInputBuffer = null;
        computeData.depthInputBuffer?.Release();
        computeData.depthInputBuffer = null;
        computeData.offsetInputBuffer?.Release();
        computeData.offsetInputBuffer = null;
        computeData.prevIndicesInputsBuffer?.Release();
        computeData.prevIndicesInputsBuffer = null;
        computeData.outputBuffer?.Release();
        computeData.outputBuffer = null;

        computeData.compute.SetVector("_CamVelocity", Vector4.zero);
        computeData.compute.SetVector("_TrainVelocity", Vector4.zero);

    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnData.bounds.center, spawnData.bounds.size);
    }
#endif
}
