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
    public MaterialIDSO materialIDs;
    public TripSO trip;
    public ZoneSpawnerSO zoneSpawner;
    public CameraStatsSO cameraStats;
    public Mesh quad;

    public Material matrixMaterial;
    public Material bloomMaterial;

    [Header("Bloom Settings")]
    public int bloomMipLevel;
    public float bloomIntensity;
    public float bloomSpread;


    [Header("Matrix Settings")]
    public Texture2D noiseTexture;

    private AtlasBatchPass batchPass;
    private AtlasParticlePass particlePass;
    private MatrixPass matrixPass;
    private BloomPass bloomPass;

    private class AtlasPassData
    {
        public CameraStatsSO cameraStats;
    }

    public override void Create()
    {
        if (quad == null) quad = AtlasRendering.SetQuad();
        batchPass = new AtlasBatchPass(materialIDs, cameraStats, quad);
        particlePass = new AtlasParticlePass(trip, zoneSpawner, materialIDs);
        matrixPass = new MatrixPass(this);
        bloomPass = new BloomPass(this);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        PrepareFrame();
        renderer.EnqueuePass(batchPass);
        renderer.EnqueuePass(particlePass);
        renderer.EnqueuePass(matrixPass);
        //renderer.EnqueuePass(bloomPass);
    }
    private class AtlasBatchPass : ScriptableRenderPass
    {
        private static MaterialIDSO materialIDs;
        private static CameraStatsSO cameraStats;
        private static Mesh quad;
        private uint[] args;
        
        public AtlasBatchPass(MaterialIDSO materialID, CameraStatsSO camStats, Mesh quadToUse)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            materialIDs = materialID;
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
            foreach ((BatchKey key, SpriteBatch data) batch in spriteBatchList)
            {
                int count = 0;
                for (int i = 0; i < batch.data.rendererList.Count && count < MAX; i++)
                {
                    AtlasRenderer renderer = batch.data.rendererList[i];

                    if (renderer == null || renderer.gameObject == null || !renderer.gameObject.activeInHierarchy) continue;
#if UNITY_EDITOR


                    if (prefabStage != null)
                    {
                        if (renderer.gameObject.scene != prefabScene) continue;
                    }
#endif

                    renderer.UpdateBounds();
#if !UNITY_EDITOR
                    //if (renderer.bounds.max.x < cameraStats.camWorldLeft || renderer.bounds.min.x > cameraStats.camWorldRight || renderer.bounds.max.y < cameraStats.camWorldBottom || renderer.bounds.min.y > cameraStats.camWorldTop) continue; //TODO: use new cam bounds
#endif
                    switch (renderer.rendererType)
                    { 
                        case AtlasRendererType.SimpleWorld:
                        case AtlasRendererType.MotionWorld:
                        case AtlasRendererType.SimpleScreen:
                        case AtlasRendererType.MotionScreen:
                        {
                            batch.data.spriteData[count] = new SpriteData
                            {
                                worldPosition = renderer.gameObject.transform.position,
                                worldPivotAndScale = renderer.worldPivotAndSize,
                                uvSizeAndPos = renderer.uvSizeAndPosition,
                                scaleAndFlip = renderer.scaleAndFlip,
                                custom = renderer.custom,
                            };
                            count++;
                        }
                        break;

                        case AtlasRendererType.SliceWorld:
                        case AtlasRendererType.TextWorld:
                        case AtlasRendererType.SliceScreen:
                        case AtlasRendererType.TextScreen:
                        {
                            for (int j = 0; j < renderer.worldPivotsAndSizes.Length; j++)
                            {
                                batch.data.spriteData[count] = new SpriteData
                                {
                                    worldPosition = renderer.transform.position,
                                    worldPivotAndScale = renderer.worldPivotsAndSizes[j],
                                    uvSizeAndPos = renderer.uvSizesAndPositions[j],
                                    scaleAndFlip = renderer.scalesAndFlips[j],
                                    custom = renderer.customs[j],

                                };
                                count++;
                            }
                        }
                        break;
                    }
                }

                if (count == 0) continue;

                batch.data.spriteDataBuffer.SetData(batch.data.spriteData, 0, 0, count);
                args[1] = (uint)count;
                batch.data.argsBuffer.SetData(args);
                batch.data.mpb.SetBuffer("_SpriteData", batch.data.spriteDataBuffer);
                batch.data.mpb.SetTexture("_AtlasTexture", batch.key.texture);
                cmd.DrawMeshInstancedIndirect(quad, 0, batch.key.material, 0, batch.data.argsBuffer, 0, batch.data.mpb);
            }
        }
    }
    private class AtlasParticlePass : ScriptableRenderPass
    {
        private TripSO trip;
        private ZoneSpawnerSO zoneSpawner;
        public AtlasParticlePass(TripSO curTrip, ZoneSpawnerSO spawner, MaterialIDSO matIDs)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            trip = curTrip;
            zoneSpawner = spawner;
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
                ExecuteZoneParticles(ctx.cmd);
            });
        }

        private void ExecuteZoneParticles(RasterCommandBuffer cmd)
        {
            for (int i = 0; i < trip.zoneAreas.Length; i++)
            {
                ZoneArea zoneSpawnerData = trip.zoneAreas[i];

                if (!zoneSpawnerData.active) continue;
                cmd.DrawProcedural(Matrix4x4.identity, zoneSpawner.material, shaderPass: 0, MeshTopology.Quads, zoneSpawnerData.particleCount * 4, 1, zoneSpawnerData.mpb);
            }
        }
    }
    private class BloomPass : ScriptableRenderPass
    {
        private static TOTTRendererFeature rendererFeature;
        public BloomPass(TOTTRendererFeature srf)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            rendererFeature = srf;
        }
        private class BloomPassData
        {
            public TextureHandle curSourceColor;
            public TextureHandle targetColor;
            public TextureHandle originalCameraColor;
            public Material material;
            public int bloomMipLevel;
            public float bloomIntensity;
            public float bloomSpread;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game) return;
            if (!resourceData.cameraColor.IsValid()) return;
            if (rendererFeature.bloomMaterial == null) return;

            TextureDesc camColorTexDesc = resourceData.cameraColor.GetDescriptor(renderGraph);
            camColorTexDesc.name = "BloomTexture";
            camColorTexDesc.useMipMap = true;
            camColorTexDesc.autoGenerateMips = true;

            TextureHandle bloomPreTexHandle = renderGraph.CreateTexture(camColorTexDesc);
            TextureHandle bloomHTexHandle = renderGraph.CreateTexture(camColorTexDesc);
            TextureHandle bloomVTexHandle = renderGraph.CreateTexture(camColorTexDesc);

            BloomPassData passData;
            using (IRasterRenderGraphBuilder preBuilder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom Pre Pass", out passData))
            {
                passData.curSourceColor = resourceData.cameraColor;
                passData.targetColor = bloomPreTexHandle;
                passData.material = rendererFeature.bloomMaterial;
                passData.bloomMipLevel = rendererFeature.bloomMipLevel;
                passData.bloomIntensity = rendererFeature.bloomIntensity;
                passData.bloomSpread = rendererFeature.bloomSpread;


                preBuilder.UseTexture(passData.curSourceColor);
                preBuilder.SetRenderAttachment(passData.targetColor, index: 0, AccessFlags.WriteAll);

                preBuilder.SetRenderFunc((BloomPassData data, RasterGraphContext ctx) =>
                {
                    ExecutePreBloomPass(data, ctx);
                });
            }

            using (IRasterRenderGraphBuilder preBuilder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom Horizontal Pass", out passData))
            {
                passData.curSourceColor = bloomPreTexHandle;
                passData.targetColor = bloomHTexHandle;
                passData.material = rendererFeature.bloomMaterial;

                preBuilder.UseTexture(passData.curSourceColor);
                preBuilder.SetRenderAttachment(passData.targetColor, index: 0, AccessFlags.WriteAll);

                preBuilder.SetRenderFunc((BloomPassData data, RasterGraphContext ctx) =>
                {
                    ExecuteHBloomPass(data, ctx);
                });
            }

            using (IRasterRenderGraphBuilder preBuilder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom Vertical Pass", out passData))
            {
                passData.curSourceColor = bloomHTexHandle;
                passData.targetColor = bloomVTexHandle;
                passData.material = rendererFeature.bloomMaterial;
                passData.originalCameraColor = resourceData.cameraColor;

                preBuilder.UseTexture(passData.curSourceColor);
                preBuilder.UseTexture(passData.originalCameraColor);
                preBuilder.SetRenderAttachment(passData.targetColor, index: 0, AccessFlags.WriteAll);

                preBuilder.SetRenderFunc((BloomPassData data, RasterGraphContext ctx) =>
                {
                    ExecuteVBloomPass(data, ctx);
                });
            }

            resourceData.cameraColor = passData.targetColor;
        }
        private static void ExecutePreBloomPass(BloomPassData passData, RasterGraphContext ctx)
        {
            if (passData.material == null) return;
            passData.material.SetFloat("_MipLevel", passData.bloomMipLevel);
            passData.material.SetFloat("_BloomIntensity", passData.bloomIntensity);
            passData.material.SetFloat("_BloomSpread", passData.bloomSpread);
            Blitter.BlitTexture(ctx.cmd, passData.curSourceColor, Vector2.one, passData.material, pass: 0);
        }
        private static void ExecuteHBloomPass(BloomPassData passData, RasterGraphContext ctx)
        {
            Blitter.BlitTexture(ctx.cmd, passData.curSourceColor, Vector2.one, passData.material, pass: 1);
        }
        private static void ExecuteVBloomPass(BloomPassData passData, RasterGraphContext ctx)
        {
            passData.material.SetTexture("_SourceTex", passData.originalCameraColor);
            Blitter.BlitTexture(ctx.cmd, passData.curSourceColor, Vector2.one, passData.material, pass: 2);
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
            passData.material.SetTexture("_NoiseTexture", rendererFeature.noiseTexture);
            Blitter.BlitTexture(ctx.cmd, passData.sourceColor, Vector2.one, passData.material, 0);
        }
    }
}

