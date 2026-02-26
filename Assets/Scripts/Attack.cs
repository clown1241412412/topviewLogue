using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public Transform weapon; // 보통 Rarm (오른팔)
    public Transform leftArm; // 왼팔 추가
    public float swingDuration = 0.2f;
    public float swingAngle = 120f; // Total arc angle
    public float cooldown = 0.1f;
    public float guardAngle = 90f;
    public int attackDamage = 1;

    [Header("Skill Settings")]
    public float spinDuration = 5.0f;
    public float hitResetInterval = 0.3f; // 얼마나 자주 다시 때릴지
    public float skillCooldown = 5f;
    private float lastSkillTime = -100f;
    public float fireballCooldown = 3f;
    private float lastFireballTime = -100f;
    public GameObject fireballPrefab;

    [Header("Skill Unlock Status")]
    public bool hasFireball = false;
    public bool hasSpin = false;
    public bool hasSwordWave = false;
    public bool hasParry = false;
    public bool hasBloodSlash = false;

    [Header("Buff Status")]
    private float swordWaveRemainingTime = 0f;
    public float swordWaveDuration = 5f;
    public float swordWaveCooldown = 8f;
    private float lastSwordWaveTime = -100f;

    [Header("Parry Settings")]
    public float parryCooldown = 4f;
    private float lastParryTime = -100f;
    public float parryWindow = 0.3f;
    public float parryAoeRadius = 5f;
    public float parryAoeAngle = 180f;
    public float parryKnockbackForce = 8f;

    [Header("Blood Slash Settings")]
    public float bloodSlashCooldown = 15f;
    private float lastBloodSlashTime = -100f;
    public int bloodSlashDamageMultiplier = 10;

    public float FireballCooldownRemaining => Mathf.Max(0, (lastFireballTime + fireballCooldown) - Time.time);
    public float SkillCooldownRemaining => Mathf.Max(0, (lastSkillTime + skillCooldown) - Time.time);
    public float SwordWaveCooldownRemaining => Mathf.Max(0, (lastSwordWaveTime + swordWaveCooldown) - Time.time);
    public float ParryCooldownRemaining => Mathf.Max(0, (lastParryTime + parryCooldown) - Time.time);
    public float BloodSlashCooldownRemaining => Mathf.Max(0, (lastBloodSlashTime + bloodSlashCooldown) - Time.time);
    public float RSkillCooldownRemaining => hasBloodSlash ? BloodSlashCooldownRemaining : SwordWaveCooldownRemaining;

    public void IncreaseDamage(int amount)
    {
        if (moveScript == null) InitializeComponents(); // 방어적 초기화
        attackDamage += amount;
        if (weaponController != null)
        {
            weaponController.damage = attackDamage;
        }
        
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.UpdateDamageText(attackDamage);
        }
    }

    private bool isAttacking = false;
    private bool isSpinning_internal = false; // 이름 충돌 방지
    private bool isGuarding = false;
    public bool IsGuarding { get { return isGuarding; } } // 외부에서 접근 가능하도록 프로퍼티 추가
    private bool isParrying = false;
    public bool IsParrying { get { return isParrying; } }
    private Quaternion initialRotation;
    private Vector3 rArmInitialPos;
    private Vector3 lArmInitialPos;
    private Quaternion lArmInitialRot;
    private WeaponController weaponController;
    private Move moveScript;
    private int heartbeatCounter = 0;

    void Start()
    {
        try 
        {
            InitializeComponents();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Attack] Start Initialization Failed: {e.Message}");
        }
    }

    void InitializeComponents()
    {
        Debug.Log("[Attack] Initializing Components...");
        
        moveScript = GetComponent<Move>();
        if (moveScript == null) moveScript = GetComponentInParent<Move>();

        // 1. 무기(오른팔) 찾기
        if (weapon == null)
        {
            weapon = FindChildRecursive(transform, "word");
            if (weapon == null) weapon = FindChildRecursive(transform, "Rarm");
        }

        if (weapon != null)
        {
            initialRotation = weapon.localRotation;
            weapon.localRotation = initialRotation;
            Debug.Log($"[Attack] Weapon found: {weapon.name}");

            // WeaponController 찾기 및 설정
            Transform sword = FindChildRecursive(weapon, "sword");
            if (sword == null && weapon.childCount > 0) sword = weapon.GetChild(0);

            if (sword != null)
            {
                weaponController = sword.GetComponent<WeaponController>();
                if (weaponController == null) weaponController = sword.gameObject.AddComponent<WeaponController>();
                weaponController.damage = attackDamage;
            }
        }

        // 2. 왼팔 찾기 (무기 유무와 상관없이 실행)
        if (leftArm == null)
        {
            leftArm = FindChildRecursive(transform, "Larm");
            if (leftArm == null) leftArm = FindChildRecursive(transform, "LeftArm");
        }

        if (leftArm != null)
        {
            lArmInitialPos = leftArm.localPosition;
            lArmInitialRot = leftArm.localRotation;
            Debug.Log($"[Attack] LeftArm found: {leftArm.name}, Pos: {lArmInitialPos}");
        }
        else
        {
            Debug.LogWarning("[Attack] LeftArm을 찾을 수 없습니다. Q 스킬 발사가 제한될 수 있습니다.");
        }

        // 3. UI 업데이트
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (health != null) health.UpdateDamageText(attackDamage);
        
        Debug.Log("[Attack] Initialization Complete.");
    }

    // 재귀적으로 자식 찾기 도우미
    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    void Update()
    {
        // 1. 무기가 없으면 조작 불가
        if (weapon == null) return;

        // 2. 입력 장치 상태 확인
        heartbeatCounter++;
        if (heartbeatCounter >= 100) { heartbeatCounter = 0; } // Heartbeat

        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;

        // 3. 키보드 입력 처리
        if (kb != null)
        {
            // 특정 키가 null일 수 있는 예외 상황 대비
            if (kb.eKey != null && kb.eKey.wasPressedThisFrame)
            {
                if (hasParry) TryStartParry();
                else TryStartSkill();
            }
            if (kb.qKey != null && kb.qKey.wasPressedThisFrame) TryStartFireball();
            if (kb.rKey != null && kb.rKey.wasPressedThisFrame)
            {
                if (hasBloodSlash) TryStartBloodSlash();
                else TryStartSwordWave();
            }
        }

        // Sword Wave 버프 타이머 업데이트
        if (swordWaveRemainingTime > 0)
        {
            swordWaveRemainingTime -= Time.deltaTime;
        }

        if (ms != null && ms.rightButton != null && Time.timeScale > 0) // 일시 정지 중에는 조작 무시
        {
            bool rightPressed = ms.rightButton.isPressed;

            if (rightPressed && !isAttacking && !isSpinning_internal)
            {
                if (!isGuarding)
                {
                    isGuarding = true;
                    weapon.localRotation = initialRotation * Quaternion.Euler(0, 0, guardAngle);
                }
            }
            else if (!rightPressed && isGuarding)
            {
                isGuarding = false;
                weapon.localRotation = initialRotation;
            }
        }
        else if (isGuarding) // 마우스를 잃어버린 경우 가드 자동 해제
        {
            isGuarding = false;
            weapon.localRotation = initialRotation;
        }
    }

    void OnDisable()
    {
        isGuarding = false;
        if (weapon != null) weapon.localRotation = initialRotation;
    }

    public void OnAttack(InputValue value)
    {
        if (weapon == null) InitializeComponents();
        if (value.isPressed && !isAttacking && !isSpinning_internal && !isGuarding && weapon != null)
        {
            if (gameObject.activeInHierarchy) 
            {
                isAttacking = true;
                StartCoroutine(PerformAttack());
            }
        }
    }

    public void OnSkill(InputValue value)
    {
        if (value.isPressed)
        {
            if (hasParry) TryStartParry();
            else TryStartSkill();
        }
    }

    public void OnFireball(InputValue value)
    {
        if (value.isPressed)
        {
            TryStartFireball();
        }
    }

    private void TryStartFireball()
    {
        if (!hasFireball) return;
        if (moveScript == null) InitializeComponents();
        if (!isAttacking && !isSpinning_internal && !isGuarding && Time.time >= lastFireballTime + fireballCooldown && leftArm != null)
        {
            if (gameObject.activeInHierarchy) 
            {
                isAttacking = true;
                StartCoroutine(PerformFireball());
            }
        }
    }

    IEnumerator PerformFireball()
    {
        Debug.Log("[Attack] 1. Fireball Coroutine Started");
        lastFireballTime = Time.time;
        // isAttacking = true; (TryStartFireball에서 이미 설정)


        try
        {
            Debug.Log("[Attack] 2. Animating LeftArm...");
            // 왼팔 앞으로 뻗기
            Vector3 extendPos = lArmInitialPos + new Vector3(0, 1.2f, 0); 
            float extendDuration = 0.15f;
            float et = 0f;

            while (et < extendDuration)
            {
                et += Time.deltaTime;
                if (leftArm != null) 
                {
                    leftArm.localPosition = Vector3.Lerp(lArmInitialPos, extendPos, et / extendDuration);
                }
                yield return null;
            }

            Debug.Log("[Attack] 3. Attempting to spawn fireball...");
            if (leftArm == null) 
            {
                Debug.LogError("[Attack] Point 3: LeftArm is NULL! Aborting.");
                yield break; 
            }

            Vector3 spawnPos = leftArm.position + (leftArm.up * 0.5f);
            GameObject fireball = null;
            
            if (fireballPrefab != null)
            {
                Debug.Log("[Attack] 4a. Instantiating Prefab...");
                fireball = Instantiate(fireballPrefab, spawnPos, transform.rotation);
            }
            else
            {
                Debug.Log("[Attack] 4b. Creating Primitive with Visual Child...");
                fireball = new GameObject("Fireball_Primitive");
                fireball.transform.position = spawnPos;
                fireball.transform.rotation = transform.rotation;

                // 시각적 요소(Sphere)를 자식으로 생성
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Visual";
                visual.transform.SetParent(fireball.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 0.5f;

                // 자식의 3D Collider 제거
                var sphereCollider = visual.GetComponent<SphereCollider>();
                if (sphereCollider != null) Destroy(sphereCollider);

                // 루트 오브젝트에 2D 컴포넌트 추가
                CircleCollider2D col = fireball.AddComponent<CircleCollider2D>();
                if (col != null) col.isTrigger = true;

                Rigidbody2D rb = fireball.AddComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }

                fireball.AddComponent<Fireball>();
                
                Renderer rend = visual.GetComponent<Renderer>();
                if (rend != null) rend.material.color = Color.red;
            }

            if (fireball != null)
            {
                Debug.Log("[Attack] 5. Setting Fireball Damage...");
                Fireball fbScript = fireball.GetComponent<Fireball>();
                if (fbScript != null) 
                {
                    fbScript.damage = attackDamage * 2;
                }
                else
                {
                    Debug.LogWarning("[Attack] 5: Created fireball but missing Fireball script!");
                }
            }
            else
            {
                Debug.LogError("[Attack] 5: Fireball object is NULL after instantiation!");
            }

            Debug.Log("[Attack] 6. Waiting for recovery...");
            yield return new WaitForSeconds(0.1f);

            // 왼팔 원상복구
            et = 0f;
            while (et < extendDuration)
            {
                et += Time.deltaTime;
                if (leftArm != null) 
                {
                    leftArm.localPosition = Vector3.Lerp(extendPos, lArmInitialPos, et / extendDuration);
                }
                yield return null;
            }
        }
        finally
        {
            Debug.Log("[Attack] 7.1: Finally Block Started");
            if (leftArm != null) 
            {
                leftArm.localPosition = lArmInitialPos;
                Debug.Log("[Attack] 7.2: LeftArm position reset");
            }
            isAttacking = false;
            Debug.Log("[Attack] 7.4: isAttacking reset complete");
        }
    }

    private void TryStartSkill()
    {
        if (!hasSpin) return;
        if (moveScript == null) InitializeComponents();
        if (!isAttacking && !isSpinning_internal && !isGuarding && Time.time >= lastSkillTime + skillCooldown && weapon != null)
        {
            if (gameObject.activeInHierarchy)
            {
                isSpinning_internal = true;
                StartCoroutine(PerformSpinAttack());
            }
        }
    }

    IEnumerator PerformSpinAttack()
    {
        // isSpinning_internal = true; (TryStartSkill에서 이미 설정)


        if (weaponController != null)
        {
            weaponController.damage = attackDamage;
            weaponController.StartAttack();
        }

        float elapsedTime = 0f;
        float lastResetTime = 0f;

        // 초당 몇 바퀴 돌지
        float rotationsPerSecond = 2f; 

        // 스킬 시작 시 팔을 앞으로 모으기 (두 손으로 잡는 연출)
        Vector3 rArmSkillPos = rArmInitialPos + new Vector3(0.2f, 0.5f, 0); // 약간 앞으로, 안으로
        Vector3 lArmSkillPos = rArmSkillPos; // 왼팔도 오른팔 근처로 (또는 겹치게)

        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 팔 모으기 보간 (0.2초 동안 모으기)
            float t_pos = Mathf.Min(elapsedTime / 0.2f, 1f);
            weapon.localPosition = Vector3.Lerp(rArmInitialPos, rArmSkillPos, t_pos);
            if (leftArm != null) leftArm.localPosition = Vector3.Lerp(lArmInitialPos, lArmSkillPos, t_pos);

            // 회전 애니메이션 (두 팔이 같이 회전)
            float rotationAngle = elapsedTime * rotationsPerSecond * 360f;
            weapon.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, rotationAngle);

            // 일정 간격마다 타격 리스트 초기화
            if (elapsedTime - lastResetTime >= hitResetInterval)
            {
                if (weaponController != null) weaponController.ClearHitList();
                lastResetTime = elapsedTime;
            }
            
            yield return null;
        }

        // 원래 위치로 복구
        float recoveryTime = 0.2f;
        float elapsedRecovery = 0f;
        Vector3 currentRArmPos = weapon.localPosition;
        Vector3 currentLArmPos = leftArm != null ? leftArm.localPosition : Vector3.zero;
        Quaternion currentRArmRot = weapon.localRotation;
        Quaternion currentLArmRot = leftArm != null ? leftArm.localRotation : Quaternion.identity;

        while (elapsedRecovery < recoveryTime)
        {
            elapsedRecovery += Time.deltaTime;
            float t = elapsedRecovery / recoveryTime;

            weapon.localPosition = Vector3.Lerp(currentRArmPos, rArmInitialPos, t);
            weapon.localRotation = Quaternion.Lerp(currentRArmRot, initialRotation, t);

            if (leftArm != null)
            {
                leftArm.localPosition = Vector3.Lerp(currentLArmPos, lArmInitialPos, t);
                leftArm.localRotation = Quaternion.Lerp(currentLArmRot, lArmInitialRot, t);
            }
            yield return null;
        }

        weapon.localPosition = rArmInitialPos;
        weapon.localRotation = initialRotation;
        if (leftArm != null)
        {
            leftArm.localPosition = lArmInitialPos;
            leftArm.localRotation = lArmInitialRot;
        }

        if (weaponController != null)
        {
            weaponController.EndAttack();
        }

        isSpinning_internal = false;
        lastSkillTime = Time.time; // 스킬 종료 후 쿨타임 시작
    }

    IEnumerator PerformAttack()
    {
        // isAttacking = true; (OnAttack에서 이미 설정)
        try
        {
            if (weaponController != null) weaponController.StartAttack(); // 공격 판정 켜기

            Quaternion startRot = initialRotation * Quaternion.Euler(0, 0, swingAngle / 2f);
            Quaternion endRot = initialRotation * Quaternion.Euler(0, 0, -swingAngle / 2f);

            float elapsedTime = 0f;

            while (elapsedTime < swingDuration)
            {
                float t = elapsedTime / swingDuration;
                t = t * t * (3f - 2f * t);

                weapon.localRotation = Quaternion.Lerp(startRot, endRot, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Sword Wave 발사
            if (swordWaveRemainingTime > 0)
            {
                SpawnSwordWave();
            }

            weapon.localRotation = endRot;
            yield return null;
        }
        finally
        {
            if (weaponController != null) weaponController.EndAttack(); // 공격 판정 끄기

            weapon.localRotation = initialRotation;
            StartCoroutine(ResetAttackAfterCooldown());
        }
    }

    IEnumerator ResetAttackAfterCooldown()
    {
        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }

    private void TryStartSwordWave()
    {
        if (!hasSwordWave) return;
        if (Time.time >= lastSwordWaveTime + swordWaveCooldown)
        {
            swordWaveRemainingTime = swordWaveDuration;
            lastSwordWaveTime = Time.time;
            Debug.Log("[Skill] Sword Wave Activated! Duration: " + swordWaveDuration);
        }
    }

    // === 블러드 슬래시 (Blood Slash) ===
    private void TryStartBloodSlash()
    {
        if (!hasBloodSlash) return;
        if (moveScript == null) InitializeComponents();
        if (!isAttacking && !isSpinning_internal && !isGuarding && !isParrying && Time.time >= lastBloodSlashTime + bloodSlashCooldown && weapon != null)
        {
            if (gameObject.activeInHierarchy)
            {
                isAttacking = true;
                StartCoroutine(PerformBloodSlash());
            }
        }
    }

    IEnumerator PerformBloodSlash()
    {
        lastBloodSlashTime = Time.time;
        Debug.Log("[Attack] Blood Slash Activated!");

        // 1. 연출 준비: 캐릭터를 붉게 물들이기 (데미지보다 먼저)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = Color.white;
        if (sr != null)
        {
            origColor = sr.color;
            sr.color = new Color(1f, 0.1f, 0.1f, 1f);
        }

        // 2. 무기 회전 연출 (빠르게 한 바퀴 휘두름)
        float slashDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < slashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slashDuration;
            weapon.localRotation = initialRotation * Quaternion.Euler(0, 0, t * 360f);
            // 연출 중에도 붉은 색 유지
            if (sr != null) sr.color = new Color(1f, 0.1f, 0.1f, 1f);
            yield return null;
        }
        weapon.localRotation = initialRotation;

        // 3. HP를 1로 감소 (연출 후)
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (health != null && health.currentHP > 1)
        {
            health.TakeDamage(health.currentHP - 1);
        }

        // FlashRed가 색을 덮어쓰지 않도록 즉시 재설정
        yield return null;
        if (sr != null) sr.color = new Color(1f, 0.1f, 0.1f, 1f);

        // 4. 맵 전체 적에게 데미지
        int bloodDamage = attackDamage * bloodSlashDamageMultiplier;
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeDamage(bloodDamage);
            }
        }
        Debug.Log($"[Attack] Blood Slash hit {allEnemies.Length} enemies for {bloodDamage} damage each!");

        // 5. 시각적 이펙트: 원형 검흔 파동 (맵 전체 확장)
        StartCoroutine(SpawnBloodSlashEffect());

        // 6. FlashRed 완전히 끝날 때까지 대기 후 색상 강제 복구
        yield return new WaitForSeconds(0.5f);
        if (sr != null) sr.color = origColor;

        isAttacking = false;
    }

    IEnumerator SpawnBloodSlashEffect()
    {
        // 원형 검흔 링 이펙트 (다중 레이어)
        int ringCount = 3;
        List<GameObject> effects = new List<GameObject>();

        for (int i = 0; i < ringCount; i++)
        {
            GameObject ring = new GameObject("BloodSlashRing_" + i);
            ring.transform.position = transform.position;
            ring.transform.rotation = Quaternion.identity;

            SpriteRenderer ringSR = ring.AddComponent<SpriteRenderer>();
            ringSR.sprite = GenerateCircularSlashSprite();
            // 각 링마다 약간 다른 색조
            float r = 1f - i * 0.1f;
            ringSR.color = new Color(r, 0.05f + i * 0.05f, 0.05f, 0.9f);
            ringSR.sortingOrder = 6 + i;
            effects.Add(ring);
        }

        // 확장 + 회전 + 페이드 애니메이션
        float duration = 0.8f;
        float el = 0f;
        while (el < duration)
        {
            el += Time.deltaTime;
            float t = el / duration;

            for (int i = 0; i < effects.Count; i++)
            {
                GameObject fx = effects[i];
                if (fx == null) continue;

                // 각 링은 시차를 두고 확장 (안쪽부터 바깥으로)
                float delay = i * 0.1f;
                float localT = Mathf.Clamp01((t - delay) / (1f - delay));
                float easeT = localT * localT * (3f - 2f * localT); // smoothstep

                float scale = Mathf.Lerp(0.5f, 30f, easeT); // 맵 전체를 덮는 크기
                fx.transform.localScale = new Vector3(scale, scale, 1f);

                // 회전 (각 링마다 반대 방향)
                float rotSpeed = (i % 2 == 0) ? 180f : -180f;
                fx.transform.rotation = Quaternion.Euler(0, 0, rotSpeed * t);

                // 알파 페이드
                float alpha = 0.9f * (1f - localT);
                SpriteRenderer fxSR = fx.GetComponent<SpriteRenderer>();
                float r = 1f - i * 0.1f;
                if (fxSR != null) fxSR.color = new Color(r, 0.05f + i * 0.05f, 0.05f, alpha);
            }
            yield return null;
        }

        foreach (GameObject fx in effects)
        {
            if (fx != null) Destroy(fx);
        }
    }

    private Sprite GenerateCircularSlashSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                // 원형 검흔: 링 형태 + 불규칙한 검흔 패턴
                float innerRadius = 0.55f;
                float outerRadius = 0.95f;
                bool inRing = dist > innerRadius && dist < outerRadius;

                if (inRing)
                {
                    // 가장자리 부드럽게
                    float edgeFadeOuter = Mathf.Clamp01((outerRadius - dist) * 8f);
                    float edgeFadeInner = Mathf.Clamp01((dist - innerRadius) * 8f);

                    // 검흔 느낌의 불규칙 패턴 (각도 기반 톱니)
                    float slashPattern = Mathf.Abs(Mathf.Sin(angle * 6f + dist * 15f));
                    float slashIntensity = Mathf.Clamp01(slashPattern * 1.5f);

                    float alpha = edgeFadeOuter * edgeFadeInner * (0.5f + slashIntensity * 0.5f);
                    float brightness = 0.7f + slashIntensity * 0.3f;

                    tex.SetPixel(x, y, new Color(brightness, brightness * 0.15f, brightness * 0.1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // === 패링 (Parry) ===
    private void TryStartParry()
    {
        if (!hasParry) return;
        if (moveScript == null) InitializeComponents();
        if (!isAttacking && !isSpinning_internal && !isGuarding && !isParrying && Time.time >= lastParryTime + parryCooldown && weapon != null)
        {
            if (gameObject.activeInHierarchy)
            {
                isParrying = true;
                StartCoroutine(PerformParry());
            }
        }
    }

    IEnumerator PerformParry()
    {
        lastParryTime = Time.time;
        isAttacking = true; // 다른 행동 방지

        // 시각적 피드백: 무기를 앞으로 들어올림
        Quaternion parryRotation = initialRotation * Quaternion.Euler(0, 0, 45f);
        weapon.localRotation = parryRotation;

        // 캐릭터 색상 변경 (시안색 플래시)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = Color.white;
        if (sr != null)
        {
            origColor = sr.color;
            sr.color = new Color(0.3f, 0.8f, 1f, 1f); // 시안색
        }

        // 패링 윈도우 대기
        yield return new WaitForSeconds(parryWindow);

        // 패링 윈도우 종료 - 원래 상태로 복구
        if (sr != null) sr.color = origColor;
        weapon.localRotation = initialRotation;

        isParrying = false;
        isAttacking = false;
    }

    public void OnParrySuccess(Enemy triggeredEnemy)
    {
        Debug.Log("[Attack] Parry Success!");

        // 1. 패링 트리거한 적 넉백
        if (triggeredEnemy != null)
        {
            Vector2 knockDir = ((Vector2)triggeredEnemy.transform.position - (Vector2)transform.position).normalized;
            triggeredEnemy.ApplyParryKnockback(knockDir, parryKnockbackForce);
        }

        // 2. 전방 부채꼴 범위 내 모든 적에게 데미지
        int parryDamage = attackDamage * 2;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryAoeRadius);
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            // 부채꼴 각도 체크
            Vector2 dirToEnemy = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle((Vector2)transform.up, dirToEnemy);
            if (angle <= parryAoeAngle / 2f)
            {
                enemy.TakeDamage(parryDamage);
            }
        }

        // 3. 시각적 이펙트 (파란색 파동)
        StartCoroutine(SpawnParryEffect());
    }

    IEnumerator SpawnParryEffect()
    {
        GameObject effect = new GameObject("ParryEffect");
        effect.transform.position = transform.position;
        effect.transform.rotation = transform.rotation;

        SpriteRenderer effectSR = effect.AddComponent<SpriteRenderer>();
        effectSR.sprite = GenerateParrySprite();
        effectSR.color = new Color(0.3f, 0.7f, 1f, 0.7f);
        effectSR.sortingOrder = 5;

        // 확장 애니메이션
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.5f, parryAoeRadius * 2f, t);
            effect.transform.localScale = new Vector3(scale, scale, 1f);
            effectSR.color = new Color(0.3f, 0.7f, 1f, 0.7f * (1f - t));
            yield return null;
        }
        Destroy(effect);
    }

    private Sprite GenerateParrySprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - (size / 2f)) / (size / 2f);
                float dy = (y - (size / 2f)) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 반원 (윗부분만 = 전방)
                if (dist < 0.9f && dist > 0.5f && dy > -0.1f)
                {
                    float alpha = Mathf.Clamp01((0.9f - dist) * 5f) * Mathf.Clamp01((dist - 0.5f) * 5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void SpawnSwordWave()
    {
        Vector3 spawnPos = transform.position + transform.up * 0.5f;
        GameObject waveObj = new GameObject("SwordWave_Crescent");
        waveObj.transform.position = spawnPos;
        waveObj.transform.rotation = transform.rotation;

        // SpriteRenderer 사용
        SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateCrescentSprite();
        sr.color = new Color(0.4f, 0.9f, 1f, 0.8f); // 밝은 시안색
        sr.sortingOrder = 5;

        // 회전: 플레이어가 바라보는 방향(transform.rotation) 그대로 사용
        waveObj.transform.rotation = transform.rotation;

        // 크기 조정 (가로로 매우 넓고, 세로 비율을 조금 높여 곡선미를 살림)
        waveObj.transform.localScale = new Vector3(5.0f, 2.0f, 1f);

        // 컴포넌트 추가
        CircleCollider2D col = waveObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        Rigidbody2D rb = waveObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        SwordWave swScript = waveObj.AddComponent<SwordWave>();
        // 데미지: 플레이어 공격력의 1.5배 (올림 처리)
        swScript.damage = Mathf.CeilToInt(attackDamage * 1.5f);
        // 사거리: 약 10유닛 (맵 너비 20의 절반) -> 12 * 0.85 = 10.2
        swScript.speed = 12f;
        swScript.lifeTime = 0.85f;
    }

    private Sprite GenerateCrescentSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - (size / 2f)) / (size / 2f);
                float dy = (y - (size / 2f)) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 바깥 원 (반지름 0.9) 안쪽 원 (반지름 1.0, 중심을 아래쪽으로 더 이동)
                bool inOuter = dist < 0.9f;
                float innerDy = dy + 0.7f; // 중심을 아래쪽(Y-)으로 더 이동시켜 깊은 반달 생성
                float innerDist = Mathf.Sqrt(dx * dx + innerDy * innerDy);
                bool inInner = innerDist < 1.0f; // 안쪽 원의 반지름을 키워 더 가늘고 굽은 모양으로

                if (inOuter && !inInner)
                {
                    // 가장자리 페이드 아웃 (곡선 끝부분 처리)
                    float alpha = Mathf.Clamp01((0.9f - dist) * 10f) * Mathf.Clamp01((innerDist - 0.6f) * 5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }


}
