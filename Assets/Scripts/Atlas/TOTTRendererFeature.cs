#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static AtlasRendering;
using static AtlasSpawn;

public class TOTTRendererFeature : ScriptableRendererFeature
{
    public TripSO trip;
    public SpawnData spawnerData;
    public SpyStatsSO spyStats;
    public CameraStatsSO cameraStats;
    public Mesh quad;

    public Material matrixMaterial;
    public Material pixelPerfectMaterial;

    private AtlasBatchPass batchPass;
    private AtlasParticlePass particlePass;
    private MatrixPass matrixPass;
    private PixelPerfectPass pixelPerfectPass;
    private class AtlasPassData
    {
        public CameraStatsSO cameraStats;
    }

    public override void Create()
    {
        if (quad == null) quad = AtlasRendering.SetQuad();
        batchPass = new AtlasBatchPass(cameraStats, quad);
        particlePass = new AtlasParticlePass(trip, spawnerData, spyStats, quad);
        matrixPass = new MatrixPass(this);
        pixelPerfectPass = new PixelPerfectPass(this);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        RefreshFrame();
        renderer.EnqueuePass(batchPass);
        renderer.EnqueuePass(particlePass);
        renderer.EnqueuePass(matrixPass);
        renderer.EnqueuePass(pixelPerfectPass);
    }
    private class AtlasBatchPass : ScriptableRenderPass
    {
        private static CameraStatsSO cameraStats;
        private static Mesh quad;
        private uint[] args;
        
        public AtlasBatchPass( CameraStatsSO camStats, Mesh quadToUse)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            cameraStats = camStats;
            quad = quadToUse;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<AtlasPassData>("Atlas Batch Pass", out AtlasPassData passData);
            passData.cameraStats = cameraStats;
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            args = new uint[5]
            {
                quad.GetIndexCount(0),
                0,
                quad.GetIndexStart(0),
                quad.GetBaseVertex(0),
                0
            };

            builder.SetRenderFunc((AtlasPassData data, RasterGraphContext ctx) =>
            {
                ExecuteBatch(ctx.cmd, data.cameraStats, quad, ref args);
            });
        }

