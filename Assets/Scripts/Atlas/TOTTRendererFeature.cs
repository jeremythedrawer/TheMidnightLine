#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static Atlas;
using static AtlasBatch;
using static AtlasSpawn;

public class TOTTRendererFeature : ScriptableRendererFeature
{
    public MaterialIDSO materialIDs;
    public TripSO trip;
    public ZoneSpawnerSO zoneSpawner;
    public CameraStatsSO cameraStats;

    public Material matrixMaterial;
    public Material bloomMaterial;

    [Header("Bloom Settings")]
    public int bloomMipLevel;
    public float bloomIntensity;
    public float bloomSpread;


    [Header("Matrix Settings")]
    public Color color1;
    public Color color2;
    public Texture2D noiseTexture;

    private AtlasBatchPass batchPass;
    private AtlasParticlePass particlePass;
    private MatrixPass matrixPass;
    private BloomPass bloomPass;
    //private AtlasPostUIPass postUIPass;

    private class AtlasPassData
    {
        public CameraStatsSO cameraStats;
    }

    public override void Create()
    {
        batchPass = new AtlasBatchPass(materialIDs, cameraStats);
        particlePass = new AtlasParticlePass(trip, zoneSpawner, materialIDs);
        matrixPass = new MatrixPass(this);
        bloomPass = new BloomPass(this);
       // postUIPass = new AtlasPostUIPass(materialIDs);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        PrepareFrame();
        renderer.EnqueuePass(batchPass);
        renderer.EnqueuePass(particlePass);
        renderer.EnqueuePass(matrixPass);
        //renderer.EnqueuePass(postUIPass);
        //renderer.EnqueuePass(bloomPass);
    }
    private class AtlasBatchPass : ScriptableRenderPass
    {
        private static MaterialIDSO materialIDs;
        private static CameraStatsSO cameraStats;
        public AtlasBatchPass(MaterialIDSO materialID, CameraStatsSO camStats)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            materialIDs = materialID;
            cameraStats = camStats;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<AtlasPassData>("Atlas Batch Pass", out AtlasPassData passData);
            passData.cameraStats = cameraStats;
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);


