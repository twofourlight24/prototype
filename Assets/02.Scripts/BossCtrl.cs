using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가
using UnityEngine.UI;

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
    public Transform player1Transform;  
    public Transform player2Transform;  
    private Transform currentTargetTransform;  // 현재 추적 대상 플레이어의 Transform
    private Transform furthestPlayerTransform;  // 가장 먼 플레이어의 Transform

    // Player 컴포넌트 캐싱
    private Player player1Component;
    private Player player2Component;

    // 공격 설정
    [Header("Attack Settings")]
    public float attackRangeX = 2f;  // X좌표 기준 공격 범위
    public Collider2D attackCollider; // 공격 시 활성화될 콜라이더 (예: 근접 공격 판정)
    public float normalAttackAnimDuration = 1.0f;  // 일반 공격 애니메이션 지속 시간 (추정치, Animator에서 확인 필요)

    // 스킬 설정
    [Header("Skill Settings")]
    public float minSkillInterval = 7f;  // 최소 스킬 발동 주기 
    public float maxSkillInterval = 10f; // 최대 스킬 발동 주기
    private float nextSkillTime;
    public bool isExecutingSkill = false; // 보스 이동을 멈추는 스킬이 발동 중인지 여부

    // Thorn 스킬
    [Header("Thorn Skill")]
    public GameObject thornPrefab;
    public List<Transform> thornSpawners = new List<Transform>(); // 3개의 Thorn Spawner
    public float thornSpawnDelay = 0.1f; // 각 Thorn 스포너 간의 딜레이
    public float thornAnimDuration = 2.0f; 

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

    // Heal 스킬 
    [Header("Heal Skill")]
    public float healSkillHpThreshold = 333.33f; // 보스 체력의 2/3
    public GameObject healSlimePrefab; // 힐 슬라임 프리팹
    public int healSlimeCount = 4;
    public float healSlimeDuration = 10f; // 힐 슬라임 소환 후 체력 회복까지의 시간
    public float healAmountPerSlime = 50f; // 슬라임 하나당 회복되는 체력
    private bool isHealingSkillActive = false; // 힐 스킬이 현재 진행 중인지 여부
    private bool hasUsedHealSkill = false; // 힐 스킬을 이미 사용했는지 여부 (한 번만 발동)

    // 2페이즈 및 사망 관련
    [Header("Phase 2 & Death")]
    public bool is2PhaseActive = false; // 보스가 2페이즈 상태인지 여부
    public bool isDying = false; // 보스가 사망 애니메이션 중인지 여부
    public string endingSceneName = "EndingScene"; 

    // --- 2페이즈 스킬 (검기) 관련 변수 추가 ---
    [SerializeField] private float minSkillInterval_Phase2 = 5f; // 2페이즈 최소 스킬 주기
    [SerializeField] private float maxSkillInterval_Phase2 = 7f; // 2페이즈 최대 스킬 주기
    [SerializeField] private GameObject swordSkillPrefab; // 검기 프리팹 (인스펙터에서 연결)
    [SerializeField] private Transform swordSpawnPoint; // 검기 생성 위치 (보스 앞 등)

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D phase1Collider; // 1페이즈 콜라이더 (작은 크기)
    [SerializeField] private Collider2D phase2Collider; // 2페이즈 콜라이더 (큰 크기)

    private Vector3 initialLocalScale; // 보스의 초기 로컬 스케일 저장

    // 현재 공격 중인지 확인하는 플래그 (코루틴 중복 실행 방지 및 상태 관리)
    private bool isAttacking = false;
    private bool isInvincible = false; 

    // 애니메이터 파라미터 해시 (성능 최적화)
    private int hashIsAttack;
    private int hashIsDash;
    private int hashIsDashFinished;
    private int hashIsWalk;
    private int hashIsHeal; 
    private int hashIsHealEnd; 
    private int hashIs2Phase; 
    private int hashIsDie; 
    private int hashIsSword; 


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- 콜라이더 초기 설정 ---
        // 게임 시작 시 (또는 1페이즈 시작 시) 1페이즈 콜라이더만 활성화
        if (phase1Collider != null)
        {
            phase1Collider.enabled = true;
        }
        if (phase2Collider != null)
        {
            phase2Collider.enabled = false;
        }

        if (attackCollider != null) attackCollider.enabled = false;
        if (fireTrigger != null) fireTrigger.enabled = false;
        if (dashAttackCollider != null) dashAttackCollider.enabled = false;

        if (healSlimePrefab == null)
        {
            Debug.LogWarning("HealSlimePrefab is not assigned in BossCtrl. Heal skill may not work!");
        }
        // 엔딩 씬 이름이 설정되지 않았다면 경고
        if (string.IsNullOrEmpty(endingSceneName))
        {
            Debug.LogWarning("EndingSceneName is not set in BossCtrl. Boss defeat may not work correctly!");
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
        hashIsHeal = Animator.StringToHash("isHeal"); 
        hashIsHealEnd = Animator.StringToHash("isHealEnd"); 
        hashIs2Phase = Animator.StringToHash("is2Phase"); 
        hashIsDie = Animator.StringToHash("isDie"); 
        hashIsSword = Animator.StringToHash("isSword"); 
    }
    // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
    void OnDestroy()
    {
        Player.OnPlayerRevived -= HandlePlayerRevived;
    }

    void Start()
    {
        currentHp = maxHp;
        // SetNextSkillTime() 호출 시 인자를 넘겨 1페이즈 쿨타임으로 초기화
        SetNextSkillTime(minSkillInterval, maxSkillInterval);

        if (player1Transform == null && player2Transform == null)
        {
        }
        // 힐 스킬 임계값 계산
        healSkillHpThreshold = maxHp * (2f / 3f);
    }

    void Update()
    {
        if (bossHpBar != null)
        {
            bossHpBar.fillAmount = currentHp / maxHp;
        }

        if (isDying)
            return;

        // 사망 조건 체크
        if (currentHp <= 0 && !isDying)
        {
            Debug.Log("[Boss] HP reached 0. Initiating Boss Defeat sequence.");
            BossDefeat();
            return; // 사망 로직 시작 후 Update 루프 종료
        }

        UpdateTargetPlayerState();
        // 모든 플레이어가 죽었을 경우, 보스는 아무것도 하지 않음 (이동 및 스킬 중단)
        if (player1Transform == null && player2Transform == null)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(hashIsWalk, false);
            anim.SetBool(hashIsAttack, false);
            isExecutingSkill = false;
            isFireSkillActive = false;
            isHealingSkillActive = false; 
            is2PhaseActive = false; 

            StopAllCoroutines();
            isAttacking = false;
            isInvincible = false; 

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

        // 2페이즈 전환 조건 체크 (사망 로직보다 먼저 체크)
        if (!is2PhaseActive && currentHp <= phase2HpThreshold)
        {
            Debug.Log($"[Phase2] HP dropped below {phase2HpThreshold}. Initiating Phase 2 transition.");
            EnterPhase2();
            // 2페이즈 전환 애니메이션이 진행되는 동안은 다른 행동을 하지 않으므로 return
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
        // 특수 스킬(가시, 불, 대쉬, 힐, 검기) 중이 아닐 때만 일반 이동 및 스킬 쿨타임 체크
        if (!isExecutingSkill && !isHealingSkillActive)
        {
            HandleMovementAndAttackDecision();
            CheckSkillCooldown(); // 스킬 쿨타임 체크
        }
        else // 특수 스킬 실행 중일 때 (힐 스킬 포함)
        {
            // 대쉬 스킬 중이 아닐 때만 이동 중단
            if (!anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDashFull") && !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossDashFull"))
            {
                rb.linearVelocity = Vector2.zero;
                anim.SetBool(hashIsWalk, false);
            }
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
        }
        else if (revivedPlayerComponent.playerType == Player.PlayerType.Player2)
        {
            player2Transform = revivedPlayerTransform;
            player2Component = revivedPlayerComponent;
        }
    }

    // UpdateTargetPlayer 함수를 플레이어의 사망 여부만 체크하도록 변경
    void UpdateTargetPlayerState()
    {
        Transform prevTargetTransform = currentTargetTransform;
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

        float distToP1 = (player1Transform != null) ?
       Vector2.Distance(transform.position, player1Transform.position) : float.MaxValue;
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
                furthestPlayerTransform = player2Transform;
            }
            else
            {
                currentTargetTransform = player2Transform;
                furthestPlayerTransform = player1Transform;
            }
        }

        if (prevTargetTransform != currentTargetTransform)
        {
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
        // 2페이즈 전환 애니메이션 중에는 Flip을 하지 않도록 is2PhaseActive 조건 추가
        if (isFireSkillActive ||
            isHealingSkillActive ||
            (isExecutingSkill && (!anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDashFull")
            && !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossDashFull"))) ||
            is2PhaseActive) // 2페이즈 진입 애니메이션 중에도 플립 방지
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
    }

    // 애니메이션 이벤트 (Animator Controller에 연결)
    public void AnimationEvent_AttackStart()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void AnimationEvent_AttackEnd()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
        anim.SetBool(hashIsAttack, false);
        isAttacking = false;
    }

    public void AnimationEvent_FireStart()
    {
        if (fireTrigger != null)
        {
            fireTrigger.enabled = true;
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

    // --- 스킬 주기 설정 함수 (페이즈에 따라 쿨타임 범위 받도록 수정) ---
    void SetNextSkillTime(float minInterval, float maxInterval)
    {
        nextSkillTime = Time.time + Random.Range(minInterval, maxInterval);
        Debug.Log($"[BossCtrl] Next skill will be at: {nextSkillTime:F2} seconds.");
    }

    // --- 스킬 쿨타임 체크 함수 (2페이즈 스킬 분기 로직 추가) ---
    void CheckSkillCooldown()
    {
        if (currentTargetTransform == null) return;

        if (!isExecutingSkill && !isHealingSkillActive) // 힐 스킬 중에는 다른 스킬 발동 안 함 
        {
            if (Time.time >= nextSkillTime)
            {
                if (is2PhaseActive)
                {
                    // 2페이즈일 때 2페이즈 스킬 선택 및 실행
                    ChooseAndExecutePhase2Skill();
                    // 다음 2페이즈 스킬 시간 설정
                    SetNextSkillTime(minSkillInterval_Phase2, maxSkillInterval_Phase2);
                }
                else
                {
                    // 1페이즈일 때 기존 스킬 선택 및 실행
                    ChooseAndExecuteSkill();
                    // 다음 1페이즈 스킬 시간 설정
                    SetNextSkillTime(minSkillInterval, maxSkillInterval);
                }
            }
        }
    }

    // --- 1페이즈 스킬 선택 및 실행 함수 ---
    void ChooseAndExecuteSkill()
    {
        isExecutingSkill = true;
        if (currentTargetTransform == null)
        {
            isExecutingSkill = false;
            return;
        }

        StopAttackState(); // 일반 공격 중단

        // 2페이즈가 아닐 때만 기존 스킬 선택
        if (!is2PhaseActive)
        {
            int skillChoice = Random.Range(0, 3); // 0: Thorn, 1: Fire, 2: Dash

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
    }

    // --- 2페이즈 스킬 선택 및 실행 함수 ---
    void ChooseAndExecutePhase2Skill()
    {
        isExecutingSkill = true; // 스킬 실행 시작

        if (currentTargetTransform == null)
        {
            isExecutingSkill = false;
            return;
        }

        StopAttackState(); // 일반 공격 중단

        Debug.Log("[BossCtrl] Executing Phase 2: Sword Skill.");
        anim.SetTrigger(hashIsSword); // 검기 스킬 애니메이션 트리거 호출

        // 애니메이션 길이에 맞춰 시간 조절 
        StartCoroutine(SwordSkillExecutionRoutine(1.5f));
    }

    // --- 검기 스킬 실행 코루틴 (스킬 종료 처리) ---
    IEnumerator SwordSkillExecutionRoutine(float duration)
    {
        // 여기에서 스킬 애니메이션이 진행될 동안 대기
        yield return new WaitForSeconds(duration);

        isExecutingSkill = false; // 스킬 실행 완료
        Debug.Log("[BossCtrl] Sword Skill execution routine ended.");
    }
    public void SpawnSwordSkillProjectile()
    {
        if (swordSkillPrefab == null || swordSpawnPoint == null)
        {
            return;
        }

        GameObject sword = Instantiate(swordSkillPrefab, swordSpawnPoint.position, Quaternion.identity);
        SwordSkill swordSkill = sword.GetComponent<SwordSkill>();
        if (swordSkill != null)
        {
            // 'Player2' 태그를 가진 플레이어 찾기
            GameObject targetPlayer = GameObject.FindGameObjectWithTag("Player2");
            if (targetPlayer != null)
            {
                swordSkill.SetTarget(targetPlayer.transform);
            }
            else
            {
                Debug.LogWarning("Player2 not found for Sword Skill targeting! Sword will fly straight.");
            }
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
        while (isExecutingSkill && waitTimer < totalAnimationDuration + 0.5f) // isExecutingSkill 조건 추가
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (isExecutingSkill) // 애니메이션이 끝났음에도 isExecutingSkill이 true라면 강제 종료
        {
            OnDashSkillFinished();
        }

        rb.linearVelocity = Vector2.zero;
    }

    // 애니메이션 이벤트: 대쉬 움직임 시작 시 호출
    public void OnDashMovementStart()
    {
        if (!isExecutingSkill) return;

        Transform target = currentTargetTransform;
        if (target == null || (player1Component != null && player1Component.isDead && player2Component != null && player2Component.isDead))
        {
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
            
        }
    }

    // --- 힐 스킬 관련 코루틴 및 애니메이션 이벤트 ---

    // 힐 스킬 발동 전 지연 및 준비
    IEnumerator HealSkillDelayAndStart()
    {
        Debug.Log("[HealSkill] Checking for active skills before healing...");
        if (isExecutingSkill)
        {
            // 다른 스킬이 사용 중이라면 해당 스킬이 끝날 때까지 기다림
            while (isExecutingSkill)
            {
                yield return null;
            }
            Debug.Log("[HealSkill] Other skills finished. Waiting 2 seconds before healing.");
            yield return new WaitForSeconds(2f); // 2초 대기
        }
        // 스킬이 진행 중이 아니면 바로 힐 스킬 시작
        StartCoroutine(HealSkillRoutine());
    }

    IEnumerator HealSkillRoutine()
    {
        Debug.Log("[HealSkill] HealSkillRoutine STARTED.");
        isHealingSkillActive = true;
        isExecutingSkill = true;
        isInvincible = true; 
        StopAttackState(); 
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);

        anim.SetTrigger(hashIsHeal); 
  
        yield return null;
    }

    // 애니메이션 이벤트: Stage4_BossHealReady 애니메이션이 끝났을 때 호출
    public void OnHealReadyAnimationEnd()
    {
        if (!isHealingSkillActive) return;
        Debug.Log($"[HealSkill Event] OnHealReadyAnimationEnd called at {Time.time}. Spawning slimes and transitioning to Heal animation.");
        // 힐 슬라임 4개 소환
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

        Debug.Log("[HealSkill] All HealSlimes destroyed. Triggering HealEnd animation.");
        anim.SetTrigger(hashIsHealEnd); // HealEnd 트리거 발동

        yield return null; // 다음 프레임까지 기다림 (이벤트가 바로 호출될 수 있도록)
    }

    // 애니메이션 이벤트: Stage4_BossHealEnd 애니메이션이 끝났을 때 호출
    public void OnHealSkillEnd()
    {
        Debug.Log($"[HealSkill Event] OnHealSkillEnd called at {Time.time}. Heal skill finished.");
        isHealingSkillActive = false; // 힐 스킬 종료
        isInvincible = false; // 무적 해제
        isExecutingSkill = false; // 힐 스킬 종료 시 isExecutingSkill도 해제

        Debug.Log($"[HealSkill Debug] After HealSkillEnd: isHealingSkillActive={isHealingSkillActive}, isInvincible={isInvincible}, isExecutingSkill={isExecutingSkill}");
    }

    // --- 2페이즈 전환 로직 ---
    void EnterPhase2()
    {
        if (is2PhaseActive) return; // 이미 2페이즈라면 중복 실행 방지

        is2PhaseActive = true;
        Debug.Log("[Phase2] Boss entering Phase 2!");

        // 콜라이더 전환
        if (phase1Collider != null) phase1Collider.enabled = false;
        if (phase2Collider != null) phase2Collider.enabled = true;

        // 모든 현재 코루틴 중지 (일반 공격, 스킬 등)
        StopAllCoroutines();
        isAttacking = false;
        isExecutingSkill = false;
        isHealingSkillActive = false;
        isFireSkillActive = false;
        isInvincible = false; // 2페이즈 전환 애니메이션 중에는 무적 상태일 수 있지만, 일단 해제

        rb.linearVelocity = Vector2.zero; // 이동 중단
        anim.SetBool(hashIsWalk, false);
        anim.SetBool(hashIsAttack, false);

        // 2페이즈 전환 애니메이션 트리거 발동
        anim.SetTrigger(hashIs2Phase);

        // 2페이즈 진입 후 다음 스킬 시간 재설정 (2페이즈 스킬 쿨타임으로)
        SetNextSkillTime(minSkillInterval_Phase2, maxSkillInterval_Phase2); 
    }

    // --- 보스 사망 로직 ---
    void BossDefeat()
    {
        if (isDying) return; // 이미 죽는 중이라면 중복 실행 방지

        isDying = true;
        Debug.Log("[Boss] Boss is defeated! Initiating death sequence.");

        // 모든 행동 중지
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);
        anim.SetBool(hashIsAttack, false);
        isAttacking = false;
        isExecutingSkill = false;
        isHealingSkillActive = false;
        isFireSkillActive = false;
        isInvincible = true; // 사망 애니메이션 중에는 무적 상태 유지

        // 모든 콜라이더 비활성화 (플레이어 충돌 방지)
        if (phase1Collider != null) phase1Collider.enabled = false;
        if (phase2Collider != null) phase2Collider.enabled = false;
        if (attackCollider != null) attackCollider.enabled = false;
        if (fireTrigger != null) fireTrigger.enabled = false;
        if (dashAttackCollider != null) dashAttackCollider.enabled = false;

        // 힐 슬라임 포함 모든 잔여 오브젝트 정리
        GameObject[] remainingSlimes = GameObject.FindGameObjectsWithTag("HealSlime");
        foreach (GameObject slime in remainingSlimes)
        {
            if (slime != null) Destroy(slime);
        }

        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime; 
            anim.speed = 1f;
            Debug.Log("[BossCtrl] Boss Animator Update Mode changed to UnscaledTime for death animation.");
        }

        // 사망 애니메이션 트리거 발동
        anim.SetTrigger(hashIsDie);

        // 게임 시간 정지 
        Time.timeScale = 0f;
        Debug.Log("[Boss] Game time stopped.");

        StartCoroutine(LoadEndingSceneAfterDelay(3f)); // 사망 애니메이션 예상 시간 + 여유
    }

    IEnumerator LoadEndingSceneAfterDelay(float delay)
    {
        Debug.Log($"[Boss] Waiting {delay} seconds to load ending scene.");
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(endingSceneName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 총알 충돌 처리
        if (other.CompareTag("AllyBullet"))
        {
            TakeDamage(10f);
            Destroy(other.gameObject); // 총알 제거
        }

        // 카타나 충돌 처리 (1페이즈 검 데미지)
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
            return;
        }
        // 보스가 죽는 중이라면 더 이상 데미지를 받지 않음
        if (isDying && damage > 0)
        {
            return;
        }

        currentHp -= damage;
        Debug.Log($"Boss took {damage} damage. Current HP: {currentHp}");
        if (bossHpBar != null)
        {
            bossHpBar.fillAmount = currentHp / maxHp;
        }
    }
}