        private static void ExecuteBatch(RasterCommandBuffer cmd, CameraStatsSO camStats, Mesh quad, ref uint[] args)
        {
#if UNITY_EDITOR
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            Scene prefabScene = default;
            if (prefabStage != null) prefabScene = prefabStage.scene;
#endif
            foreach ((BatchKey key, SpriteBatch data) spriteBatch in spriteBatchList)
            {
                int count = 0;
                for (int i = 0; i < spriteBatch.data.atlasRendererList.Count && count < MAX_SPRITE_DATA_COUNT; i++)
                {
                    AtlasRenderer renderer = spriteBatch.data.atlasRendererList[i];

                    if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
#if UNITY_EDITOR
                    if (prefabStage != null) { if (renderer.gameObject.scene != prefabScene) continue; }
#else
                    //if (renderer.bounds.max.x < cameraStats.camWorldLeft || renderer.bounds.min.x > cameraStats.camWorldRight || renderer.bounds.max.y < cameraStats.camWorldBottom || renderer.bounds.min.y > cameraStats.camWorldTop) continue; //TODO: use new cam bounds
#endif

                    renderer.UpdateBounds();

                    switch (renderer.rendererType)
                    { 
                        case AtlasRendererType.SimpleWorld:
                        case AtlasRendererType.MotionWorld:
                        {
                            SpriteData spriteData = spriteBatch.data.spriteData[count];

                            spriteData.worldPosition = renderer.transform.position;
                            spriteData.worldPivotAndSize = renderer.worldPivotAndSize;
                            spriteData.uvSizeAndPos = renderer.uvSizeAndPosition;
                            spriteData.scaleAndFlip = renderer.scaleAndFlip;
                            spriteData.custom = renderer.custom;
                            spriteData.customBit = renderer.customBit;
                            spriteBatch.data.spriteData[count] = spriteData;
                            count++;
                        }
                        break;

                        case AtlasRendererType.SliceWorld:
                        {
                            for (int j = 0; j < renderer.worldPivotsAndSizes.Length; j++)
                            {
                                SpriteData spriteData = spriteBatch.data.spriteData[count];

                                spriteData.worldPosition = renderer.transform.position;    
                                spriteData.worldPivotAndSize = renderer.worldPivotsAndSizes[j];
                                spriteData.uvSizeAndPos = renderer.uvSizesAndPositions[j];
                                spriteData.scaleAndFlip = renderer.scalesAndFlips[j];
                                spriteData.custom = renderer.customs[j];
                                spriteData.customBit = renderer.customBit;
                                spriteBatch.data.spriteData[count] = spriteData;

                                count++;
                            }
                        }
                        break;
                    }
                }

                if (count == 0) continue;

                spriteBatch.data.spriteDataBuffer.SetData(spriteBatch.data.spriteData, 0, 0, count);
                args[1] = (uint)count;
                spriteBatch.data.argsBuffer.SetData(args);
                spriteBatch.data.mpb.SetBuffer("_SpriteData", spriteBatch.data.spriteDataBuffer);
                spriteBatch.data.mpb.SetTexture("_AtlasTexture", spriteBatch.key.texture);
                cmd.DrawMeshInstancedIndirect(quad, 0, spriteBatch.key.material, 0, spriteBatch.data.argsBuffer, 0, spriteBatch.data.mpb);
            }

            foreach((BatchKey key, TextBatch data) textBatch in textBatchList)
            {
                int count = 0;
                for (int i = 0; i < textBatch.data.atlasTextRendererList.Count && count < MAX_SPRITE_DATA_COUNT; i++)
                {
                    AtlasTextRenderer renderer = textBatch.data.atlasTextRendererList[i];

                    if (renderer.gameObject == null || !renderer.gameObject.activeInHierarchy) continue;
#if UNITY_EDITOR
                    if (prefabStage != null) { if (renderer.gameObject.scene != prefabScene) continue; }
#else
                    //if (renderer.bounds.max.x < cameraStats.camWorldLeft || renderer.bounds.min.x > cameraStats.camWorldRight || renderer.bounds.max.y < cameraStats.camWorldBottom || renderer.bounds.min.y > cameraStats.camWorldTop) continue;
#endif
                    for (int j = 0; j < renderer.worldPivotsAndSizes.Length; j++)
                    {
                        SpriteData spriteData = textBatch.data.spriteData[count];

                        spriteData.worldPosition = renderer.transform.position;
                        spriteData.worldPivotAndSize = renderer.worldPivotsAndSizes[j];
                        spriteData.uvSizeAndPos = renderer.uvSizesAndPositions[j];
                        spriteData.scaleAndFlip = renderer.scalesAndFlips[j];
                        spriteData.custom = renderer.customs[j];
                        textBatch.data.spriteData[count] = spriteData;

                        count++;
                    }
                }

                if (count == 0) continue;

                textBatch.data.spriteDataBuffer.SetData(textBatch.data.spriteData, 0, 0, count);
                args[1] = (uint)count;
                textBatch.data.argsBuffer.SetData(args);
                textBatch.data.mpb.SetBuffer("_SpriteData", textBatch.data.spriteDataBuffer);
                textBatch.data.mpb.SetTexture("_AtlasTexture", textBatch.key.texture);
                cmd.DrawMeshInstancedIndirect(quad, 0, textBatch.key.material, 0, textBatch.data.argsBuffer, 0, textBatch.data.mpb);
            }
        }
    }
    private class AtlasParticlePass : ScriptableRenderPass
    {
        private TripSO trip;
        private SpawnData spawner;
        private SpyStatsSO spyStats;
        private static Mesh quad;
        public AtlasParticlePass(TripSO curTrip, SpawnData spawner, SpyStatsSO spyStats, Mesh quadToUse)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            trip = curTrip;
            this.spawner = spawner;
            this.spyStats = spyStats;
            quad = quadToUse;

        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<AtlasPassData>("Zone Particle Pass", out AtlasPassData passData);
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);

            builder.SetRenderFunc((AtlasPassData data, RasterGraphContext ctx) =>
            {
                ExecuteParticles(ctx.cmd);
            });
        }

        private void ExecuteParticles(RasterCommandBuffer cmd)
        {
            if (!spawner.active) return;

            for (int i = 0; i < trip.particleAtlasArray.Length; i++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
                if (particleAtlas.isCompleted) continue;

                for (int j = 0; j < particleAtlas.posData.Length; j++)
                {
                    ParticlePosData posData = particleAtlas.posData[j];

                    if (trip.ticketsCheckedTotal < posData.ticketCheckStart) break;
                    
                    if (posData.argsBuffer == null) continue;

                    argsSpawn[1] = posData.quadCount;
                    posData.argsBuffer.SetData(argsSpawn);

                    switch(particleAtlas.particleType)
                    {
                        case ParticleType.Zone:
                        {
                            cmd.DrawProceduralIndirect(Matrix4x4.identity, spawner.zoneData.material, shaderPass: 0, MeshTopology.Triangles, posData.argsBuffer, argsOffset: 0, posData.mpb);
                        }
                        break;

                        case ParticleType.Scroll:
                        {
                            cmd.DrawProceduralIndirect(Matrix4x4.identity, spawner.scrollData.material, shaderPass: 0, MeshTopology.Triangles, posData.argsBuffer, argsOffset: 0, posData.mpb);

                            switch (posData.spawnState)
                            {
                                case SpawnState.MovingIn:
                                {
                                    if (posData.preScrollers == null) return;
                                    for (int k = 0; k < posData.preScrollers.Length; k++)
                                    {
                                        EdgeScroller preScroller = posData.preScrollers[k];
                                        argsSpawn[1] = (uint)preScroller.spriteData.Length;
                                        preScroller.argsBuffer.SetData(argsSpawn);
                                        cmd.DrawProceduralIndirect(Matrix4x4.identity, spawner.edgeScrollMaterial, shaderPass: 0, MeshTopology.Triangles, preScroller.argsBuffer, argsOffset: 0, preScroller.mpb);
                                    }
                                }
                                break;
                                case SpawnState.MovingOut:
                                {
                                    if (posData.postScrollers == null)
                                    for (int k = 0; k < posData.postScrollers.Length; k++)
                                    {
                                        EdgeScroller postScroller = posData.postScrollers[k];
                                        argsSpawn[1] = (uint)postScroller.spriteData.Length;
                                        Debug.Log(postScroller.spriteData.Length);
                                        postScroller.argsBuffer.SetData(argsSpawn);
                                        cmd.DrawProceduralIndirect(Matrix4x4.identity, spawner.edgeScrollMaterial, shaderPass: 0, MeshTopology.Triangles, postScroller.argsBuffer, argsOffset: 0, postScroller.mpb);
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }

            }
        }
    }
    private class PixelPerfectPass : ScriptableRenderPass
    {
        private static TOTTRendererFeature rendererFeature;
        public PixelPerfectPass(TOTTRendererFeature srf)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            rendererFeature = srf;
        }
        private class PixelPerfectPassData
        {
            public TextureHandle curSourceColor;
            public TextureHandle targetColor;
            public Material material;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game) return;
            if (!resourceData.cameraColor.IsValid()) return;
            if (rendererFeature.pixelPerfectMaterial == null) return;

            TextureDesc camColorTexDesc = resourceData.cameraColor.GetDescriptor(renderGraph);
            camColorTexDesc.name = "PixelPerfectTexture";

            TextureHandle texHandle = renderGraph.CreateTexture(camColorTexDesc);

            PixelPerfectPassData passData;
            using (IRasterRenderGraphBuilder preBuilder = renderGraph.AddRasterRenderPass<PixelPerfectPassData>("Pixel Perfect Pass", out passData))
            {
                passData.curSourceColor = resourceData.cameraColor;
                passData.targetColor = texHandle;
                passData.material = rendererFeature.pixelPerfectMaterial;

                preBuilder.UseTexture(passData.curSourceColor);
                preBuilder.SetRenderAttachment(passData.targetColor, index: 0, AccessFlags.WriteAll);

                preBuilder.SetRenderFunc((PixelPerfectPassData data, RasterGraphContext ctx) =>
                {
                    ExecutePixelPerfectPass(data, ctx);
                });
            }

            resourceData.cameraColor = passData.targetColor;
        }
        private static void ExecutePixelPerfectPass(PixelPerfectPassData passData, RasterGraphContext ctx)
        {
            Blitter.BlitTexture(ctx.cmd, passData.curSourceColor, Vector2.one, passData.material, pass: 0);
        }
    }
    private class MatrixPass : ScriptableRenderPass
    {
        private static TOTTRendererFeature rendererFeature;
        public MatrixPass(TOTTRendererFeature srf)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            rendererFeature = srf;
        }
        private class MatrixPassData
        {
            public TextureHandle sourceColor;
            public TextureHandle targetColor;
            public TextureHandle depthTexture;
            public Material material;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (rendererFeature.matrixMaterial == null) return;
            if (cameraData.cameraType != CameraType.Game) return;
            if (resourceData.cameraColor.IsValid())
            {
                TextureDesc camColorTexDesc = resourceData.cameraColor.GetDescriptor(renderGraph);

                camColorTexDesc.name = "MatrixTexture";

                TextureHandle matrixTexHandle = renderGraph.CreateTexture(camColorTexDesc);

                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MatrixPassData>("Matrix Pass", out MatrixPassData passData);
                passData.sourceColor = resourceData.cameraColor;
                passData.targetColor = matrixTexHandle;
                passData.material = rendererFeature.matrixMaterial;
                passData.depthTexture = resourceData.activeDepthTexture;

                builder.UseTexture(passData.sourceColor);
                builder.UseTexture(passData.depthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(passData.targetColor, index: 0, AccessFlags.WriteAll);

                builder.SetRenderFunc((MatrixPassData maxtrixPassData, RasterGraphContext ctx) =>
                {
                    ExecuteMatrixPass(maxtrixPassData, ctx);
                });

                resourceData.cameraColor = passData.targetColor;
            }
        }
        private static void ExecuteMatrixPass(MatrixPassData passData, RasterGraphContext ctx)
        {
            passData.material.SetTexture("_SourceTex", passData.sourceColor);
            passData.material.SetTexture("_CameraDepthTexture", passData.depthTexture);
            Blitter.BlitTexture(ctx.cmd, passData.sourceColor, Vector2.one, passData.material, 0);
        }
    }
}