            builder.SetRenderFunc((AtlasPassData data, RasterGraphContext ctx) =>
            {
                ExecuteBatch(ctx.cmd, data.cameraStats);
            });
        }

        private static void ExecuteBatch(RasterCommandBuffer cmd, CameraStatsSO camStats)
        {
#if UNITY_EDITOR
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            Scene prefabScene = default;

            if (prefabStage != null) prefabScene = prefabStage.scene;
#endif
            foreach ((BatchKey key, BatchData data) batch in batchList)
            {
                int count = 0;
                for (int i = 0; i < batch.data.singularRenderInputs.Count && count < MAX; i++)
                {
                    SingularRenderInput renderInput = batch.data.singularRenderInputs[i];

                    if (renderInput == null || !renderInput.gameObject.activeInHierarchy) continue;
#if UNITY_EDITOR

                    if (prefabStage != null)
                    {
                        if (renderInput.gameObject.scene != prefabScene) continue;
                    }
#endif
                    //if (atlasRenderer.bounds.max.x < cameraStats.camLeft || atlasRenderer.bounds.min.x > cameraStats.camRight || atlasRenderer.bounds.max.y < cameraStats.camBottom || atlasRenderer.bounds.min.y > cameraStats.camTop) continue;

                    //if (atlasRenderer.customMPB != null)
                    //{
                    //    cmd.DrawMesh(AtlasBatch.quad, atlasRenderer.spriteMatrices[0], atlasRenderer.batchKey.material, submeshIndex: 0, shaderPass: 0, atlasRenderer.customMPB);
                    //    continue;
                    //}

                    batch.data.spriteData[count] = new SpriteData
                    {
                        worldPosition = renderInput.gameObject.transform.position,
                        worldPivotAndScale = renderInput.pivotAndSize,
                        uvSizeAndPos = renderInput.uvSizeAndPos,
                        scaleAndFlip = renderInput.scaleAndFlip,
                    };
                    count++;
                }

                for (int i = 0; i < batch.data.multipleRenderInputs.Count && count < MAX; i++)
                {
                    MultipleRenderInput renderInput = batch.data.multipleRenderInputs[i];

                    if (renderInput == null || !renderInput.gameObject.activeInHierarchy) continue;
#if UNITY_EDITOR

                    if (prefabStage != null)
                    {
                        if (renderInput.gameObject.scene != prefabScene) continue;
                    }
#endif
                    //if (renderInput.bounds.max.x < cameraStats.camLeft || renderInput.bounds.min.x > cameraStats.camRight || renderInput.bounds.max.y < cameraStats.camBottom || renderInput.bounds.min.y > cameraStats.camTop) continue;
                    for (int j = 0; j < renderInput.worldPivotAndSize.Length; j++)
                    {
                        batch.data.spriteData[count] = new SpriteData
                        {
                            worldPosition = renderInput.gameObject.transform.position, // TODO: Make an offset set hear
                            worldPivotAndScale = renderInput.worldPivotAndSize[j],
                            uvSizeAndPos = renderInput.uvSizeAndPos[j],
                            scaleAndFlip = renderInput.scaleAndFlip[j],
                        };
                        count++;
                    }
                }

                if (count == 0) continue;

                batch.data.spriteDataBuffer.SetData(batch.data.spriteData, 0, 0, count);
                AtlasBatch.args[1] = (uint)count;
                batch.data.argsBuffer.SetData(AtlasBatch.args);
                batch.data.mpb.SetBuffer("_SpriteData", batch.data.spriteDataBuffer);

                cmd.DrawMeshInstancedIndirect(AtlasBatch.Quad, 0, batch.key.material, 0, batch.data.argsBuffer, 0, batch.data.mpb);
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
            for (int i = 0; i < trip.zoneSpawnerData.Length; i++)
            {
                ZoneSpawnerData zoneSpawnerData = trip.zoneSpawnerData[i];

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
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;
            rendererFeature = srf;
        }
        private class MatrixPassData
        {
            public TextureHandle sourceColor;
            public TextureHandle targetColor;
            public TextureHandle depthTexture;
            public Material material;
            public Color color1;
            public Color color2;
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
                passData.color1 = rendererFeature.color1;
                passData.color2 = rendererFeature.color2;

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
            passData.material.SetColor("_Color1", passData.color1);
            passData.material.SetColor("_Color2", passData.color2);
            passData.material.SetTexture("_NoiseTexture", rendererFeature.noiseTexture);
            Blitter.BlitTexture(ctx.cmd, passData.sourceColor, Vector2.one, passData.material, 0);
        }
    }
//    private class AtlasPostUIPass : ScriptableRenderPass
//    {
//        private static MaterialIDSO materialIDs;
//        public AtlasPostUIPass(MaterialIDSO materialID)
//        {
//            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
//            materialIDs = materialID;
//        }
//        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
//        {
//            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

//            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<AtlasPassData>("Atlas Batch Pass", out AtlasPassData passData);
//            builder.SetRenderAttachment(resources.activeColorTexture, 0);
//            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
//            builder.AllowPassCulling(false);


//            builder.SetRenderFunc((AtlasPassData data, RasterGraphContext ctx) =>
//            {
//                ExecuteUI(ctx.cmd);
//            });
//        }

//        private static void ExecuteUI(RasterCommandBuffer cmd)
//        {
//#if UNITY_EDITOR
//            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
//            Scene prefabScene = default;

//            if (prefabStage != null) prefabScene = prefabStage.scene;
//#endif
//            foreach ((BatchKey key, UIBatchData data) batch in uiBatchList)
//            {
//                int count = 0;
//                for (int i = 0; i < batch.data.uiData.Count && count < MAX; i++)
//                {
//                    RenderInput renderInput = batch.data.uiData[i];
//#if UNITY_EDITOR
//                    if (prefabStage != null)
//                    {
//                        if (renderInput.gameObject.scene != prefabScene) continue;
//                    }
//#endif
//                    for (int j = 0; j < renderInput.position.Length; j++)
//                    {
//                        batch.data.matrices[count] = renderInput.matrices[j];
//                        batch.data.uvSizeAndPosData[count] = renderInput.sprites[j].uvSizeAndPos;
//                        batch.data.widthHeightFlip[count] = renderInput.widthHeightFlip[j];
//                        count++;
//                    }
//                }

//                if (count == 0) continue;

//                batch.data.mpb.SetVectorArray(materialIDs.ids.uvSizeAndPos, batch.data.uvSizeAndPosData);
//                batch.data.mpb.SetVectorArray(materialIDs.ids.widthHeightFlip, batch.data.widthHeightFlip);
//                cmd.DrawMeshInstanced(AtlasBatch.quad, submeshIndex: 0, batch.key.material, shaderPass: 0, batch.data.matrices, count, batch.data.mpb);
//            }
//        }
//    }
}

