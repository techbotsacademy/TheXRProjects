
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceCommandTester : MonoBehaviour
{
    public RedCubeVoiceController voiceController;

    void Update()
    {
        if (Keyboard.current == null || voiceController == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SendCommand("move forward");

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SendCommand("move backward");

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SendCommand("move left");

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SendCommand("move right");

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            SendCommand("jump");

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SendCommand("rotate left");

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            SendCommand("rotate right");

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
            SendCommand("stop");

        if (Keyboard.current.digit9Key.wasPressedThisFrame)
            SendCommand("reset");
    }

    void SendCommand(string command)
    {
        Debug.Log("Testing voice command: " + command);
        voiceController.ProcessCommand(command);
    }
}
