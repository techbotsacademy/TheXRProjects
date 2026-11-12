
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

[RequireComponent(typeof(WhisperNative))]
public class WhisperMicrophoneController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private int recordingFrequency = 16000;
    [SerializeField] private float silenceDuration = 0.8f;
    [SerializeField] private float speechThreshold = 0.012f;
    [SerializeField] private float maximumSpeechDuration = 8f;
    [SerializeField] private float minimumSpeechDuration = 0.4f;

    private const int WhisperSampleRate = 16000;

    private WhisperNative whisper;
    private RedCubeVoiceController cubeController;

    private AudioClip microphoneClip;
    private string microphoneDevice;

    private int lastPosition;
    private int microphoneChannels;
    private int actualSampleRate;

    private readonly List<float> speechSamples = new List<float>();

    private bool isRecordingSpeech;
    private bool isTranscribing;

    private float silenceTimer;
    private float speechTimer;

    private Task<string> transcriptionTask;

    private void Start()
    {
        whisper = GetComponent<WhisperNative>();
        cubeController = GetComponent<RedCubeVoiceController>();

        StartCoroutine(StartListening());
    }

    private IEnumerator StartListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);

            float permissionWait = 0f;

            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone)
                   && permissionWait < 15f)
            {
                permissionWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogError("Microphone permission was denied.");
                yield break;
            }
        }
#endif

        float waitTime = 0f;

        while (!whisper.IsInitialized && waitTime < 180f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!whisper.IsInitialized)
        {
            Debug.LogError("Whisper is not initialized.");
            yield break;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected.");
            yield break;
        }

        microphoneDevice = null;

        microphoneClip = Microphone.Start(
            microphoneDevice,
            true,
            10,
            recordingFrequency
        );

        if (microphoneClip == null)
        {
            Debug.LogError("Could not start microphone.");
            yield break;
        }

        microphoneChannels = microphoneClip.channels;
        actualSampleRate = microphoneClip.frequency;

        if (actualSampleRate <= 0 || microphoneChannels <= 0)
        {
            Debug.LogError("Invalid microphone audio format.");
            yield break;
        }

        Debug.Log("Microphone frequency: " + actualSampleRate);
        Debug.Log("Microphone channels: " + microphoneChannels);

        while (Microphone.GetPosition(microphoneDevice) <= 0)
            yield return null;

        lastPosition = Microphone.GetPosition(microphoneDevice);

        Debug.Log("WHISPER MICROPHONE LISTENING!");
    }

    private void Update()
    {
        if (microphoneClip == null)
            return;

        if (isTranscribing)
        {
            CheckTranscription();
            return;
        }

        CaptureMicrophoneAudio();
    }

    private void CaptureMicrophoneAudio()
    {
        int currentPosition = Microphone.GetPosition(microphoneDevice);

        if (currentPosition < 0)
            return;

        int availableSamples =
            (currentPosition - lastPosition + microphoneClip.samples)
            % microphoneClip.samples;

        if (availableSamples == 0)
            return;

        float[] audioData =
            new float[availableSamples * microphoneChannels];

        int firstPart = Mathf.Min(
            availableSamples,
            microphoneClip.samples - lastPosition
        );

        ReadMicrophonePart(audioData, lastPosition, firstPart, 0);

        int remaining = availableSamples - firstPart;

        if (remaining > 0)
        {
            ReadMicrophonePart(
                audioData,
                0,
                remaining,
                firstPart * microphoneChannels
            );
        }

        lastPosition = currentPosition;

        ProcessAudio(audioData);
    }

    private void ReadMicrophonePart(
        float[] destination,
        int startSample,
        int sampleCount,
        int destinationOffset)
    {
        float[] part = new float[sampleCount * microphoneChannels];

        if (!microphoneClip.GetData(part, startSample))
            return;

        Array.Copy(
            part,
            0,
            destination,
            destinationOffset,
            part.Length
        );
    }

    private void ProcessAudio(float[] audioData)
    {
        int frames = audioData.Length / microphoneChannels;

        float[] monoAudio = new float[frames];

        float sumSquares = 0f;

        for (int i = 0; i < frames; i++)
        {
            float sample = 0f;

            for (int channel = 0; channel < microphoneChannels; channel++)
            {
                sample += audioData[
                    i * microphoneChannels + channel
                ];
            }

            sample /= microphoneChannels;

            monoAudio[i] = sample;
            sumSquares += sample * sample;
        }

        float rms = Mathf.Sqrt(sumSquares / Mathf.Max(1, frames));

        float duration = (float)frames / actualSampleRate;

        if (rms >= speechThreshold)
        {
            isRecordingSpeech = true;
            silenceTimer = 0f;
        }
        else if (isRecordingSpeech)
        {
            silenceTimer += duration;
        }

        if (isRecordingSpeech)
        {
            speechSamples.AddRange(monoAudio);

            speechTimer += duration;

            if (silenceTimer >= silenceDuration ||
                speechTimer >= maximumSpeechDuration)
            {
                FinishSpeech();
            }
        }
    }

    private void FinishSpeech()
    {
        isRecordingSpeech = false;

        if (speechTimer < minimumSpeechDuration)
        {
            ResetSpeech();
            return;
        }

        float[] completedAudio = speechSamples.ToArray();

        ResetSpeech();

        // Convert microphone audio to Whisper's required 16 kHz.
        float[] whisperAudio = ResampleAudio(
            completedAudio,
            actualSampleRate,
            WhisperSampleRate
        );

        Debug.Log("Sending " + whisperAudio.Length +
                  " audio samples to Whisper.");

        isTranscribing = true;

        transcriptionTask = Task.Run(
            () => whisper.Transcribe(whisperAudio)
        );
    }

    private float[] ResampleAudio(
        float[] input,
        int sourceRate,
        int targetRate)
    {
        if (sourceRate == targetRate)
            return input;

        int outputLength = Mathf.Max(
            1,
            Mathf.RoundToInt(
                input.Length * (float)targetRate / sourceRate
            )
        );

        float[] output = new float[outputLength];

        float ratio = (float)sourceRate / targetRate;

        for (int i = 0; i < outputLength; i++)
        {
            float sourcePosition = i * ratio;

            int index = Mathf.FloorToInt(sourcePosition);
            int nextIndex = Mathf.Min(index + 1, input.Length - 1);

            float fraction = sourcePosition - index;

            output[i] = Mathf.Lerp(
                input[Mathf.Min(index, input.Length - 1)],
                input[nextIndex],
                fraction
            );
        }

        return output;
    }

    private void CheckTranscription()
    {
        if (transcriptionTask == null ||
            !transcriptionTask.IsCompleted)
            return;

        isTranscribing = false;

        if (transcriptionTask.IsFaulted)
        {
            Debug.LogError(
                "Whisper transcription error: " +
                transcriptionTask.Exception
            );

            transcriptionTask = null;
            return;
        }

        string recognizedText = transcriptionTask.Result;

        transcriptionTask = null;

        if (string.IsNullOrWhiteSpace(recognizedText))
            return;

        recognizedText = recognizedText.Trim();

        Debug.Log("Recognized speech: " + recognizedText);

        if (cubeController != null)
        {
            cubeController.ProcessCommand(
                recognizedText.ToLowerInvariant()
            );
        }
        else
        {
            Debug.LogError("RedCubeVoiceController not found.");
        }
    }

    private void ResetSpeech()
    {
        speechSamples.Clear();
        speechTimer = 0f;
        silenceTimer = 0f;
    }

    private void OnDestroy()
    {
        if (microphoneClip != null &&
            Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
    }
}
