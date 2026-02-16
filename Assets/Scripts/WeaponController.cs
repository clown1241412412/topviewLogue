using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public int damage = 1;
    private BoxCollider2D col;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            // 초기 사이즈 설정 (스프라이트에 맞게 Inspector에서 조절 필요)
            col.size = new Vector2(0.5f, 2.5f); 
            col.offset = new Vector2(0, 1.25f);
        }
        col.enabled = false; // 평소엔 꺼둠
    }

    public void StartAttack()
    {
        hitEnemies.Clear();
        col.enabled = true;
        // Debug.Log("공격 시작! Collider 활성화됨");
    }

    public void EndAttack()
    {
        col.enabled = false;
        // Debug.Log("공격 종료! Collider 비활성화됨");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("무기 충돌 감지! 대상: " + other.name);

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null && !hitEnemies.Contains(other.gameObject))
        {
            hitEnemies.Add(other.gameObject);
            enemy.TakeDamage(damage);
        }
    }
}
