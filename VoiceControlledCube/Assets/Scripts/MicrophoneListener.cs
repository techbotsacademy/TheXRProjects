
using UnityEngine;
using System.Collections;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class MicrophoneListener : MonoBehaviour
{
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private bool isListening = false;

    public int recordingFrequency = 16000;
    public int recordingLength = 10;

    void Start()
    {
        StartCoroutine(StartMicrophone());
    }

    IEnumerator StartMicrophone()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(
            Permission.Microphone))
        {
            Permission.RequestUserPermission(
                Permission.Microphone);

            yield return new WaitForSeconds(2f);
        }

        if (!Permission.HasUserAuthorizedPermission(
            Permission.Microphone))
        {
            Debug.LogError("Microphone permission denied!");
            yield break;
        }
        #endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            yield break;
        }

        microphoneDevice = Microphone.devices[0];

        microphoneClip = Microphone.Start(
            microphoneDevice,
            true,
            recordingLength,
            recordingFrequency
        );

        while (Microphone.GetPosition(microphoneDevice) <= 0)
        {
            yield return null;
        }

        isListening = true;

        Debug.Log("Microphone started successfully!");
        Debug.Log("Listening to: " + microphoneDevice);
    }

    void OnDisable()
    {
        if (isListening)
        {
            Microphone.End(microphoneDevice);
            isListening = false;
        }
    }
}
