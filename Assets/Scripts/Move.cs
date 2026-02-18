using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    public float speed = 5f;
    public bool canRotate = true; // 스킬 사용 시 회전 제한을 위해 추가
    private Vector2 moveInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Vector2 movement = moveInput.normalized;
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (!canRotate) return;

        Mouse ms = Mouse.current;
        Camera cam = Camera.main;

        if (ms != null && cam != null && ms.position != null)
        {
            Vector3 mouseScreenPos = ms.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            Vector2 direction = (mouseWorldPos - transform.position);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
