
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CubeMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 90f;
    public float jumpHeight = 0.5f;
    public float jumpDuration = 0.6f;

    private Vector3 startPosition;
    private bool isJumping = false;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // Movement
        if (Keyboard.current.wKey.isPressed)
            MoveForward();

        if (Keyboard.current.sKey.isPressed)
            MoveBackward();

        if (Keyboard.current.aKey.isPressed)
            MoveLeft();

        if (Keyboard.current.dKey.isPressed)
            MoveRight();

        // Rotation
        if (Keyboard.current.qKey.isPressed)
            RotateLeft();

        if (Keyboard.current.eKey.isPressed)
            RotateRight();

        // Jump
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            Jump();

        // Reset
        if (Keyboard.current.rKey.wasPressedThisFrame)
            ResetPosition();
    }

    public void MoveForward()
    {
        transform.position += Vector3.forward * moveSpeed * Time.deltaTime;
    }

    public void MoveBackward()
    {
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;
    }

    public void MoveLeft()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }

    public void MoveRight()
    {
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    public void RotateLeft()
    {
        transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
    }

    public void RotateRight()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void Jump()
    {
        if (!isJumping)
            StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;

        float elapsed = 0f;
        Vector3 initialPosition = transform.position;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;

            float height = Mathf.Sin(
                Mathf.Clamp01(elapsed / jumpDuration) * Mathf.PI
            ) * jumpHeight;

            transform.position = initialPosition + Vector3.up * height;

            yield return null;
        }

        transform.position = initialPosition;
        isJumping = false;
    }

    public void ResetPosition()
    {
        StopAllCoroutines();
        isJumping = false;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }
}
