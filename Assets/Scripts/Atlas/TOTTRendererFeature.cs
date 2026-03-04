using UnityEditor.SceneManagement;
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
    public AtlasSpawnerSettingsSO spawnerSettings;
    public ZoneSpawnerStatsSO spawnerStats;

    public Material matrixMaterial;
    public Material bloomMaterial;

    [Header("Bloom Settings")]
    public int bloomMipLevel;
    public float bloomIntensity;
    public float bloomSpread;


    [Header("OKLAB Settings")]
    public float lightness;
    public float greenToRed;
    public float blueToYellow;
    
    private AtlasBatchPass batchPass;
    private AtlasParticlePass particlePass;
    private MatrixPass matrixPass;
    private BloomPass bloomPass;

    private class ZonePassData
    {
        public Camera camera;
    }

    public override void Create()
    {
        batchPass = new AtlasBatchPass(materialIDs);
        particlePass = new AtlasParticlePass(spawnerSettings, spawnerStats, materialIDs);
        matrixPass = new MatrixPass(this);
        bloomPass = new BloomPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        PrepareFrame();
        renderer.EnqueuePass(batchPass);
        renderer.EnqueuePass(particlePass);
        renderer.EnqueuePass(matrixPass);
        renderer.EnqueuePass(bloomPass);
    }
    private class AtlasBatchPass : ScriptableRenderPass
    {
        private static MaterialIDSO materialIDs;
        public AtlasBatchPass(MaterialIDSO materialID)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            materialIDs = materialID;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<ZonePassData>("Atlas Batch Pass", out ZonePassData passData);
            passData.camera = cameraData.camera;
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);


            builder.SetRenderFunc((ZonePassData data, RasterGraphContext ctx) =>
            {
                ExecuteBatch(ctx.cmd, data.camera);
            });
        }

        private static void ExecuteBatch(RasterCommandBuffer cmd, Camera camera)
        {
#if UNITY_EDITOR
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            Scene prefabScene = default;

            if (prefabStage != null) prefabScene = prefabStage.scene;
#endif
            foreach ((BatchKey key, BatchData data) batch in batchList)
            {
                int count = 0;
                for (int i = 0; i < batch.data.renderers.Count && count < MAX; i++)
                {
                    AtlasRenderer atlasRenderer = batch.data.renderers[i];

                    if (atlasRenderer == null || !atlasRenderer.enabled) continue;

#if UNITY_EDITOR
                    if (prefabStage != null)
                    {
                        if (atlasRenderer.gameObject.scene != prefabScene) continue;
                    }
#endif

                    if (atlasRenderer.mpb != null)
                    {
                        cmd.DrawMesh(atlasRenderer.batchKey.mesh, atlasRenderer.GetMatrix(), atlasRenderer.batchKey.material, submeshIndex: 0, shaderPass: 0, atlasRenderer.mpb);
                        continue;
                    }

                    if (atlasRenderer.spriteMode == SpriteMode.Slice)
                    {
                        ref SliceSprite slicedSprite = ref atlasRenderer.atlas.slicedSprites[atlasRenderer.spriteIndex];

                        Matrix4x4[] sliceMatrices = atlasRenderer.Get9SliceMatrices();

                        for (int j = 0; j < 9; j++)
                        {
                            Matrix4x4 sliceMatrix = sliceMatrices[j];

                            batch.data.matrices[count] = sliceMatrix;
                            batch.data.uvSizeAndPosData[count] = slicedSprite.uvSizeAndPos[j];
                            batch.data.widthHeightFlip[count] = atlasRenderer.widthHeightFlip[j];
                            count++;
                        }
                    }
                    else
                    {
                        batch.data.matrices[count] = atlasRenderer.GetMatrix();
                        batch.data.uvSizeAndPosData[count] = atlasRenderer.sprite.uvSizeAndPos;
                        batch.data.widthHeightFlip[count] = atlasRenderer.widthHeightFlip[0];
                        count++;
                    }

                }

                if (count == 0) continue;

                batch.data.mpb.SetVectorArray(materialIDs.ids.uvSizeAndPos, batch.data.uvSizeAndPosData);
                batch.data.mpb.SetVectorArray(materialIDs.ids.widthHeightFlip, batch.data.widthHeightFlip);
                cmd.DrawMeshInstanced(batch.key.mesh, submeshIndex: 0, batch.key.material, shaderPass: 0, batch.data.matrices, count, batch.data.mpb);
            }
        }
    }
    private class AtlasParticlePass : ScriptableRenderPass
    {
        private AtlasSpawnerSettingsSO zoneSpawnerSettings;
        private ZoneSpawnerStatsSO zoneSpawnerStats;
        public AtlasParticlePass(AtlasSpawnerSettingsSO settings, ZoneSpawnerStatsSO stats, MaterialIDSO matIDs)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            zoneSpawnerSettings = settings;
            zoneSpawnerStats = stats;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<ZonePassData>("Zone Particle Pass", out ZonePassData passData);
            passData.camera = cameraData.camera;
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);

            builder.SetRenderFunc((ZonePassData data, RasterGraphContext ctx) =>
            {
                ExecuteZoneParticles(ctx.cmd);
            });
        }

        private void ExecuteZoneParticles(RasterCommandBuffer cmd)
        {

            for (int i = 0; i < zoneSpawnerStats.zoneSpawnerDataArray.Length; i++)
            {
                ZoneSpawnerData zoneSpawnerData = zoneSpawnerStats.zoneSpawnerDataArray[i];

                if (!zoneSpawnerData.zoneSpawnerData.active || zoneSpawnerData.zoneSpawnerData.particleBuffer == null) continue;

                cmd.DrawProcedural(Matrix4x4.identity, zoneSpawnerSettings.material, shaderPass: 0, MeshTopology.Quads, MAX_VERTEX_COUNT, 1, zoneSpawnerData.zoneSpawnerData.mpb);
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
            passData.material.SetFloat("_Lightness", rendererFeature.lightness);
            passData.material.SetFloat("_GreenToRed", rendererFeature.greenToRed);
            passData.material.SetFloat("_BlueToYellow", rendererFeature.blueToYellow);
            Blitter.BlitTexture(ctx.cmd, passData.sourceColor, Vector2.one, passData.material, 0);
        }
    }
}

