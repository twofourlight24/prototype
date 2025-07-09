using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For .ToList() and LINQ queries

public class BossCtrl : MonoBehaviour
{
    // 보스 핵심 스탯
    [Header("Boss Core Stats")]
    public float maxHp = 500f;
    public float currentHp;
    public float phase2HpThreshold = 200f; // 2페이즈 전환 체력 (기존 사용)
    public float moveSpeed = 3f;
    public float touchDamage = 30f; // 플레이어와 닿았을 때 주는 데미지 (주 콜라이더용)
    public Image bossHpBar;

    // 플레이어 감지
    [Header("Player Detection")]
    public Transform player1Transform; // 플레이어 1 트랜스폼 (인스펙터에서 할당 권장)
    public Transform player2Transform; // 플레이어 2 트랜스폼 (인스펙터에서 할당 권장)
    private Transform currentTargetTransform; // 현재 추적 대상 플레이어의 Transform
    private Transform furthestPlayerTransform; // 가장 먼 플레이어의 Transform (Thorn 스킬용)

    // Player 컴포넌트 캐싱
    private Player player1Component;
    private Player player2Component;

    // 공격 설정
    [Header("Attack Settings")]
    public float attackRangeX = 2f; // X좌표 기준 공격 범위
    public Collider2D attackCollider; // 공격 시 활성화될 콜라이더 (예: 근접 공격 판정)
    public float normalAttackAnimDuration = 1.0f; // 일반 공격 애니메이션 지속 시간 (추정치, Animator에서 확인 필요)

    // 스킬 설정
    [Header("Skill Settings")]
    public float minSkillInterval = 7f; // 최소 스킬 발동 주기
    public float maxSkillInterval = 10f; // 최대 스킬 발동 주기
    private float nextSkillTime;
    public bool isExecutingSkill = false; // 보스 이동을 멈추는 특수 스킬(가시, 불, 대쉬)이 발동 중인지 여부

    // Thorn 스킬
    [Header("Thorn Skill")]
    public GameObject thornPrefab;
    public List<Transform> thornSpawners = new List<Transform>(); // 3개의 Thorn Spawner
    public float thornSpawnDelay = 0.1f; // 각 Thorn 스포너 간의 딜레이
    public float thornAnimDuration = 2.0f; // Thorn 애니메이션 예상 지속 시간 (Animator에서 정확한 길이 확인 후 설정)

    // Fire 스킬
    [Header("Fire Skill")]
    public Collider2D fireTrigger; // Fire 스킬 시 활성화될 트리거
    public float fireAnimDuration = 1.5f; // Fire 애니메이션 예상 지속 시간
    private bool isFireSkillActive = false; // Fire 스킬이 현재 활성화되었는지 추적하는 전용 플래그

    // Dash 스킬
    [Header("Dash Skill")]
    public float dashSpeed = 15f; // 대쉬 시 보스의 순간 속도
    public float dashReadyDuration = 0.608f; // 대쉬 준비 애니메이션 시간 (참고용)
    public float dashPerMoveDuration = 0.480f; // 실제 대쉬 이동 애니메이션 시간 (참고용)
    public Collider2D dashAttackCollider; // 대쉬 공격 콜라이더
    public float dashDamage = 50f; // 대쉬 공격 데미지

    // Heal 스킬 (새로 추가된 부분)
    [Header("Heal Skill")]
    public float healSkillHpThreshold = 333.33f; // 보스 체력의 2/3 (500 * 2/3)
    public GameObject healSlimePrefab; // 힐 슬라임 프리팹 (인스펙터에서 할당)
    public int healSlimeCount = 4; // 소환할 힐 슬라임 개수
    public float healSlimeDuration = 10f; // 힐 슬라임 소환 후 체력 회복까지의 시간
    public float healAmountPerSlime = 50f; // 슬라임 하나당 회복되는 체력
    private bool isHealingSkillActive = false; // 힐 스킬이 현재 진행 중인지 여부
    private bool hasUsedHealSkill = false; // 힐 스킬을 이미 사용했는지 여부 (한 번만 발동)
    // private List<GameObject> activeHealSlimes = new List<GameObject>(); // HealSlime 태그로 찾을 것이므로 이 리스트는 이제 필요 없습니다.

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D mainCollider; // 보스의 주 콜라이더 (플레이어 접촉 데미지용)

