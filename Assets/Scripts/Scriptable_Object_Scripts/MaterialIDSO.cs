using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialIDSO", menuName = "Midnight Line SOs / Material ID SO")]
public class MaterialIDSO : ScriptableObject
{
    [Serializable] public struct IDs
    {
        public int normAnimTime;
        public int fontColor;
        public int ditherValue;
        public int color;
        public int zPos;
        public int alpha;
        public int mainTex;
        public int hovered;
        public int selected;
        public int targetPosition;
        public int density;
        public int scrollTime;
        public int brightness;
        public int atlasSize;
        public int entityDepthRange;
        public int trainVelocity;


        public int bgParticleOutputs;
        public int bgParticleInputs;
        public int backgroundMaskCount;
        public int spawnerMinPos;
        public int spawnerMaxPos;
        public int spawnerSize;
        public int lodLevelThreshold0;
        public int lodLevelThreshold1;
        public int particleCount;

        public int atlas;
        public int uvPositions;
        public int uvSizes;
        public int spriteCount;
        public int backgroundMask;
        public int lodLevel;
    }
    [SerializeField] public IDs ids;

    public void SetMaterialPropertyIDs()
    {
        ids.normAnimTime = Shader.PropertyToID("_NormAnimTime");
        ids.fontColor = Shader.PropertyToID("_FaceColor");
        ids.ditherValue = Shader.PropertyToID("_DitherValue");
        ids.color = Shader.PropertyToID("_Color");
        ids.zPos = Shader.PropertyToID("_ZPos");
        ids.alpha = Shader.PropertyToID("_Alpha");
        ids.mainTex = Shader.PropertyToID("_MainTex");
        ids.hovered = Shader.PropertyToID("_Hovered");
        ids.selected = Shader.PropertyToID("_Selected");
        ids.targetPosition = Shader.PropertyToID("_TargetPosition");
        ids.density = Shader.PropertyToID("_Density");
        ids.scrollTime = Shader.PropertyToID("_ScrollTime");
        ids.brightness = Shader.PropertyToID("_Brightness");
        ids.atlasSize = Shader.PropertyToID("_AtlasSize");
        ids.entityDepthRange = Shader.PropertyToID("_EntityDepthRange");
        ids.trainVelocity = Shader.PropertyToID("_TrainVelocity");
        ids.spawnerMinPos = Shader.PropertyToID("_SpawnerMinPos");
        ids.spawnerMaxPos = Shader.PropertyToID("_SpawnerMaxPos");
        ids.spawnerSize = Shader.PropertyToID("_SpawnerSize");

        ids.particleCount = Shader.PropertyToID("_ParticleCount");
        ids.bgParticleOutputs = Shader.PropertyToID("_BGParticleOutputs");
        ids.atlas = Shader.PropertyToID("_Atlas");
        ids.uvPositions = Shader.PropertyToID("_UVPositions");
        ids.uvSizes = Shader.PropertyToID("_UVSizes");
        ids.spriteCount = Shader.PropertyToID("_SpriteCount");
        ids.bgParticleInputs = Shader.PropertyToID("_BGParticleInputs");
        ids.backgroundMask = Shader.PropertyToID("_BackgroundMask");
        ids.backgroundMaskCount = Shader.PropertyToID("_BackgroundMaskCount");
        ids.lodLevel = Shader.PropertyToID("_LODLevel");
        ids.lodLevelThreshold0 = Shader.PropertyToID("_LODThreshold0");
        ids.lodLevelThreshold1 = Shader.PropertyToID("_LODThreshold1");
    }
}
