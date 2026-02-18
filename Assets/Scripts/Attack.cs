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
    public float skillCooldown = 3f;
    private float lastSkillTime = -100f;
    private bool isSpinning = false;

    public void IncreaseDamage(int amount)
    {
        attackDamage += amount;
        if (weaponController != null)
        {
            weaponController.damage = attackDamage;
        }
        
        PlayerHealth health = GetComponent<PlayerHealth>();
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

    void Start()
    {
        if (weapon == null)
        {
            // "word" 또는 "Rarm" 등 무기 부모 찾기 시도
            weapon = transform.Find("word");
            if (weapon == null) weapon = transform.Find("Rarm");
        }

        if (weapon != null)
        {
            initialRotation = weapon.localRotation;
            rArmInitialPos = weapon.localPosition;

            // 왼팔 자동으로 찾기
            if (leftArm == null) leftArm = transform.Find("Larm");
            if (leftArm != null)
            {
                lArmInitialPos = leftArm.localPosition;
                lArmInitialRot = leftArm.localRotation;
            }
            
            // 1. "sword" 이름으로 찾기
            Transform sword = weapon.Find("sword");
            // 2. 없으면 첫 번째 자식을 무기로 간주 (Square 등일 수 있음)
            if (sword == null && weapon.childCount > 0) 
            {
                sword = weapon.GetChild(0);
                // Debug.Log("sword를 이름으로 못 찾아서 첫 번째 자식(" + sword.name + ")을 사용합니다.");
            }

            if (sword != null)
            {
                weaponController = sword.GetComponent<WeaponController>();
                if (weaponController == null)
                {
                    weaponController = sword.gameObject.AddComponent<WeaponController>();
                    // Debug.Log("WeaponController가 없어서 자동으로 추가했습니다.");
                }
                weaponController.damage = attackDamage;
            }

            // UI 초기화
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health == null) health = GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.UpdateDamageText(attackDamage);
            }
            else
            {
                // Debug.LogError("무기에 자식 오브젝트(칼날)가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Weapon transform을 찾을 수 없습니다! 'word' 또는 'Rarm' 자식이 있는지 확인하세요.");
        }
    }

    void Update()
    {
        if (weapon == null) return;

        // E 키 직접 체크 (Input System 외 예외 처리용)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryStartSkill();
        }

        bool rightPressed = Mouse.current.rightButton.isPressed;

        if (rightPressed && !isAttacking && !isSpinning_internal)
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
        if (value.isPressed && !isAttacking && !isSpinning_internal && !isGuarding && weapon != null)
        {
            StartCoroutine(PerformAttack());
        }
    }

    public void OnSkill(InputValue value)
    {
        if (value.isPressed)
        {
            TryStartSkill();
        }
    }

    private void TryStartSkill()
    {
        if (!isAttacking && !isSpinning_internal && !isGuarding && Time.time >= lastSkillTime + skillCooldown && weapon != null)
        {
            StartCoroutine(PerformSpinAttack());
        }
    }

    IEnumerator PerformSpinAttack()
    {
        isSpinning_internal = true;
        lastSkillTime = Time.time;

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
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

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

        if (weaponController != null) weaponController.EndAttack(); // 공격 판정 끄기

        weapon.localRotation = initialRotation;
        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }


}