    private Vector3 initialLocalScale; // 보스의 초기 로컬 스케일 저장

    // 현재 공격 중인지 확인하는 플래그 (코루틴 중복 실행 방지 및 상태 관리)
    private bool isAttacking = false;
    private bool isInvincible = false; // 보스 무적 상태

    // 애니메이터 파라미터 해시 (성능 최적화)
    private int hashIsAttack;
    private int hashIsDash;
    private int hashIsDashFinished;
    private int hashIsWalk;
    private int hashIsHeal; // "isHeal" Trigger 파라미터
    private int hashIsHealEnd; // "isHealEnd" Trigger 파라미터

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<BoxCollider2D>(); // 보스의 주 콜라이더는 보통 BoxCollider2D

        if (attackCollider != null) attackCollider.enabled = false;
        if (fireTrigger != null) fireTrigger.enabled = false;
        if (dashAttackCollider != null) dashAttackCollider.enabled = false;
        // 새로 추가: 힐 슬라임 프리팹이 할당되지 않았다면 경고
        if (healSlimePrefab == null)
        {
            Debug.LogWarning("HealSlimePrefab is not assigned in BossCtrl. Heal skill may not work!");
        }

        // 플레이어 부활 이벤트 구독
        Player.OnPlayerRevived += HandlePlayerRevived;

        // 초기 플레이어 찾기 (Awake에서 FindWithTag 사용)
        GameObject p1Obj = GameObject.FindWithTag("Player1");
        if (p1Obj != null)
        {
            player1Transform = p1Obj.transform;
            player1Component = p1Obj.GetComponent<Player>();
        }
        else
        {
            Debug.LogWarning("Player1 object with tag 'Player1' not found in scene!");
        }

        GameObject p2Obj = GameObject.FindWithTag("Player2");
        if (p2Obj != null)
        {
            player2Transform = p2Obj.transform;
            player2Component = p2Obj.GetComponent<Player>();
        }
        else
        {
            Debug.LogWarning("Player2 object with tag 'Player2' not found in scene!");
        }

        initialLocalScale = transform.localScale; // 초기 로컬 스케일 저장

