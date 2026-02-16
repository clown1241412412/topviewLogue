using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 3.5f;
    public float hitDistance = 0.35f;
    public int maxHP = 2;
    public int contactDamage = 1;

    private int currentHP;
    private Transform player;
    private float spawnTimer = 0f;
    private float activateDelay = 0.5f;
    private bool canHit = false;

    // HP바 관련
    private GameObject hpBarBG;
    private GameObject hpBarFill;

    void Start()
    {
        currentHP = maxHP;

        // 충돌 감지를 위해 Rigidbody2D 필수 (Kinematic)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 충돌 감지를 위해 Collider 필수 (Trigger)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        CreateHPBar();
    }

    void CreateHPBar()
    {
        // HP바 배경 (어두운 색)
        hpBarBG = new GameObject("HPBarBG");
        // 부모 설정 안 함 (독립 오브젝트, 회전 영향 없음)
        SpriteRenderer bgRenderer = hpBarBG.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateSquareSprite();
        bgRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        bgRenderer.sortingOrder = 10;
        hpBarBG.transform.position = transform.position + new Vector3(0, -0.7f, 0);
        hpBarBG.transform.localScale = new Vector3(1f, 0.1f, 1f);

        // HP바 채우기 (빨간 색)
        hpBarFill = new GameObject("HPBarFill");
        hpBarFill.transform.SetParent(hpBarBG.transform, false);
        SpriteRenderer fillRenderer = hpBarFill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateSquareSprite();
        fillRenderer.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        fillRenderer.sortingOrder = 11;
        hpBarFill.transform.localPosition = Vector3.zero;
        hpBarFill.transform.localScale = Vector3.one;
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (player == null) return;

        // 스폰 후 딜레이
        if (!canHit)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= activateDelay)
            {
                canHit = true;
            }
        }

        // 플레이어를 향해 직선 이동
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 플레이어를 바라보도록 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // HP바가 항상 적 아래에 고정
        if (hpBarBG != null)
        {
            hpBarBG.transform.position = transform.position + new Vector3(0, -0.7f, 0);
        }

        // 거리 기반 피격 판정
        if (canHit)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= hitDistance)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(contactDamage);
                }
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (hpBarBG != null) Destroy(hpBarBG);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        UpdateHPBar();

        if (currentHP <= 0)
        {
            // 경험치 지급
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.AddExp(1);
            }
            Destroy(gameObject);
        }
    }

    void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            hpBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
            // 왼쪽부터 줄어들도록 위치 조정
            hpBarFill.transform.localPosition = new Vector3(-(1f - ratio) / 2f, 0, 0);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitDistance);
    }
}
