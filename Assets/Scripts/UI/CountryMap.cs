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
        void EnterButton(IconButton icon)
        {
            icon.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
            icon.atlasRenderer.customBit &= ~(int)ColorBits.BlueChannel;
        }
        void ExitButton(IconButton icon)
        {
            icon.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            icon.atlasRenderer.customBit |= (int)ColorBits.BlueChannel;
            icon.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
        }

        void MouseDown(IconButton icon)
        {
            icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;
        }

        void MouseUpBase(IconButton icon)
        {
            icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;
        }

        void MouseUpThirdPoint(IconButton icon)
        {
            MouseUpBase(icon);

            meridiaMapRenderer.gameObject.SetActive(false);
            thirdPointMap.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
            options.curRegion = options.thirdPointRegion;
            curMap = thirdPointMap;
        }

        void MouseUpBackButton(IconButton icon)
        {
            meridiaMapRenderer.gameObject.SetActive(true);
            curMap.gameObject.SetActive(false);
            curMap = null;
            backButton.gameObject.SetActive(false);
        }
        firstPointButton.InitButton(MouseUpThirdPoint, MouseDown, EnterButton, ExitButton);
        secondPointButton.InitButton(MouseUpThirdPoint, MouseDown, EnterButton, ExitButton);
        thirdPointButton.InitButton(MouseUpThirdPoint, MouseDown, EnterButton, ExitButton);
        capitalRegionButton.InitButton(MouseUpThirdPoint, MouseDown, EnterButton, ExitButton);

        backButton.InitButton(MouseUpBackButton, MouseDown, EnterButton, ExitButton);
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