        // Animator 파라미터 해시 값 미리 계산
        hashIsAttack = Animator.StringToHash("isAttack");
        hashIsDash = Animator.StringToHash("isDash");
        hashIsDashFinished = Animator.StringToHash("isDashFinished");
        hashIsWalk = Animator.StringToHash("isWalk");
        hashIsHeal = Animator.StringToHash("isHeal"); // "isHeal" Trigger 파라미터
        hashIsHealEnd = Animator.StringToHash("isHealEnd"); // "isHealEnd" Trigger 파라미터
    }

    void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        Player.OnPlayerRevived -= HandlePlayerRevived;
    }

    void Start()
    {
        currentHp = maxHp;
        SetNextSkillTime();

        if (player1Transform == null && player2Transform == null)
        {
            Debug.LogWarning("게임 시작 시 플레이어 트랜스폼이 모두 할당되지 않았습니다. 플레이어 생성 및 태그를 확인하세요.", this);
        }
        // 힐 스킬 임계값 계산
        healSkillHpThreshold = maxHp * (2f / 3f);
    }

    void Update()
    {
        // 보스 체력바 업데이트
        if (bossHpBar != null)
        {
            bossHpBar.fillAmount = currentHp / maxHp;
        }

        UpdateTargetPlayerState(); // 플레이어 사망 여부만 확인하고 Transform을 null로 설정

        // 모든 플레이어가 죽었을 경우, 보스는 아무것도 하지 않음 (이동 및 스킬 중단)
        if (player1Transform == null && player2Transform == null)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(hashIsWalk, false);
            anim.SetBool(hashIsAttack, false);
            isExecutingSkill = false;
            isFireSkillActive = false;
            isHealingSkillActive = false; // 힐 스킬도 중단

            StopAllCoroutines();
            isAttacking = false;
            isInvincible = false; // 무적 상태 해제

            if (attackCollider != null && attackCollider.enabled)
            {
                attackCollider.enabled = false;
                Debug.Log("All players dead, Boss Attack Collider Disabled.");
            }
            if (fireTrigger != null && fireTrigger.enabled)
            {
                fireTrigger.enabled = false;
                Debug.Log("All players dead, Boss Fire Trigger Disabled.");
            }
            if (dashAttackCollider != null && dashAttackCollider.enabled)
            {
                dashAttackCollider.enabled = false;
                Debug.Log("All players dead, Boss Dash Attack Collider Disabled.");
            }
            // 힐 슬라임들도 정리
            GameObject[] remainingSlimes = GameObject.FindGameObjectsWithTag("HealSlime");
            foreach (GameObject slime in remainingSlimes)
            {
                if (slime != null) Destroy(slime);
            }

            return;
        }

        // 힐 스킬 발동 조건 체크 (한 번만 발동)
        if (!hasUsedHealSkill && currentHp <= healSkillHpThreshold && !isHealingSkillActive)
        {
            Debug.Log($"[HealSkill] HP dropped below {healSkillHpThreshold}. Initiating heal skill sequence.");
            hasUsedHealSkill = true; // 스킬 발동 플래그 설정
            StartCoroutine(HealSkillDelayAndStart());
        }

        // 플레이어가 한 명이라도 살아있다면
        // 특수 스킬(가시, 불, 대쉬, 힐) 중이 아닐 때만 일반 이동 및 스킬 쿨타임 체크
        if (!isExecutingSkill && !isHealingSkillActive)
        {
            HandleMovementAndAttackDecision();
            CheckSkillCooldown(); // 스킬 쿨타임 체크
        }
        else // 특수 스킬 실행 중일 때 (힐 스킬 포함)
        {
            // 스킬 중에는 이동 중단 (대쉬 스킬은 이동 로직이 다르므로 예외 처리)
            // Fire 스킬, Thorn 스킬, Heal 스킬 중에는 이동 중단
            if (!anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDashFull") && !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossDashFull"))
            {
                rb.linearVelocity = Vector2.zero;
                anim.SetBool(hashIsWalk, false);
            }
            // 스킬 중에는 isAttacking이 false여야 함 (일반 공격과 겹치지 않도록)
            if (isAttacking)
            {
                StopAttackState();
            }
        }

        // Attack Collider 자동 비활성화 로직 (일반 공격용)
        if (attackCollider != null && attackCollider.enabled)
        {
            if (!anim.GetBool(hashIsAttack) || !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossAttack"))
            {
                attackCollider.enabled = false;
            }
        }

        // Fire 스킬 중이거나 isExecutingSkill이 true이지만 Dash가 아닐 경우 (Thorn), 방향 전환을 하지 않습니다.
        // isFireSkillActive일 때도 FlipSprite를 완전히 막음
        // isHealingSkillActive일 때도 FlipSprite를 막음
        if (!isFireSkillActive && !isHealingSkillActive)
        {
            // isExecutingSkill이 false이거나 대쉬 중일 때만 FlipSprite 호출
            if (!isExecutingSkill || anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDashFull") || anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossDashFull"))
            {
                FlipSprite();
            }
        }
    }

    // 플레이어가 부활했을 때 호출될 이벤트 핸들러
    private void HandlePlayerRevived(Transform revivedPlayerTransform, Player revivedPlayerComponent)
    {
        if (revivedPlayerComponent.playerType == Player.PlayerType.Player1)
        {
            player1Transform = revivedPlayerTransform;
            player1Component = revivedPlayerComponent;
            Debug.Log("BossCtrl: Player1 has revived, re-targeting!");
        }
        else if (revivedPlayerComponent.playerType == Player.PlayerType.Player2)
        {
            player2Transform = revivedPlayerTransform;
            player2Component = revivedPlayerComponent;
            Debug.Log("BossCtrl: Player2 has revived, re-targeting!");
        }
    }

    // UpdateTargetPlayer 함수를 플레이어의 사망 여부만 체크하도록 변경
    void UpdateTargetPlayerState()
    {
        Transform prevTargetTransform = currentTargetTransform;

        // Player Component가 null이 아니면서 isDead라면 Transform도 null로 설정
        if (player1Component != null && player1Component.isDead)
        {
            player1Transform = null;
            player1Component = null;
        }

        if (player2Component != null && player2Component.isDead)
        {
            player2Transform = null;
            player2Component = null;
        }

        float distToP1 = (player1Transform != null) ? Vector2.Distance(transform.position, player1Transform.position) : float.MaxValue;
        float distToP2 = (player2Transform != null) ? Vector2.Distance(transform.position, player2Transform.position) : float.MaxValue;

        if (player1Transform == null && player2Transform == null)
        {
            currentTargetTransform = null;
            furthestPlayerTransform = null;
        }
        else if (player1Transform == null)
        {
            currentTargetTransform = player2Transform;
            furthestPlayerTransform = player2Transform;
        }
        else if (player2Transform == null)
        {
            currentTargetTransform = player1Transform;
            furthestPlayerTransform = player1Transform;
        }
        else
        {
            // 둘 다 살아있으면 더 가까운 플레이어를 currentTarget으로
            if (distToP1 <= distToP2)
            {
                currentTargetTransform = player1Transform;
                furthestPlayerTransform = player2Transform; // 가장 먼 플레이어 (Thorn용)
            }
            else
            {
                currentTargetTransform = player2Transform;
                furthestPlayerTransform = player1Transform; // 가장 먼 플레이어 (Thorn용)
            }
        }

        // currentTargetTransform이 변경되었을 때 공격 상태를 확실히 리셋
        if (prevTargetTransform != currentTargetTransform)
        {
            Debug.Log($"Target player changed from {prevTargetTransform?.name ?? "null"} to {currentTargetTransform?.name ?? "null"}");
            StopAttackState(); // 공격 상태를 확실히 종료하는 함수 호출
        }
    }

    // 공격 상태를 리셋하는 전용 함수
    void StopAttackState()
    {
        if (isAttacking)
        {
            StopCoroutine("AttackRoutine");
            isAttacking = false;
            anim.SetBool(hashIsAttack, false);

            if (attackCollider != null && attackCollider.enabled)
            {
                attackCollider.enabled = false;
                Debug.Log("StopAttackState: Attack Collider Disabled.");
            }
        }
    }

    void HandleMovementAndAttackDecision()
    {
        if (currentTargetTransform == null)
        {
            anim.SetBool(hashIsWalk, false);
            anim.SetBool(hashIsAttack, false);
            rb.linearVelocity = Vector2.zero;
            StopAttackState();
            return;
        }

        float distanceX = Mathf.Abs(transform.position.x - currentTargetTransform.position.x);

        if (!isExecutingSkill && !isHealingSkillActive) // 힐 스킬 중에도 이동 및 공격 중단
        {
            if (distanceX <= attackRangeX)
            {
                if (!isAttacking)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
            else // 공격 범위 밖에 있다면 이동 시작
            {
                MoveTowardsPlayer(currentTargetTransform);
                anim.SetBool(hashIsWalk, true);
                StopAttackState();
            }
        }
    }

    void MoveTowardsPlayer(Transform target)
    {
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
    }

    void FlipSprite()
    {
        // Fire 스킬 중이거나 isExecutingSkill이 true이지만 Dash가 아닐 경우 (Thorn), 방향 전환을 하지 않습니다.
        // isFireSkillActive일 때도 FlipSprite를 완전히 막음
        // isHealingSkillActive일 때도 FlipSprite를 막음
        if (isFireSkillActive || isHealingSkillActive || (isExecutingSkill && (!anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDashFull") && !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossDashFull"))))
        {
            return;
        }

        // 이동 방향에 따른 뒤집기
        if (rb.linearVelocity.x > 0.1f)
        {
            transform.localScale = new Vector3(initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
        }
        else if (rb.linearVelocity.x < -0.1f)
        {
            transform.localScale = new Vector3(-initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
        }
        // 이동 중이지 않을 때, 그리고 스킬 중이 아닐 때 (기본적으로 플레이어 방향 바라보기)
        else if (!isExecutingSkill && currentTargetTransform != null)
        {
            if (currentTargetTransform.position.x < transform.position.x)
            {
                transform.localScale = new Vector3(-initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
            }
            else
            {
                transform.localScale = new Vector3(initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
            }
        }
        // Dash 스킬 중일 때는 OnDashMovementStart에서 직접 스프라이트를 뒤집으므로 여기서는 추가 로직이 필요 없습니다.
    }


    // 애니메이션 이벤트 (Animator Controller에 연결)
    public void AnimationEvent_AttackStart()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            Debug.Log("AnimationEvent_AttackStart: Attack Collider Enabled");
        }
    }

    public void AnimationEvent_AttackEnd()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            Debug.Log("AnimationEvent_AttackEnd: Attack Collider Disabled");
        }
        anim.SetBool(hashIsAttack, false);
        isAttacking = false;
    }

    public void AnimationEvent_FireStart()
    {
        if (fireTrigger != null)
        {
            fireTrigger.enabled = true;
            Debug.Log($"[FireSkill Debug] FireTrigger ENABLED by AnimationEvent_FireStart at {Time.time}");
        }
        else
        {
            Debug.LogWarning("[FireSkill Debug] Fire Trigger is NULL in AnimationEvent_FireStart!");
        }
    }

    public void AnimationEvent_FireEnd()
    {
        if (fireTrigger != null)
        {
            fireTrigger.enabled = false;
            Debug.Log($"[FireSkill Debug] Fire Trigger DISABLED by AnimationEvent_FireEnd at {Time.time}");
        }
        else
        {
            Debug.LogWarning("[FireSkill Debug] Fire Trigger is NULL in AnimationEvent_FireEnd!");
        }
    }

    public void AnimationEvent_ThornSpawn()
    {
        StartCoroutine(SpawnThornsRoutine());
    }

    // 공격/스킬 코루틴

    IEnumerator AttackRoutine()
    {
        if (isAttacking) yield break;

        isAttacking = true;
        anim.SetBool(hashIsAttack, true);
        anim.SetBool(hashIsWalk, false);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(normalAttackAnimDuration);

        StopAttackState();
    }

    void SetNextSkillTime()
    {
        nextSkillTime = Time.time + Random.Range(minSkillInterval, maxSkillInterval);
    }

    void CheckSkillCooldown()
    {
        if (currentTargetTransform == null) return;

        if (Time.time >= nextSkillTime && !isExecutingSkill && !isHealingSkillActive) // 힐 스킬 중에는 다른 스킬 발동 안 함
        {
            ChooseAndExecuteSkill();
            SetNextSkillTime();
        }
    }

    void ChooseAndExecuteSkill()
    {
        isExecutingSkill = true;

        if (currentTargetTransform == null)
        {
            isExecutingSkill = false;
            return;
        }

        StopAttackState(); // 일반 공격 중단

        int skillChoice = Random.Range(0, 3); // 0: Thorn, 1: Fire, 2: Dash (힐 스킬은 랜덤 선택에 포함되지 않음)

        switch (skillChoice)
        {
            case 0:
                StartCoroutine(ThornSkillRoutine());
                break;
            case 1:
                StartCoroutine(FireSkillRoutine());
                break;
            case 2:
                StartCoroutine(DashSkillRoutine());
                break;
        }
    }

    IEnumerator ThornSkillRoutine()
    {
        isExecutingSkill = true;
        isFireSkillActive = false;
        isInvincible = false; // Thorn 스킬은 무적이 아님
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);
        anim.SetTrigger("isThorn");

        yield return new WaitForSeconds(thornAnimDuration);
        isExecutingSkill = false;
    }

    IEnumerator SpawnThornsRoutine()
    {
        if (furthestPlayerTransform == null)
        {
            Debug.LogWarning("ThornSkill: Furthest player not found or dead!");
            yield break;
        }

        for (int i = 0; i < thornSpawners.Count; i++)
        {
            if (thornSpawners[i] != null && thornPrefab != null)
            {
                GameObject thorn = Instantiate(thornPrefab, thornSpawners[i].position, Quaternion.identity);
                Debug.Log($"Thorn spawned from Spawner {i + 1}");
                yield return new WaitForSeconds(thornSpawnDelay);
            }
        }
    }

    IEnumerator FireSkillRoutine()
    {
        isExecutingSkill = true;
        isFireSkillActive = true;
        isInvincible = false; // Fire 스킬은 무적이 아님
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);

        anim.SetTrigger("isFire");
        Debug.Log($"[FireSkill Debug] FireSkillRoutine STARTED at {Time.time}. Trigger 'isFire' set.");

        yield return new WaitForSeconds(fireAnimDuration);

        isFireSkillActive = false;
        isExecutingSkill = false;
        Debug.Log($"[FireSkill Debug] FireSkillRoutine ENDED at {Time.time}. isExecutingSkill set to false.");
    }

    // 대쉬 스킬 코루틴
    IEnumerator DashSkillRoutine()
    {
        Debug.Log($"[DashSkill Debug] DashSkillRoutine STARTED at {Time.time}");
        isFireSkillActive = false;
        isInvincible = false; // Dash 스킬은 무적이 아님

        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);
        anim.ResetTrigger(hashIsDashFinished);

        anim.SetTrigger(hashIsDash);

        float totalAnimationDuration = dashReadyDuration + dashPerMoveDuration;
        float waitTimer = 0f;
        while (isExecutingSkill && waitTimer < totalAnimationDuration + 0.5f)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (isExecutingSkill)
        {
            Debug.LogError("[DashSkill Debug] Dash skill animation did not finish cleanly. Forcing end.");
            OnDashSkillFinished();
        }

        rb.linearVelocity = Vector2.zero;
        Debug.Log($"[DashSkill Debug] Dash Skill Routine Finished at {Time.time}");
    }

    // 애니메이션 이벤트 콜백 메서드 (Public 함수로 Animator에 연결)

    // 애니메이션 이벤트: 대쉬 움직임 시작 시 호출
    public void OnDashMovementStart()
    {
        if (!isExecutingSkill) return;

        Debug.Log($"[DashSkill Event] OnDashMovementStart called at {Time.time}");

        Transform target = currentTargetTransform;

        if (target == null || (player1Component != null && player1Component.isDead && player2Component != null && player2Component.isDead))
        {
            Debug.LogWarning("OnDashMovementStart: No valid target player or all players dead. Stopping dash movement.");
            rb.linearVelocity = Vector2.zero;
            OnDashSkillFinished();
            return;
        }

        float dashDirection = Mathf.Sign(target.position.x - transform.position.x);
        transform.localScale = new Vector3(dashDirection * initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);

        if (dashAttackCollider != null)
        {
            dashAttackCollider.enabled = true;
            Debug.Log($"[DashSkill Event] Dash Attack Collider ENABLED at {Time.time}");
        }
        Debug.Log($"[DashSkill Event] Boss dashing towards {target.name}");
    }

    // 애니메이션 이벤트: 대쉬 움직임 종료 시 호출
    public void OnDashMovementEnd()
    {
        if (!isExecutingSkill) return;

        Debug.Log($"[DashSkill Event] OnDashMovementEnd called at {Time.time}");
        rb.linearVelocity = Vector2.zero;

        if (dashAttackCollider != null)
        {
            dashAttackCollider.enabled = false;
            Debug.Log($"[DashSkill Event] Dash Attack Collider DISABLED at {Time.time}");
        }
    }

    // 애니메이션 이벤트: 전체 대쉬 스킬 애니메이션이 완전히 끝났을 때 호출
    public void OnDashSkillFinished()
    {
        Debug.Log($"[DashSkill Event] OnDashSkillFinished called at {Time.time}. Skill Finished.");
        isExecutingSkill = false;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger(hashIsDashFinished);

        if (dashAttackCollider != null && dashAttackCollider.enabled)
        {
            dashAttackCollider.enabled = false;
            Debug.Log($"[DashSkill Event] Dash Attack Collider FORCED DISABLED by OnDashSkillFinished at {Time.time}");
        }
    }

    // --- 새로운 힐 스킬 관련 코루틴 및 애니메이션 이벤트 ---

    // 힐 스킬 발동 전 지연 및 준비
    IEnumerator HealSkillDelayAndStart()
    {
        Debug.Log("[HealSkill] Checking for active skills before healing...");
        // 다른 스킬이 사용 중이라면 해당 스킬이 끝날 때까지 기다림
        while (isExecutingSkill)
        {
            yield return null;
        }
        Debug.Log("[HealSkill] Other skills finished. Waiting 2 seconds before healing.");
        yield return new WaitForSeconds(2f); // 2초 대기

        // 힐 스킬 시작
        StartCoroutine(HealSkillRoutine());
    }

    IEnumerator HealSkillRoutine()
    {
        Debug.Log("[HealSkill] HealSkillRoutine STARTED.");
        isHealingSkillActive = true;
        isExecutingSkill = true; // <--- 이 줄을 추가합니다. 힐 스킬도 '스킬 실행 중' 상태로 간주
        isInvincible = true; // 힐 스킬 시작 시 무적
        StopAttackState(); // 일반 공격 중단
        rb.linearVelocity = Vector2.zero; // 이동 중단
        anim.SetBool(hashIsWalk, false); // 걷기 애니메이션 중단

        anim.SetTrigger(hashIsHeal); // "isHeal" Trigger를 발동시켜 Stage4_BossHealReady 시작
        Debug.Log("[HealSkill] Animator parameter 'isHeal' Trigger set (HealReady animation).");

        yield return null;
    }

    // 애니메이션 이벤트: Stage4_BossHealReady 애니메이션이 끝났을 때 호출
    public void OnHealReadyAnimationEnd()
    {
        if (!isHealingSkillActive) return;

        Debug.Log($"[HealSkill Event] OnHealReadyAnimationEnd called at {Time.time}. Spawning slimes and transitioning to Heal animation.");

        // 힐 슬라임 4개 소환
        // 힐 슬라임 생성 코드 수정
        for (int i = 0; i < healSlimeCount; i++)
        {
            if (healSlimePrefab != null)
            {
                // 보스의 위치에서 생성
                Vector3 spawnPos = transform.position;

                GameObject slime = Instantiate(healSlimePrefab, spawnPos, Quaternion.identity);

                Rigidbody2D slimeRb = slime.GetComponent<Rigidbody2D>();
                if (slimeRb != null)
                {
                    // x, y 모두에 힘을 주되, y는 항상 양수(위쪽)로
                    Vector2 randomForce = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(2f, 4f)).normalized * 12f;
                    slimeRb.AddForce(randomForce, ForceMode2D.Impulse);
                }

                Debug.Log($"[HealSkill] Spawned HealSlime {i + 1}");
            }
        }

        // 힐 슬라임 타이머 시작
        StartCoroutine(HealSlimeTimerRoutine());

        // 이 시점에서 Stage4_BossHeal 애니메이션이 시작되어야 함.
        // Animator Controller에서 Stage4_BossHealReady -> Stage4_BossHeal로의 트랜지션이
        // Has Exit Time 등으로 자동으로 처리되도록 설정되었는지 확인.
    }

    // 힐 슬라임 타이머 및 체력 회복 로직
    IEnumerator HealSlimeTimerRoutine()
    {
        Debug.Log($"[HealSkill] HealSlimeTimerRoutine STARTED. Waiting {healSlimeDuration} seconds.");
        yield return new WaitForSeconds(healSlimeDuration); // 10초 대기

        Debug.Log("[HealSkill] HealSlimeTimer finished. Calculating healing...");

        // 남아있는 힐 슬라임 개수 확인 (태그로 찾기)
        GameObject[] remainingSlimes = GameObject.FindGameObjectsWithTag("HealSlime");
        int currentSlimeCount = remainingSlimes.Length;
        float totalHealAmount = currentSlimeCount * healAmountPerSlime;

        Debug.Log($"[HealSkill] Found {currentSlimeCount} remaining HealSlimes. Healing for {totalHealAmount} HP.");

        // 체력 회복
        TakeDamage(-totalHealAmount); // 음수 데미지는 체력 회복을 의미

        // 남아있는 슬라임 모두 제거
        foreach (GameObject slime in remainingSlimes)
        {
            if (slime != null)
            {
                Destroy(slime);
            }
        }
        // activeHealSlimes.Clear(); // 이 리스트는 더 이상 사용되지 않습니다.

        Debug.Log("[HealSkill] All HealSlimes destroyed. Triggering HealEnd animation.");

        // Stage4_BossHealEnd 애니메이션 시작 (Trigger로 변경)
        anim.SetTrigger(hashIsHealEnd); // HealEnd 트리거 발동
        // isHeal 트리거는 이미 소비되었으므로 다시 ResetTrigger 호출은 필요 없습니다.

        // OnHealSkillEnd 이벤트가 호출될 때까지 기다림
        yield return null; // 다음 프레임까지 기다림 (이벤트가 바로 호출될 수 있도록)
    }

    // 애니메이션 이벤트: Stage4_BossHealEnd 애니메이션이 끝났을 때 호출
    public void OnHealSkillEnd()
    {
        Debug.Log($"[HealSkill Event] OnHealSkillEnd called at {Time.time}. Heal skill finished.");
        isHealingSkillActive = false; // 힐 스킬 종료
        isInvincible = false;       // 무적 해제
        isExecutingSkill = false;   // <--- 이 줄을 추가합니다. 힐 스킬 종료 시 isExecutingSkill도 해제

        Debug.Log("[HealSkill] Boss returning to normal behavior.");
        Debug.Log($"[HealSkill Debug] After HealSkillEnd: isHealingSkillActive={isHealingSkillActive}, isInvincible={isInvincible}, isExecutingSkill={isExecutingSkill}");
    }


    // 트리거 충돌 처리 (플레이어 접촉 데미지 및 스킬 콜라이더)
    void OnTriggerStay2D(Collider2D other)
    {
        // 보스의 주 콜라이더 (mainCollider)가 플레이어와 닿았을 때 데미지
        // 힐 스킬 중에는 무적 상태이므로 데미지를 주지 않음
        if (!isInvincible && (other.CompareTag("Player1") || other.CompareTag("Player2")))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && !player.isDead)
            {
                player.TakeDamage(touchDamage);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 총알 충돌 처리
        if (other.CompareTag("AllyBullet"))
        {
            TakeDamage(10f);
            Destroy(other.gameObject); // 총알 제거
        }

        // 카타나 충돌 처리
        else if (other.CompareTag("Katana"))
        {
            TakeDamage(30f);
        }
        // 대쉬 공격 콜라이더에 플레이어가 닿았을 때 데미지 추가
        else if ((other.CompareTag("Player1") || other.CompareTag("Player2")) && dashAttackCollider != null && dashAttackCollider.enabled)
        {
            // 힐 스킬 중에는 무적 상태이므로 대쉬 데미지도 주지 않음
            if (isInvincible) return;

            Player player = other.GetComponent<Player>();
            if (player != null && !player.isDead)
            {
                player.TakeDamage(dashDamage);
                Debug.Log($"[DashSkill] Player took {dashDamage} damage from Dash Attack!");
            }
        }
    }

    // 보스 데미지 처리
    public void TakeDamage(float damage)
    {
        // 무적 상태이고, 받는 데미지가 양수일 경우 (즉, 공격받는 경우) 데미지 무시
        if (isInvincible && damage > 0)
        {
            Debug.Log($"[HealSkill] Boss is invincible! Ignored {damage} damage.");
            return;
        }

        currentHp -= damage;
        Debug.Log($"Boss took {damage} damage. Current HP: {currentHp}");

        if (bossHpBar != null)
        {
            bossHpBar.fillAmount = currentHp / maxHp;
        }

        if (currentHp <= 0)
        {
            // BossDefeat(); // 필요하다면 보스 처치 로직 추가
            Debug.Log("Boss Defeated!");
        }
    }
}