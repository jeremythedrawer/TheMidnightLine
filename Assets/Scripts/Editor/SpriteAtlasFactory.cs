using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "SpriteAtlasFactory", menuName = "Editor SOs / Sprite Atlas Factory")]
public class SpriteAtlasFactory : ScriptableObject
{
    [Serializable] public struct ProcessedComponents
    {     
        public Texture2D spriteAtlas;
        public AnimationClip smokingClip;
        public NPCSO npc;
    }
    [SerializeField] ProcessedComponents[] processedNPCComponents;

    [Serializable] public struct SpyComponent
    {
        public Texture2D spriteAtlas;
        public SpyStatsSO stats;
    }
    [SerializeField] SpyComponent spyComponent;
    public void SetNPCSpritePositionData()
    {
        foreach (ProcessedComponents npcComponent in processedNPCComponents)
        {
            TextureImporter atlasImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(npcComponent.spriteAtlas));
            atlasImporter.isReadable = true;
            atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(npcComponent.spriteAtlas);
            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();

            List<float> smokeKeyframeTimes = new List<float>();
            for (int i = 0; i < spriteRects.Length; i++) // Looping through each sprite in atlas
            {
            
                int startX = Mathf.RoundToInt(spriteRects[i].rect.x);
                int startY = Mathf.RoundToInt(spriteRects[i].rect.y);
                int width = Mathf.RoundToInt(spriteRects[i].rect.width);
                int height = Mathf.RoundToInt(spriteRects[i].rect.height);
                bool foundRed = false;

                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY; y < startY + height; y++)
                    {
                        Color color = npcComponent.spriteAtlas.GetPixel(x, y);

                        if (color == Color.red)
                        {
                            float xPiv = (x - startX) / (float)width;
                            float yPiv = (y - startY) / (float)height;

                            spriteRects[i].pivot = new Vector2(xPiv, yPiv);
                            spriteRects[i].alignment = SpriteAlignment.Custom;

                            foundRed = true;
                        }
                    }
                }

                if (!foundRed) Debug.LogWarning($"never found red pixel origin point of: {spriteRects[i].name}");
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            EditorUtility.SetDirty(npcComponent.spriteAtlas);

            List<Vector2> smokePos = new List<Vector2>();
            for (int i = 0; i < spriteRects.Length; i++) // Looping through each sprite in atlas
            {
                int startX = Mathf.RoundToInt(spriteRects[i].rect.x);
                int startY = Mathf.RoundToInt(spriteRects[i].rect.y);
                int width = Mathf.RoundToInt(spriteRects[i].rect.width);
                int height = Mathf.RoundToInt(spriteRects[i].rect.height);

                float pivotPixelX = startX + spriteRects[i].pivot.x * width;
                float pivotPixelY = startY + spriteRects[i].pivot.y * height;

                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY; y < startY + height; y++)
                    {
                        Color color = npcComponent.spriteAtlas.GetPixel(x, y);

                        if (color == Color.green)
                        {
                            float localPixelX = x - pivotPixelX;
                            float localPixelY = y - pivotPixelY;

                            float metersXPos = localPixelX / atlasImporter.spritePixelsPerUnit;
                            float metersYPos = localPixelY / atlasImporter.spritePixelsPerUnit;

                            smokePos.Add(new Vector2(metersXPos, metersYPos));
                        }
                    }
                }
            }

            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(npcComponent.smokingClip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;

                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(npcComponent.smokingClip, binding);

                foreach (ObjectReferenceKeyframe keyframe in keyframes)
                {
                    smokeKeyframeTimes.Add(keyframe.time);
                }
            }

            npcComponent.npc.smokeAnimPosData = new NPCSO.AnimEventPosData[smokePos.Count];
            for (int i = 0; i < npcComponent.npc.smokeAnimPosData.Length; i++)
            {
                npcComponent.npc.smokeAnimPosData[i].position = smokePos[i];
                npcComponent.npc.smokeAnimPosData[i].time = smokeKeyframeTimes[i];
            }
            EditorUtility.SetDirty(npcComponent.npc);
        }
        AssetDatabase.SaveAssets();
    }

    public void SetSpySpritePositionData()
    {
            TextureImporter atlasImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(spyComponent.spriteAtlas));
            atlasImporter.isReadable = true;
            atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(spyComponent.spriteAtlas);
            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();

            //for (int i = 0; i < spriteRects.Length; i++) // Looping through each sprite in atlas
            //{

            //    int startX = Mathf.RoundToInt(spriteRects[i].rect.x);
            //    int startY = Mathf.RoundToInt(spriteRects[i].rect.y);
            //    int width = Mathf.RoundToInt(spriteRects[i].rect.width);
            //    int height = Mathf.RoundToInt(spriteRects[i].rect.height);
            //    bool foundRed = false;

            //    for (int x = startX; x < startX + width; x++)
            //    {
            //        for (int y = startY; y < startY + height; y++)
            //        {
            //            Color color = npcComponent.spriteAtlas.GetPixel(x, y);

            //            if (color == Color.red)
            //            {
            //                float xPiv = (x - startX) / (float)width;
            //                float yPiv = (y - startY) / (float)height;

            //                spriteRects[i].pivot = new Vector2(xPiv, yPiv);
            //                spriteRects[i].alignment = SpriteAlignment.Custom;

            //                foundRed = true;
            //            }
            //        }
            //    }

            //    if (!foundRed) Debug.LogWarning($"never found red pixel origin point of: {spriteRects[i].name}");
            //}

            //dataProvider.SetSpriteRects(spriteRects);
            //dataProvider.Apply();

        for (int i = 0; i < spriteRects.Length; i++) // Looping through each sprite in atlas
        {
            int startX = Mathf.RoundToInt(spriteRects[i].rect.x);
            int startY = Mathf.RoundToInt(spriteRects[i].rect.y);
            int width = Mathf.RoundToInt(spriteRects[i].rect.width);
            int height = Mathf.RoundToInt(spriteRects[i].rect.height);

            float pivotPixelX = startX + spriteRects[i].pivot.x * width;
            float pivotPixelY = startY + spriteRects[i].pivot.y * height;

            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    Color color = spyComponent.spriteAtlas.GetPixel(x, y);

                    if (color == Color.blue)
                    {
                        float localPixelX = x - pivotPixelX;
                        float localPixelY = y - pivotPixelY;

                        float metersXPos = localPixelX / atlasImporter.spritePixelsPerUnit;
                        float metersYPos = localPixelY / atlasImporter.spritePixelsPerUnit;

                        spyComponent.stats.phonePosition = new Vector2(metersXPos, metersYPos);
                        EditorUtility.SetDirty(spyComponent.stats);
                        AssetDatabase.SaveAssets();
                        break;
                    }
                }
            }
        }
    }
}

[CustomEditor(typeof(SpriteAtlasFactory))]
public class SpriteAtlasFactorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteAtlasFactory factory = (SpriteAtlasFactory)target;

        if (GUILayout.Button("Set NPC Position Data"))
        {
            factory.SetNPCSpritePositionData();
        }

        if (GUILayout.Button("Set Spy Position Data"))
        {
            factory.SetSpySpritePositionData();
        }
    }
}

