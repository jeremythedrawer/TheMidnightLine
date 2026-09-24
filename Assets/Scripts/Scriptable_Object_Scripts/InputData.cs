using System;
using UnityEngine;
using UnityEngine.InputSystem;

using static UnityEngine.InputSystem.InputAction;

using static AtlasUI;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class InputData : ScriptableObject
{
    [Serializable] public class InputTrigger
    {
        public bool keyDown;
        public bool keyHold;
        public bool keyUp;

        public KeybindSpriteIndex keybindSpriteIndex;

        public void OnStart(CallbackContext ctx)
        {
            keyDown = true;
            keyHold = true;
        }
        public void OnCancel(CallbackContext ctx)
        {
            keyUp = true;
            keyHold = false;
        }
        public void SetTriggerKeybindSpriteIndex(InputAction action)
        {
            string path = action.bindings[0].effectivePath;

            string keyName = path.Replace("<Keyboard>/", "").ToLower();

            if (keyName.StartsWith("digit"))
            {
                keybindSpriteIndex = (KeybindSpriteIndex)int.Parse(keyName.Substring(startIndex: 5));
            }
            else
            {
                int index = 10 + (char.ToUpper(keyName[0]) - 'A');
                keybindSpriteIndex = (KeybindSpriteIndex)index;
            }
        }
    }
    [Serializable] public class InputAxis
    {
        public int keyDownValue;
        public int keyUpValue;
        public int keyHoldValue;

        public KeybindSpriteIndex postiveKeybindSpriteIndex;
        public KeybindSpriteIndex negativeKeybindSpriteIndex;
        
        public void OnStart(CallbackContext ctx)
        {
            float value = ctx.ReadValue<float>();
            keyDownValue = (int)value;
            keyHoldValue = (int)value;
        }

        public void OnCancel(CallbackContext ctx)
        {
            keyUpValue = keyHoldValue;
            keyHoldValue = 0;
        }
    }

    public InputAxis horizontalInputAxis;
    public InputAxis verticalInputAxis;

    public InputTrigger primaryInteractInputTrigger;
    public InputTrigger secondaryInteractInputTrigger;
    public InputTrigger focusInputTrigger;


    public int numpad;
    public bool mouseLeftDown;
    public bool mouseLeftHold;
    public bool mouseLeftUp;

    public Vector3 mouseScreenPos;
    public Vector3 mouseWorldPos;

}
