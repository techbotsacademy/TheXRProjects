
using UnityEngine;
using System.Collections;

public class RedCubeVoiceController : MonoBehaviour
{
    [Header("Voice Command Settings")]
    public float moveSpeed = 1.5f;
    public float moveDistance = 1f;
    public float rotationSpeed = 90f;
    public float jumpHeight = 0.5f;
    public float jumpDuration = 0.6f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Coroutine activeAction;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    // This method will receive text from Whisper AI later.
    public void ProcessCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        command = command.ToLower().Trim();

        Debug.Log("Received command: " + command);

        if (command.Contains("stop"))
        {
            StopAction();
            return;
        }

        if (command.Contains("reset"))
        {
            StopAction();
            transform.position = startPosition;
            transform.rotation = startRotation;
            Debug.Log("Cube reset!");
            return;
        }

        if (command.Contains("jump"))
        {
            RunAction(JumpRoutine());
            return;
        }

        if (command.Contains("rotate") && command.Contains("left"))
        {
            RunAction(RotateRoutine(-90f));
            return;
        }

        if (command.Contains("rotate") && command.Contains("right"))
        {
            RunAction(RotateRoutine(90f));
            return;
        }

        if (command.Contains("forward"))
        {
            RunAction(MoveRoutine(Vector3.forward));
            return;
        }

        if (command.Contains("backward") ||
            command.Contains("back"))
        {
            RunAction(MoveRoutine(Vector3.back));
            return;
        }

        if (command.Contains("left"))
        {
            RunAction(MoveRoutine(Vector3.left));
            return;
        }

        if (command.Contains("right"))
        {
            RunAction(MoveRoutine(Vector3.right));
            return;
        }

        Debug.LogWarning("Command not recognized: " + command);
    }

    void RunAction(IEnumerator action)
    {
        StopAction();
        activeAction = StartCoroutine(action);
    }

    public void StopAction()
    {
        if (activeAction != null)
        {
            StopCoroutine(activeAction);
            activeAction = null;
        }
    }

 IEnumerator MoveRoutine(Vector3 direction)
{
    Debug.Log("Movement started: " + direction);

    Vector3 targetPosition =
        transform.position + transform.TransformDirection(direction) * moveDistance;

    while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        yield return null;
    }

    transform.position = targetPosition;
    activeAction = null;

    Debug.Log("Movement completed!");
}

    IEnumerator RotateRoutine(float angle)
    {
        Quaternion initial = transform.rotation;
        Quaternion target = initial *
            Quaternion.Euler(0f, angle, 0f);

        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotationSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = target;
        activeAction = null;
    }

    IEnumerator JumpRoutine()
    {
        Vector3 initialPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;

            float height = Mathf.Sin(
                Mathf.Clamp01(elapsed / jumpDuration) * Mathf.PI
            ) * jumpHeight;

            transform.position =
                initialPosition + Vector3.up * height;

            yield return null;
        }

        transform.position = initialPosition;
        activeAction = null;
    }
}
