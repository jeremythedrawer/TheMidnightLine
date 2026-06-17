using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
public class ColorPicker : MonoBehaviour
{
    public const float OPEN_TIME = 1;

    public ColorsSO colorsSO;
    public PlayerInputsSO playerInputs;
    public AtlasRenderer[] colorRenderers;
    public AtlasRenderer palletteRenderer;
    [Header("Generated")]
    public AtlasRenderer selectedRenderer;

    public Vector2[] openColorRendererPositions;

    public float openClock;
    public float openSpriteWidth;
    public float openSpriteHeight;
    public float curSpriteWidth;
    public float curSpriteHeight;

    public Vector3 openWorldSize;
    public Vector3 openWorldPos;
    public Vector3 closeWorldPos;
    public Vector3 curWorldPos;
    public Vector3 closeColorRendererPosition;

    public bool isHovering;
    public bool entered;
    public bool finishedOpening;
    public bool finishedClosing;
    public bool isClosed;

    private void Start()
    {
        SetSelectableColors();
        SetOpenPosAndSize();
        ToggleColorPicker(false);
        Shader.SetGlobalColor("_BlackColor", colorsSO.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsSO.whiteColor);
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);
    }
    private void Update()
    {
#if UNITY_EDITOR
        SetSelectableColors();      
        Shader.SetGlobalColor("_BlackColor", colorsSO.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsSO.whiteColor);
        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);

        isClosed = finishedClosing;

