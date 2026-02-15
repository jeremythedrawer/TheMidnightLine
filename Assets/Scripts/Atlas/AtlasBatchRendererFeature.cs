using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static AtlasBatch;

public class AtlasBatchRendererFeature : ScriptableRendererFeature
{
    public MaterialIDSO materialIDs;
    AtlasBatchRenderPass pass;
    class PassData
    {
        public Camera camera;
    }

    public override void Create()
    {
        pass = new AtlasBatchRenderPass(materialIDs);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        PrepareFrame();
        renderer.EnqueuePass(pass);
    }
    class AtlasBatchRenderPass : ScriptableRenderPass
    {
        private static MaterialIDSO materialIDs;
        public AtlasBatchRenderPass(MaterialIDSO materialID)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            materialIDs = materialID;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData> ("Atlas Batch Pass", out PassData passData);
            passData.camera = cameraData.camera;
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
            builder.AllowPassCulling(false);


            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Execute(ctx.cmd, data.camera);
            });
        }

        private static void Execute(RasterCommandBuffer cmd, Camera camera)
        {
            foreach ((BatchKey key, BatchData data) batch in batchList)
            {
                int count = 0;
                for (int i = 0; i < batch.data.renderers.Count && count < MAX; i++)
                {
                    AtlasRenderer atlasRenderer = batch.data.renderers[i];
                    if (atlasRenderer == null || !atlasRenderer.enabled) continue;


                    if (atlasRenderer.spriteType == Atlas.SpriteType.Slice)
                    {
                        Matrix4x4[] sliceMatrices = atlasRenderer.Get9SliceMatrices();

                        for (int j = 0; j < 9; j++)
                        {
                            Matrix4x4 sliceMatrix = sliceMatrices[j];

                            batch.data.matrices[count] = sliceMatrix;
                            batch.data.uvSizeAndPosData[count] = atlasRenderer.sliceSprite.uvSizeAndPos[j];
                            batch.data.widthHeightArray[count] = atlasRenderer.widthAndHeight[j];

                            count++;

                        }
                    }
                    else
                    {
                        batch.data.matrices[count] = atlasRenderer.GetMatrix();
                        batch.data.uvSizeAndPosData[count] = atlasRenderer.sprite.uvSizeAndPos;
                        batch.data.widthHeightArray[count] = atlasRenderer.widthAndHeight[0];
                        count++;
                    }

                }

                if (count == 0) continue;

                batch.data.mpb.SetVectorArray(materialIDs.ids.uvSizeAndPos, batch.data.uvSizeAndPosData);
                batch.data.mpb.SetVectorArray(materialIDs.ids.widthAndHeight, batch.data.widthHeightArray);
                cmd.DrawMeshInstanced(batch.key.mesh, submeshIndex: 0, batch.key.material, shaderPass: 0, batch.data.matrices, count, batch.data.mpb);
            }
        }
    }
}

