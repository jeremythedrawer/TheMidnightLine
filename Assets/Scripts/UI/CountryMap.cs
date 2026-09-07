using System;
using UnityEngine;
using static AtlasUI;
using static ColorPicker;

public class CountryMap : MonoBehaviour
{
    public Options options;
    
    public IconButton firstPointButton;
    public IconButton secondPointButton;
    public IconButton thirdPointButton;
    public IconButton capitalRegionButton;

    public IconButton backButton;

    public AtlasRenderer meridiaMapRenderer;

    public RegionMap thirdPointMap;
    

    [Header("Generated")]
    public RegionMap curMap;
    
    private void Start()
    {
        InitButtons();
        InitMaps();
    }
    private void InitButtons()
    {
        void EnterButtonThirdPoint()
        {
            thirdPointButton.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
            thirdPointButton.atlasRenderer.customBit &= ~(int)ColorBits.BlueChannel;
        }
        void ExitButtonThirdPoint()
        {
            thirdPointButton.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            thirdPointButton.atlasRenderer.customBit |= (int)ColorBits.BlueChannel;
            thirdPointButton.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
        }

        void MouseUpThirdPoint()
        {
            thirdPointButton.MouseUp();

            meridiaMapRenderer.gameObject.SetActive(false);
            thirdPointMap.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
            options.curRegion = options.thirdPointRegion;
            curMap = thirdPointMap;
        }

        void MouseUpBackButton()
        {
            backButton.MouseUp();

            meridiaMapRenderer.gameObject.SetActive(true);
            curMap.gameObject.SetActive(false);
            curMap = null;
            backButton.gameObject.SetActive(false);
        }
        thirdPointButton.InitButton(onMouseUp: MouseUpThirdPoint, onEnter: EnterButtonThirdPoint, onExit: ExitButtonThirdPoint);
        backButton.InitButton(onMouseUp: MouseUpBackButton);
    }
    private void InitMaps()
    {
        thirdPointMap.gameObject.SetActive(false);
    }
    public void UpdateButtons()
    {
        if (curMap == null)
        {
            if (options.firstPointRegion.unlocked) firstPointButton.UpdateButton();
            if (options.secondPointRegion.unlocked) secondPointButton.UpdateButton();
            if (options.thirdPointRegion.unlocked) thirdPointButton.UpdateButton();
            if (options.capitalRegion.unlocked) capitalRegionButton.UpdateButton();
        }
        else
        {
            backButton.UpdateButton();
        }
    }
}
