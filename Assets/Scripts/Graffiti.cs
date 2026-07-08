using System;
using UnityEngine;

public class Graffiti : MonoBehaviour
{
    public const float LIFETIME = 60;

    public AtlasRenderer[] renderers;
    public AtlasSO atlas;
    public bool active;
    public float clock;

    private void Update()
    {
        if (!active) return;
        clock -= Time.deltaTime;
        float t = clock / LIFETIME;

        for(int i = 0; i < renderers.Length; i++)
        {
            AtlasRenderer renderer = renderers[i];
            renderer.custom.x = t;
        }

        if (clock <= 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                AtlasRenderer renderer = renderers[i];
                renderer.enabled = false;
            }
            clock = 0;
            active = false;
            enabled = false;
            NPCManager.ReturnGraffiti(this);
        }
    }
    public void SetSprites(int index)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            AtlasRenderer renderer = renderers[i];
            renderer.enabled = true;
            renderer.UpdateSpriteInputsByIndex(index);
            renderer.custom.x = 0;
        }
    }
    public void UpdateAlpha(float alpha)
    {
        if (active) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            AtlasRenderer renderer = renderers[i];
            renderer.custom.x = alpha;
        }
        if (alpha > 0.9f)
        {
            active = true;
            clock = LIFETIME;
        }
    }
}