#endif
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
    }
    private void SetSelectableColors()
    {
        for (int i = 0; i < colorRenderers.Length; i++)
        {
            AtlasRenderer colorRenderer = colorRenderers[i];
            colorRenderer.custom = colorsSO.selectableColors[i].linear;
        }
    }
    public void SetOpenPosAndSize()
    {
        openSpriteWidth = palletteRenderer.width;
        openWorldSize = palletteRenderer.GetBounds().size;
        openSpriteHeight = palletteRenderer.height;

        openColorRendererPositions = new Vector2[colorRenderers.Length];
        
        for (int i = 0; i < openColorRendererPositions.Length; i++)
        {
            openColorRendererPositions[i] = colorRenderers[i].transform.localPosition;
        }

        AtlasRenderer lastColorRenderer = colorRenderers[colorRenderers.Length - 1];
        closeColorRendererPosition = new Vector3(0, lastColorRenderer.transform.localPosition.y, lastColorRenderer.transform.localPosition.z);

        closeWorldPos.z = palletteRenderer.transform.position.z;
        openWorldPos.z = palletteRenderer.transform.position.z;
        curWorldPos.z = palletteRenderer.transform.position.z;
    }
    public void ToggleColorPicker(bool toggle, AtlasRenderer renderer = null)
    {
        palletteRenderer.enabled = toggle;
        for (int i = 0;i < colorRenderers.Length; i++)
        {
            colorRenderers[i].enabled = toggle;
        }
        if (renderer != null)
        {
            selectedRenderer = renderer;

            Bounds selectedRendBounds = selectedRenderer.GetBounds();

            closeWorldPos.x = selectedRendBounds.min.x;
            closeWorldPos.y = selectedRendBounds.max.y;

            openWorldPos.x = closeWorldPos.x - openWorldSize.x;
            openWorldPos.y = closeWorldPos.y;
            curWorldPos.y = closeWorldPos.y;

            palletteRenderer.transform.localPosition = curWorldPos;
        }
        else
        {
            selectedRenderer = null;
        }

    }
    public void UpdateOpen(AtlasRenderer rend)
    {
        if (CursorController.EnteredBounds(rend))
        {
            ToggleColorPicker(true, rend);
            entered = true;
            finishedOpening = false;
        }
        else if (openClock < OPEN_TIME)
        {
            isHovering = true;
            entered = false;
            openClock += Time.deltaTime;
            float t = openClock / OPEN_TIME;
            if (t < 0.5)
            {
                float easeOutT = 1 - Mathf.Pow(1 - t * 2, 5);
                curSpriteWidth = openSpriteWidth * easeOutT;
                curWorldPos.x = Mathf.Lerp(closeWorldPos.x, openWorldPos.x, easeOutT);

                palletteRenderer.transform.position = curWorldPos;
                palletteRenderer.width = curSpriteWidth;
                palletteRenderer.UpdateSliceSpriteInputsSelf();
                for (int i = 0; i < colorRenderers.Length; i++)
                {
                    float posX = Mathf.Lerp(closeColorRendererPosition.x, openColorRendererPositions[i].x, easeOutT);
                    colorRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
                }
            }
            else
            {
                float easOutT = 1 - Mathf.Pow(1 - (t - 0.5f) * 2, 5);
                curSpriteHeight = Mathf.Lerp(0.5f, openSpriteHeight, easOutT);
                palletteRenderer.height = curSpriteHeight;
                palletteRenderer.UpdateSliceSpriteInputsSelf();

                for (int i = 0; i < colorRenderers.Length; i++)
                {
                    float posY = Mathf.Lerp(closeColorRendererPosition.y, openColorRendererPositions[i].y, easOutT);
                    colorRenderers[i].transform.localPosition = new Vector3(openColorRendererPositions[i].x, posY, closeColorRendererPosition.z);
                }
            }
        }
        else if (openClock >= OPEN_TIME && !finishedOpening)
        {
            finishedOpening = true;
        }
    }
    public void SetNewColor()
    {
        if (playerInputs.mouseLeftDown)
        {
            if (selectedRenderer == null) return;
            for (int i = 0; i < colorRenderers.Length; i++)
            {
                AtlasRenderer colorRenderer = colorRenderers[i];
                if (CursorController.IsInsideBounds(colorRenderer.GetBounds()))
                {
                    Color selectedColor = colorsSO.selectableColors[i].linear;
                    selectedRenderer.custom = selectedColor;
                    Shader.SetGlobalColor("_ColorKey" + i, selectedColor);
                    break;
                }
            }
        }
    }
    public void UpdateClose()
    {
        if (CursorController.ExitBounds() && openClock >= OPEN_TIME)
        {
            isHovering = false;
        }
        else if (openClock > 0)
        {
            openClock -= Time.deltaTime;
            float t = openClock / OPEN_TIME;

            if (t < 0.5)
            {
                float easeOutT = Mathf.Max(1 - Mathf.Pow(1 - t * 2, 5), 0);
                curSpriteWidth = openSpriteWidth * easeOutT;
                curWorldPos.x = Mathf.Lerp(closeWorldPos.x, openWorldPos.x, easeOutT);

                palletteRenderer.transform.localPosition = curWorldPos;
                palletteRenderer.width = curSpriteWidth;
                palletteRenderer.UpdateSliceSpriteInputsSelf();
                for (int i = 0; i < colorRenderers.Length; i++)
                {
                    float posX = Mathf.Lerp(closeColorRendererPosition.x, openColorRendererPositions[i].x, easeOutT);
                    colorRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
                }
            }
            else
            {
                float easOutT = Mathf.Max(1 - Mathf.Pow(1 - (t - 0.5f) * 2, 5), 0);
                curSpriteHeight = Mathf.Lerp(0.5f, openSpriteHeight, easOutT);
                palletteRenderer.height = curSpriteHeight;
                palletteRenderer.UpdateSliceSpriteInputsSelf();

                for (int i = 0; i < colorRenderers.Length; i++)
                {
                    float posY = Mathf.Lerp(closeColorRendererPosition.y, openColorRendererPositions[i].y, easOutT);
                    colorRenderers[i].transform.localPosition = new Vector3(openColorRendererPositions[i].x, posY, closeColorRendererPosition.z);
                }
            }
        }
        else
        {
            if (finishedClosing)
            {
                finishedClosing = false;
                ToggleColorPicker(false);
            }
            else
            {
                finishedClosing = true;
            }
        }
    }
}
