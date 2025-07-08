using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BossCtrl : MonoBehaviour
{
    // 보스 핵심 스탯
    [Header("Boss Core Stats")]
    public float maxHp = 500f;
    public float currentHp;
    public float phase2HpThreshold = 200f; // 2페이즈 전환 체력
    public float moveSpeed = 3f;
    public float touchDamage = 30f; // 플레이어와 닿았을 때 주는 데미지
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

    // Dash 스킬 (수정된 부분)
    [Header("Dash Skill")]
    public float dashSpeed = 15f; // 대쉬 시 보스의 순간 속도
    public float dashReadyDuration = 0.608f; // 대쉬 준비 애니메이션 시간 (참고용)
    public float dashPerMoveDuration = 0.480f; // 실제 대쉬 이동 애니메이션 시간 (참고용)
    // 이전의 dashCount, dashDistanceX, dashPauseBetweenDashes는 이제 필요 없습니다. (2회 대쉬가 아니므로)

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D mainCollider; // 보스의 주 콜라이더 (플레이어 접촉 데미지용)

    private Vector3 initialLocalScale; // 보스의 초기 로컬 스케일 저장

    // 현재 공격 중인지 확인하는 플래그 (코루틴 중복 실행 방지 및 상태 관리)
    private bool isAttacking = false;

    // 애니메이터 파라미터 해시 (성능 최적화)
    private int hashIsAttack;
    private int hashIsDash; // "isDash" 트리거 (새로 통합된 대쉬 애니메이션 시작용)
    private int hashIsDashFinished; // "isDashFinished" 트리거 (대쉬 스킬 종료용)
    private int hashIsWalk; // "isWalk" bool 파라미터 추가

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<BoxCollider2D>();

        if (attackCollider != null) attackCollider.enabled = false;
        if (fireTrigger != null) fireTrigger.enabled = false;

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
        hashIsDash = Animator.StringToHash("isDash"); // "isDash" 트리거 이름에 맞춰야 합니다.
        hashIsDashFinished = Animator.StringToHash("isDashFinished"); // "isDashFinished" 트리거 이름에 맞춰야 합니다.
        hashIsWalk = Animator.StringToHash("isWalk"); // "isWalk" 파라미터 이름에 맞춰야 합니다.
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
            rb.linearVelocity   = Vector2.zero; // linearVelocity 대신 velocity 사용 (Rigidbody2D는 linearVelocity 없음)
            anim.SetBool(hashIsWalk, false);
            anim.SetBool(hashIsAttack, false);
            isExecutingSkill = false;
            isFireSkillActive = false;

            StopAllCoroutines();
            isAttacking = false;

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
            return;
        }

        // 플레이어가 한 명이라도 살아있다면
        if (!isExecutingSkill) // 특수 스킬(가시, 불, 대쉬) 중이 아닐 때만 일반 이동 및 스킬 쿨타임 체크
        {
            HandleMovementAndAttackDecision();
            CheckSkillCooldown(); // 스킬 쿨타임 체크
        }
        else // 특수 스킬 실행 중일 때
        {
            // 스킬 중에는 이동 중단 (대쉬 스킬은 이동 로직이 다르므로 예외 처리)
            // Fire 스킬 중에도 이동 중단
            // 현재 애니메이터의 어떤 레이어에 Dash 애니메이션이 있는지 확인 필요 (일반적으로 Base Layer(0)이 아니면 LayerIndex 명시)
            // 여기서는 Dash가 Skill Layer (1)에 있다고 가정
            if (!anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDash_Combined_Single"))
            {
                rb.linearVelocity = Vector2.zero; // 이동 중단
                anim.SetBool(hashIsWalk, false);
            }
            // 스킬 중에는 isAttacking이 false여야 함 (일반 공격과 겹치지 않도록)
            if (isAttacking)
            {
                StopAttackState();
            }
        }

        // Attack Collider 자동 비활성화 로직
        if (attackCollider != null && attackCollider.enabled)
        {
            if (!anim.GetBool(hashIsAttack) || !anim.GetCurrentAnimatorStateInfo(0).IsName("Stage4_BossAttack"))
            {
                attackCollider.enabled = false;
            }
        }

        // Fire 스킬 중이거나 isExecutingSkill이 true이지만 Dash가 아닐 경우 (Thorn), 방향 전환을 하지 않습니다.
        // isFireSkillActive는 Fire 스킬 중일 때 FlipSprite를 완전히 막기 위함
        if (!isFireSkillActive)
        {
            // isExecutingSkill이 false이거나 대쉬 중일 때만 FlipSprite 호출
            // Dash의 애니메이션 상태 이름은 "Stage4_BossDash_Combined_Single"로 가정
            if (!isExecutingSkill || anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDash_Combined_Single"))
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

        if (!isExecutingSkill)
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
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y); // linearVelocity 대신 velocity 사용
    }

    void FlipSprite()
    {
        // Fire 스킬 중이거나 isExecutingSkill이 true이지만 Dash가 아닐 경우 (Thorn), 방향 전환을 하지 않습니다.
        // Dash의 애니메이션 상태 이름은 "Stage4_BossDash_Combined_Single"로 가정
        if (isFireSkillActive || (isExecutingSkill && !anim.GetCurrentAnimatorStateInfo(1).IsName("Stage4_BossDash_Combined_Single")))
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
        else if (!isExecutingSkill && currentTargetTransform != null) // isExecutingSkill 검사는 이미 위에서 필터링됨
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

    // ThornEnd 애니메이션 이벤트는 isExecutingSkill을 false로 바꾸는 용도로 사용될 수 있습니다.
    // 하지만 ThornSkillRoutine이 스스로 isExecutingSkill을 false로 만드므로, 이 이벤트가 필수는 아닙니다.
    // public void AnimationEvent_ThornEnd() { /* handled by routine */ }

    // DashReadyEnd 애니메이션 이벤트는 더 이상 사용되지 않습니다. 단일 애니메이션으로 합쳐졌기 때문입니다.
    // public void AnimationEvent_DashReadyEnd() { /* Not used with combined animation */ }

    // DashEnd 애니메이션 이벤트는 더 이상 사용되지 않습니다. OnDashMovementEnd에서 처리합니다.
    // public void AnimationEvent_DashEnd() { /* Handled by OnDashMovementEnd */ }


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

        if (Time.time >= nextSkillTime && !isExecutingSkill)
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

        StopAttackState();

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
                StartCoroutine(DashSkillRoutine()); // <-- 여기가 수정된 부분입니다.
                break;
        }
    }

    IEnumerator ThornSkillRoutine()
    {
        isExecutingSkill = true;
        isFireSkillActive = false;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);
        anim.SetTrigger("isThorn"); // "isThorn" 트리거 이름에 맞춰야 합니다.

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

        // Vector3 targetPos = furthestPlayerTransform.position; // 현재 사용되지 않음

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
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(hashIsWalk, false);

        anim.SetTrigger("isFire");
        Debug.Log($"[FireSkill Debug] FireSkillRoutine STARTED at {Time.time}. Trigger 'isFire' set.");

        yield return new WaitForSeconds(fireAnimDuration);

        isFireSkillActive = false;
        isExecutingSkill = false;
        Debug.Log($"[FireSkill Debug] FireSkillRoutine ENDED at {Time.time}. isExecutingSkill set to false.");
    }

    // 대쉬 스킬 코루틴 (단일 대쉬, 애니메이션 이벤트 기반으로 전면 수정)
    IEnumerator DashSkillRoutine()
    {
        if (isExecutingSkill) yield break;

        isExecutingSkill = true;
        isFireSkillActive = false;
        // dashCount는 단일 대쉬이므로 더 이상 필요 없습니다.

        rb.linearVelocity = Vector2.zero; // 대쉬 시작 전 보스 이동 중단
        anim.SetBool(hashIsWalk, false); // 걷기 애니메이션 중단
        anim.ResetTrigger(hashIsDashFinished); // 혹시 남았을 종료 트리거 리셋

        // 합쳐진 단일 대쉬 애니메이션 시작 트리거
        anim.SetTrigger(hashIsDash);
        Debug.Log($"[DashSkill Debug] Single Dash Animation Triggered at {Time.time}");

        // 애니메이션이 완전히 끝날 때까지 기다립니다.
        // 실제 움직임은 애니메이션 이벤트에서 제어됩니다.
        // 이 시간은 'Stage4_BossDash_Combined_Single' 애니메이션 클립의 총 길이와 일치해야 합니다.
        float totalAnimationDuration = dashReadyDuration + dashPerMoveDuration;
        float waitTimer = 0f;
        while (isExecutingSkill && waitTimer < totalAnimationDuration + 0.5f) // 안전을 위해 약간 여유 시간 추가
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        // 만약 OnDashSkillFinished 이벤트가 호출되지 않고 코루틴이 종료될 경우 대비
        if (isExecutingSkill)
        {
            Debug.LogError("[DashSkill Debug] Dash skill animation did not finish cleanly. Forcing end.");
            OnDashSkillFinished(); // 강제로 스킬 종료 처리
        }

        rb.linearVelocity = Vector2.zero; // 최종적으로 속도 0으로 (애니메이션 이벤트에서 처리하지만 안전 장치)
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
            OnDashSkillFinished(); // 즉시 스킬 종료 처리
            return;
        }

        float dashDirection = Mathf.Sign(target.position.x - transform.position.x);
        transform.localScale = new Vector3(dashDirection * initialLocalScale.x, initialLocalScale.y, initialLocalScale.z); // 스프라이트 뒤집기
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y); // 대쉬 속도 설정
        Debug.Log($"[DashSkill Event] Boss dashing towards {target.name}");
    }

    // 애니메이션 이벤트: 대쉬 움직임 종료 시 호출
    public void OnDashMovementEnd()
    {
        if (!isExecutingSkill) return;

        Debug.Log($"[DashSkill Event] OnDashMovementEnd called at {Time.time}");
        rb.linearVelocity = Vector2.zero; // 대쉬 움직임 중단
    }

    // 애니메이션 이벤트: 전체 대쉬 스킬 애니메이션이 완전히 끝났을 때 호출
    public void OnDashSkillFinished()
    {
        Debug.Log($"[DashSkill Event] OnDashSkillFinished called at {Time.time}. Skill Finished.");
        isExecutingSkill = false;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger(hashIsDashFinished); // Animator에게 스킬이 끝났음을 알림 (Idle/Walk 등으로 전환)
    }

    // 트리거 충돌 처리 (플레이어 접촉 데미지)
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && !player.isDead)
            {
                player.TakeDamage(touchDamage);
            }
        }
    }

    // 보스 데미지 처리
    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"Boss took {damage} damage. Current HP: {currentHp}");

        if (currentHp <= phase2HpThreshold && currentHp > 0)
        {
            // StartPhase2(); // 필요하다면 2페이즈 시작 로직 추가
        }

        if (currentHp <= 0)
        {
            // BossDefeat(); // 필요하다면 보스 처치 로직 추가
            Debug.Log("Boss Defeated!");
        }
    }
}