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

    public float FireballCooldownRemaining => Mathf.Max(0, (lastFireballTime + fireballCooldown) - Time.time);
    public float SkillCooldownRemaining => Mathf.Max(0, (lastSkillTime + skillCooldown) - Time.time);

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
            rArmInitialPos = weapon.localPosition;
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
            if (kb.eKey != null && kb.eKey.wasPressedThisFrame) TryStartSkill();
            if (kb.qKey != null && kb.qKey.wasPressedThisFrame) TryStartFireball();
        }

        // 4. 마우스 입력 처리 (가드)
        if (ms != null && ms.rightButton != null)
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
            TryStartSkill();
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


}
