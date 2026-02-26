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

    // 엘리트 및 보스 관련
    public bool isElite = false;
    public bool isBoss = false;
    private SpriteRenderer mainRenderer;

    // 보스 행동 패턴 관련
    private float bossActionTimer = 0f;
    private bool isTelegraphing = false;
    private bool isDashing = false;
    private Vector2 dashDirection;
    private float dashDuration = 0.2f;
    private float dashTimer = 0f;
    private float dashSpeed = 45f;

    // HP바 관련
    private GameObject hpBarBG;
    private GameObject hpBarFill;

    void Start()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        // currentHP 초기화는 Spawner에서 SetHPByWave를 호출하므로 삭제하거나 기본값 유지
        if (currentHP <= 0) currentHP = maxHP;

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

    // 넉백 관련 변수
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    public float knockbackDuration = 0.2f; // 넉백 지속 시간
    public float knockbackSpeed = 10f; // 넉백 속도 (일반 이동 속도보다 빠름)
    private Vector2 knockbackDir;

    void Update()
    {
        if (player == null) return;

        // 넉백 상태 처리
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            transform.position += (Vector3)(knockbackDir * knockbackSpeed * Time.deltaTime);

            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            return; // 넉백 중에는 일반 이동 및 공격 불가
        }

        // 보스 행동 패턴 처리
        if (isBoss)
        {
            if (isDashing)
            {
                transform.position += (Vector3)(dashDirection * dashSpeed * Time.deltaTime);
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0) isDashing = false;
                
                CheckPlayerHit(); // 돌진 중에도 피격 판정
                return;
            }

            if (isTelegraphing)
            {
                bossActionTimer += Time.deltaTime;
                // 돌진 전 예고 연출 (빨간색으로 깜빡임)
                if (mainRenderer != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 10, 1.0f);
                    mainRenderer.color = Color.Lerp(new Color(0.5f, 0, 0.5f), Color.red, pulse);
                }

                if (bossActionTimer >= 1.0f) // 1초 대기 후 돌진
                {
                    isTelegraphing = false;
                    isDashing = true;
                    dashTimer = dashDuration;
                    dashDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    bossActionTimer = 0f;
                    if (mainRenderer != null) mainRenderer.color = new Color(0.5f, 0, 0.5f); // 원래 색 복구
                }
                return;
            }

            bossActionTimer += Time.deltaTime;
            if (bossActionTimer >= 5.0f) // 5초 이동 후 telegraph
            {
                isTelegraphing = true;
                bossActionTimer = 0f;
                return;
            }
        }

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

        CheckPlayerHit();
    }

    void CheckPlayerHit()
    {
        if (player == null || !canHit) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= hitDistance * (isBoss ? 2.5f : 1.0f)) // 보스는 피격 범위가 큼
        {
            // 플레이어가 가드 중인지 확인
            Attack playerAttack = player.GetComponent<Attack>();
            bool isGuarding = false;

            if (playerAttack != null && playerAttack.IsGuarding)
            {
                // 가드 각도 계산 (전방 120도 = 좌우 60도)
                Vector2 dirToEnemy = ((Vector2)transform.position - (Vector2)player.position).normalized;
                if (Vector2.Angle((Vector2)player.up, dirToEnemy) < 60f)
                {
                    isGuarding = true;
                }
            }

            if (isGuarding)
            {
                // 가드 성공: 적 넉백 발생
                isKnockedBack = true;
                knockbackTimer = knockbackDuration;
                knockbackDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            }
            else if (playerAttack != null && playerAttack.IsParrying)
            {
                // 패링 성공: 데미지 방어 + 패링 카운터 어택
                Vector2 dirToEnemy = ((Vector2)transform.position - (Vector2)player.position).normalized;
                if (Vector2.Angle((Vector2)player.up, dirToEnemy) < 90f) // 전방 180도 범위
                {
                    playerAttack.OnParrySuccess(this);
                }
                else
                {
                    // 후방 공격: 패링 실패, 일반 데미지
                    PlayerHealth health = player.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(contactDamage);
                    }
                    if (!isBoss) Destroy(gameObject);
                }
            }
            else
            {
                // 플레이어에게 데미지 입힘
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(contactDamage);
                }

                // 보스 추가 효과: 플레이어 넉백
                if (isBoss)
                {
                    Move playerMove = player.GetComponent<Move>();
                    if (playerMove != null)
                    {
                        Vector2 pushDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                        playerMove.ApplyKnockback(pushDir, 18f, 0.25f); // 큰 넉백
                    }
                }
                else
                {
                    // 일반 적은 자폭
                    Destroy(gameObject);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (hpBarBG != null) Destroy(hpBarBG);
    }

    public void ApplyParryKnockback(Vector2 direction, float force)
    {
        isKnockedBack = true;
        knockbackTimer = 0.4f; // 패링 넉백은 일반보다 길게
        knockbackDir = direction;
        knockbackSpeed = force;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        UpdateHPBar();

        if (currentHP <= 0)
        {
            // 보스 처치 시 로직
            if (isBoss && LevelManager.Instance != null)
            {
                LevelManager.Instance.GameOver(true);
            }

            // 경험치 지급
            if (LevelManager.Instance != null)
            {
                // 엘리트는 경험치 2배
                LevelManager.Instance.AddExp(isElite ? 2 : 1);
            }

            // 웨이브 킬 카운트 업데이트
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.OnEnemyKilled();
            }

            Destroy(gameObject);
        }
    }

    void UpdateHPBar()
    {
        // 1. 개별 유닛 위의 HP바 업데이트
        if (hpBarFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            hpBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
            hpBarFill.transform.localPosition = new Vector3(-(1f - ratio) / 2f, 0, 0);
        }

        // 2. 보스일 경우 화면 상단 대형 HP바 업데이트
        if (isBoss && LevelManager.Instance != null)
        {
            LevelManager.Instance.UpdateBossHPBar(currentHP, maxHP);
        }
    }

    public void SetHPByWave(int wave)
    {
        if (isBoss) return; // 보스는 별도 스탯 사용하므로 스킵

        // 기본 체력 2 + 매 웨이브마다 1씩 증가
        maxHP = 2 + (wave - 1);
        
        // 데미지 성장: 5개 웨이브마다 1씩 증가
        contactDamage = 1 + (wave - 1) / 5;

        // 엘리트라면 체력/데미지 3배
        if (isElite)
        {
            maxHP *= 3;
            contactDamage *= 3;
        }

        currentHP = maxHP;
        UpdateHPBar();
    }

    public void SetElite(bool elite)
    {
        isElite = elite;
        if (isElite)
        {
            // 초록색 및 크기 조정
            if (mainRenderer == null) mainRenderer = GetComponent<SpriteRenderer>();
            if (mainRenderer != null) mainRenderer.color = Color.green;
            transform.localScale = Vector3.one * 1.5f;

            // 이미 SetHPByWave가 호출되었을 상황을 대비해 스탯 재설정 (Spawner 순서 변경 대비)
            maxHP = (2 + (EnemySpawner.Instance.currentWave - 1)) * 3;
            contactDamage = (1 + (EnemySpawner.Instance.currentWave - 1) / 5) * 3;
            currentHP = maxHP;
            UpdateHPBar();
        }
    }

    public void SetBoss(bool boss)
    {
        isBoss = boss;
        if (isBoss)
        {
            // 보라색으로 변경
            if (mainRenderer == null) mainRenderer = GetComponent<SpriteRenderer>();
            if (mainRenderer != null) mainRenderer.color = new Color(0.5f, 0, 0.5f); // Purple
            
            // 크기 3배
            transform.localScale = Vector3.one * 3.0f;

            // 체력/데미지 보정 (요청사항: 체력 100)
            maxHP = 100;
            contactDamage = 35; // 3대 맞으면 죽음 (maxHP 60-100 사이 기준)
            currentHP = maxHP;
            UpdateHPBar();

            // 보스 체력바 생성
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CreateBossHPBar(maxHP);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitDistance);
    }
}
