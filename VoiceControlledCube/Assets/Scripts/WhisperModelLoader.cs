
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperModelLoader : MonoBehaviour
{
    [SerializeField] private WhisperNative whisperNative;

    private const string ModelFileName = "ggml-tiny.en.bin";

    private IEnumerator Start()
    {
        if (whisperNative == null)
            whisperNative = GetComponent<WhisperNative>();

        if (whisperNative == null)
        {
            Debug.LogError("WhisperNative component not found!");
            yield break;
        }

        string destinationPath = Path.Combine(
            Application.persistentDataPath,
            ModelFileName
        );

        // Copy the model only if it is not already available.
        if (!File.Exists(destinationPath))
        {
            string sourcePath = Path.Combine(
                Application.streamingAssetsPath,
                "Models/" + ModelFileName
            );

            Debug.Log("Copying Whisper model...");

            using (UnityWebRequest request =
                   UnityWebRequest.Get(sourcePath))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        "Model copy failed: " + request.error
                    );
                    yield break;
                }

                try
                {
                    File.WriteAllBytes(
                        destinationPath,
                        request.downloadHandler.data
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Saving model failed: " + e.Message);
                    yield break;
                }
            }
        }

        Debug.Log("Whisper model available: " + destinationPath);

        // Initialize the native Whisper engine.
        Debug.Log("Initializing Whisper...");

        bool success = whisperNative.Initialize(destinationPath);

        if (success)
            Debug.Log("WHISPER READY!");
        else
            Debug.LogError("Whisper initialization failed.");
    }
}
