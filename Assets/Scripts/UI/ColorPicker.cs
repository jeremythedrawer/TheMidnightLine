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
    public float openWidth;
    public float openHeight;
    public Vector3 openLocalPos;
    public Vector3 closeLocalPos;
    public Vector3 curLocalPos;

    public float curWidth;
    public float curHeight;

    public Vector3 closeColorRendererPosition;

    public static bool IsHoveringColorPicker;
    public static bool EnteredColorPicker;
    public static bool ClosedColorPicker;

    public bool isHovering;
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

        isHovering = IsHoveringColorPicker;
        isClosed = ClosedColorPicker;

#endif
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
    }
    private void SetSelectableColors()
    {
        for (int i = 0; i < colorRenderers.Length; i++)
        {
            AtlasRenderer colorRenderer = colorRenderers[i];

            for (int j = 0; j < colorRenderer.customs.Length; j++)
            {
                colorRenderer.customs[j] = colorsSO.selectableColors[i].linear;
            }
        }
    }
    public void SetOpenPosAndSize()
    {
        openWidth = palletteRenderer.width;
        openHeight = palletteRenderer.height;
        openLocalPos.x = palletteRenderer.transform.localPosition.x;

        openColorRendererPositions = new Vector2[colorRenderers.Length];
        
        for (int i = 0; i < openColorRendererPositions.Length; i++)
        {
            openColorRendererPositions[i] = colorRenderers[i].transform.localPosition;
        }

        AtlasRenderer lastColorRenderer = colorRenderers[colorRenderers.Length - 1];
        closeColorRendererPosition = new Vector3(0, lastColorRenderer.transform.localPosition.y, lastColorRenderer.transform.localPosition.z);

        closeLocalPos.z = palletteRenderer.transform.localPosition.z;
        openLocalPos.z = palletteRenderer.transform.localPosition.z;
        curLocalPos.z = palletteRenderer.transform.localPosition.z;
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
            Vector3 paletteBoundsLocalPos = palletteRenderer.transform.parent.InverseTransformPoint(selectedRenderer.bounds.min);

            closeLocalPos.x = paletteBoundsLocalPos.x;
            closeLocalPos.y = paletteBoundsLocalPos.y; 

            openLocalPos.y = paletteBoundsLocalPos.y;
            curLocalPos.y = paletteBoundsLocalPos.y;
            palletteRenderer.transform.localPosition = curLocalPos;
        }
        else
        {
            selectedRenderer = null;
        }

    }
    public void OpenColorPicker(AtlasRenderer rend)
    {
        if (CursorController.EnteredBounds(rend))
        {
            ToggleColorPicker(true, rend);
            EnteredColorPicker = true;
        }
        else if (selectedRenderer != null && (CursorController.IsInsideBounds(selectedRenderer.bounds) || CursorController.IsInsideBounds(palletteRenderer.GetBounds())) && openClock < OPEN_TIME)
        {
            IsHoveringColorPicker = true;
            EnteredColorPicker = false;
            openClock += Time.deltaTime;
            float t = openClock / OPEN_TIME;
            if (t < 0.5)
            {
                float easeOutT = 1 - Mathf.Pow(1 - t * 2, 5);
                curWidth = openWidth * easeOutT;
                curLocalPos.x = Mathf.Lerp(closeLocalPos.x, openLocalPos.x, easeOutT);

                palletteRenderer.transform.localPosition = curLocalPos;
                palletteRenderer.width = curWidth;
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
                curHeight = openHeight * easOutT;
                palletteRenderer.height = curHeight;
                palletteRenderer.UpdateSliceSpriteInputsSelf();

                for (int i = 0; i < colorRenderers.Length; i++)
                {
                    float posY = Mathf.Lerp(closeColorRendererPosition.y, openColorRendererPositions[i].y, easOutT);
                    colorRenderers[i].transform.localPosition = new Vector3(openColorRendererPositions[i].x, posY, closeColorRendererPosition.z);
                }
            }
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
                    for (int j = 0; j < selectedRenderer.customs.Length; j++)
                    {
                        selectedRenderer.customs[j] = selectedColor;
                    }
                    Shader.SetGlobalColor("__ColorKey" + i, selectedColor);
                    break;
                }
            }
        }
    }
    public void CloseColorPicker()
    {
        if (CursorController.ExitBounds() && openClock >= OPEN_TIME)
        {
            IsHoveringColorPicker = false;
        }
        else if (selectedRenderer != null && !CursorController.IsInsideBounds(selectedRenderer.bounds) && !CursorController.IsInsideBounds(palletteRenderer.GetBounds()))
        {
            IsHoveringColorPicker = false;
            if (openClock > 0)
            {
                openClock -= Time.deltaTime;
                float t = openClock / OPEN_TIME;

                if (t < 0.5)
                {
                    float easeOutT = Mathf.Max(1 - Mathf.Pow(1 - t * 2, 5), 0);
                    curWidth = openWidth * easeOutT;
                    curLocalPos.x = Mathf.Lerp(closeLocalPos.x, openLocalPos.x, easeOutT);

                    palletteRenderer.transform.localPosition = curLocalPos;
                    palletteRenderer.width = curWidth;
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
                    curHeight = openHeight * easOutT;
                    palletteRenderer.height = curHeight;
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
                if (ClosedColorPicker)
                {
                    ClosedColorPicker = false;
                    ToggleColorPicker(false);
                }
                else
                {
                    ClosedColorPicker = true;
                }
            }
        }
    }
}
