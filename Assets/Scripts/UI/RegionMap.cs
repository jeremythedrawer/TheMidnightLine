using Proselyte.Sigils;
using System;
using UnityEngine;
using static AtlasUI;
using static Passenger;

public class RegionMap : MonoBehaviour
{
    [Serializable] public struct TripButton
    {
        public IconButton button;
        public TripData trip;
    }

    public GameEvent onBeginTrip;
    public Options options;
    public CameraData camData;

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
                options.curTrip.ticketsCheckedSinceLastStation = 0;
                options.curTrip.ticketsCheckedTotal = 0;
                options.curTrip.traitorsSpawned = 0;
                icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;

                camData.curLocationState = Spy.LocationState.Title;
                onBeginTrip?.Raise();
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
