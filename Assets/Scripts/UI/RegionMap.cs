using System;
using UnityEngine;
using static AtlasUI;

public class RegionMap : MonoBehaviour
{
    public static event Action OnStartTrip;
    [Serializable] public struct TripButton
    {
        public IconButton button;
        public TripData trip;
    }

    public Options options;

    public TripButton[] tripButtons;

    public void Start()
    {
        InitButtons();
        UpdateUnlocks();
    }
    private void Update()
    {
        UpdateButtons();
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

        for (int i = 0; i < tripButtons.Length; i++)
        {
            int index = i;
            void MouseUp(IconButton icon)
            {
                TripButton button = tripButtons[index];
                options.curTrip = button.trip;
                OnStartTrip?.Invoke();
                icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;
            }

            TripButton regionButton = tripButtons[i];
            regionButton.button.InitButton(MouseUp, MouseDown, EnterButton, ExitButton);
        }
    }
    private void UpdateUnlocks()
    {
        for (int i = 0; i < tripButtons.Length; i++)
        {
            TripButton regionButton = tripButtons[i];
            if (regionButton.trip.unlocked)
            {
                regionButton.button.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
            }
        }
    }
    private void UpdateButtons()
    {
        for (int i = 0;i < tripButtons.Length;i++)
        {
            TripButton regionButton = tripButtons[i];
            if (!regionButton.trip.unlocked) continue;
            regionButton.button.UpdateButton();
        }
    }
}
