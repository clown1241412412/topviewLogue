using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public Transform weapon;
    public float swingDuration = 0.2f;
    public float swingAngle = 120f; // Total arc angle
    public float cooldown = 0.1f;
    public float guardAngle = 90f;

    private bool isAttacking = false;
    private bool isGuarding = false;
    private Quaternion initialRotation;

    void Start()
    {
        if (weapon == null)
        {
            weapon = transform.Find("word");
        }

        if (weapon != null)
        {
            initialRotation = weapon.localRotation;
        }
        else
        {
            Debug.LogError("Weapon 'word' not found! Please assign it in the Inspector or name the child 'word'.");
        }
    }

    void Update()
    {
        if (weapon == null) return;

        bool rightPressed = Mouse.current.rightButton.isPressed;

        if (rightPressed && !isAttacking)
        {
            isGuarding = true;
            weapon.localRotation = initialRotation * Quaternion.Euler(0, 0, guardAngle);
        }
        else if (!rightPressed && isGuarding)
        {
            isGuarding = false;
            weapon.localRotation = initialRotation;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking && !isGuarding && weapon != null)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        Quaternion startRot = initialRotation * Quaternion.Euler(0, 0, swingAngle / 2f);
        Quaternion endRot = initialRotation * Quaternion.Euler(0, 0, -swingAngle / 2f);

        float elapsedTime = 0f;

        while (elapsedTime < swingDuration)
        {
            float t = elapsedTime / swingDuration;
            // Smooth step for better feel
            t = t * t * (3f - 2f * t);

            // Interpolate rotation
            weapon.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap to end
        weapon.localRotation = endRot;

        // Return to initial rotation (or keep it there? usually return)
        // Let's create a quick return or just snap back after a frame
        yield return null; 
        weapon.localRotation = initialRotation;

        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }
}
