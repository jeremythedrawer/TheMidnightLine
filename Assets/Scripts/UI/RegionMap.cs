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



        for (int i = 0; i < tripButtons.Length; i++)
        {
            int index = i;
            void MouseUp()
            {
                TripButton tripButton = tripButtons[index];

                options.curTrip = tripButton.trip;
                options.curTrip.passengersTalkToSinceLastStation = 0;
                options.curTrip.passengersTalkToTotal = 0;
                options.curTrip.traitorsSpawned = 0;
                tripButton.button.atlasRenderer.customBit ^= (int)ColorBits.Invert;

                camData.curLocationState = Spy.LocationState.Title;
                onBeginTrip?.Raise();
            }
            void EnterButton()
            {
                TripButton tripButton = tripButtons[index];

                tripButton.button.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
                tripButton.button.atlasRenderer.customBit &= ~(int)ColorBits.BlueChannel;
            }
            void ExitButton()
            {
                TripButton tripButton = tripButtons[index];

                tripButton.button.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
                tripButton.button.atlasRenderer.customBit |= (int)ColorBits.BlueChannel;
                tripButton.button.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
            }
            TripButton regionButton = tripButtons[i];
            regionButton.button.InitButton(onMouseUp: MouseUp, onEnter: EnterButton, onExit: ExitButton);
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
