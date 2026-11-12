
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class WhisperNative : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string LibraryName = "whisper_unity";
#else
    private const string LibraryName = "whisper_unity";
#endif

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr whisper_unity_init(string modelPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr whisper_unity_transcribe(
        IntPtr context,
        float[] audio,
        int audioLength,
        int numThreads
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whisper_unity_release(IntPtr context);

    private IntPtr whisperContext = IntPtr.Zero;

    public bool IsInitialized
    {
        get { return whisperContext != IntPtr.Zero; }
    }

    // Initialize Whisper with the model stored in Android app storage.
    public bool Initialize(string modelPath)
    {
        if (IsInitialized)
            return true;

        try
        {
            whisperContext = whisper_unity_init(modelPath);

            if (whisperContext == IntPtr.Zero)
            {
                Debug.LogError("Whisper initialization failed.");
                return false;
            }

            Debug.Log("Whisper initialized successfully.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper initialization error: " + e.Message);
            return false;
        }
    }

    // Transcribe 16 kHz mono audio samples.
    public string Transcribe(float[] audioSamples)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Whisper is not initialized.");
            return "";
        }

        if (audioSamples == null || audioSamples.Length == 0)
            return "";

        try
        {
            IntPtr result = whisper_unity_transcribe(
                whisperContext,
                audioSamples,
                audioSamples.Length,
                4
            );

            if (result == IntPtr.Zero)
                return "";

            return Marshal.PtrToStringAnsi(result);
        }
        catch (Exception e)
        {
            Debug.LogError("Transcription error: " + e.Message);
            return "";
        }
    }

    private void OnDestroy()
    {
        if (whisperContext != IntPtr.Zero)
        {
            whisper_unity_release(whisperContext);
            whisperContext = IntPtr.Zero;
        }
    }
}
