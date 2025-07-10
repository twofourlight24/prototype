using UnityEngine;

public class SlimeController : MonoBehaviour
{
    // [Header("HP")] 제거 - 체력 시스템 필요 없음

    [Header("Target & Range")]
    public Transform player1; // 플레이어 1 트랜스폼
    public Transform player2; // 플레이어 2 트랜스폼
    private Transform target; // 현재 추적 대상 플레이어
    public float traceRangeX = 10f; // 플레이어를 추적하는 X 범위
    public float traceRangeY = 5f;  // 플레이어를 추적하는 Y 범위
    public float attackRange = 1f;  // 슬라임은 근접 공격 (점프해서 닿기)
    public float attackDamage = 10f; // 플레이어에게 줄 데미지
    public float attackDgeSmall = 5f; // 작은 슬라임이 플레이어에게 줄 데미지 (분열된 슬라임)

    [Header("Move - Jump Only")]
    public float jumpForce = 8f; // 점프 높이
    public float jumpInterval = 1.5f; // 점프 주기
    private float jumpTimer; // 다음 점프까지 남은 시간
    public float moveSpeed = 3f; // 점프 방향을 결정하는 수평 속도

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // 애니메이션이 있다면

    [Header("Ground Check")]
    public Transform groundCheck; // 지면 체크를 위한 Transform (슬라임 발 아래 자식으로 배치)
    public float groundCheckDistance = 0.1f; // 지면 감지 거리
    public LayerMask groundLayer; // 지면 레이어
    private bool isGrounded; // 현재 지면에 닿아 있는지

    [Header("Split Logic")]
    public GameObject smallSlimePrefab; // 분열될 작은 슬라임 프리팹 (여기에 다시 SlimeController가 붙어있어야 함)
    public int minSplitCount = 2; // 최소 분열 개수
    public int maxSplitCount = 3; // 최대 분열 개수
    public float splitSizeMultiplier = 0.7f; // 분열된 슬라임의 크기 배율

    private bool isSplitSlime = false; // 이 슬라임이 분열로 생성된 작은 슬라임인지 여부

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        jumpTimer = jumpInterval;

        if (!isSplitSlime)
        {
            gameObject.tag = "Enemy";
        }

        // 플레이어 트랜스폼 동적 할당
        if (player1 == null)
        {
            GameObject p1 = GameObject.FindWithTag("Player1");
            if (p1 != null) player1 = p1.transform;
        }
        if (player2 == null)
        {
            GameObject p2 = GameObject.FindWithTag("Player2");
            if (p2 != null) player2 = p2.transform;
        }
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("Player 1 또는 Player 2 트랜스폼이 할당되지 않았습니다! 플레이어 추적이 제대로 작동하지 않을 수 있습니다.", this);
        }
    }

    // 외부에서 호출하여 이 슬라임이 분열된 작은 슬라임임을 설정
    public void SetAsSplitSlime()
    {
        isSplitSlime = true;
        // 분열된 슬라임은 청소기 흡입 가능 상태로 시작
        gameObject.tag = "SmallMonster"; // 태그를 "SmallMonster"로 변경

        // 크기 조정
        transform.localScale = transform.localScale * splitSizeMultiplier;

        // 분열된 슬라임의 점프력이나 간격을 조절하고 싶다면 여기서 조정 가능
        // jumpForce *= 0.8f;
        // jumpInterval *= 0.7f;
    }


    private void Update()
    {
        UpdateTarget(); // 플레이어 추적
        CheckGround(); // 지면 확인
        HandleJumpMove(); // 점프 이동 로직

        // 분열된 슬라임 (SmallMonster 태그)일 경우 시각적 피드백
        if (isSplitSlime && spriteRenderer != null)
        {
            // 예시: 투명도 조절, 색상 변경 등 (흡입 가능한 상태임을 표시)
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f); // 약간 투명하게
        }
        else if (spriteRenderer != null) // 원래 슬라임은 불투명하게
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void UpdateTarget()
    {
        if (player1 == null || player2 == null) return;

        // 두 플레이어와의 거리 계산
        Vector2 pos = transform.position;
        Vector2 p1 = player1.position;
        Vector2 p2 = player2.position;

        float dx1 = Mathf.Abs(pos.x - p1.x);
        float dy1 = Mathf.Abs(pos.y - p1.y);
        float dx2 = Mathf.Abs(pos.x - p2.x);
        float dy2 = Mathf.Abs(pos.y - p2.y);

        bool p1InRange = dx1 <= traceRangeX && dy1 <= traceRangeY;
        bool p2InRange = dx2 <= traceRangeX && dy2 <= traceRangeY;

        if (p1InRange && p2InRange)
        {
            // 둘 다 범위 안이면 더 가까운 쪽 추적
            target = (dx1 * dx1 + dy1 * dy1) < (dx2 * dx2 + dy2 * dy2) ? player1 : player2;
        }
        else if (p1InRange)
        {
            target = player1;
        }
        else if (p2InRange)
        {
            target = player2;
        }
        else
        {
            target = null; // 둘 다 범위 밖이면 추적 안 함
        }
    }

    private void CheckGround()
    {
        // groundCheck Transform에서 아래로 레이캐스트를 쏴서 지면 감지
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        // Debug.DrawRay(groundCheck.position, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red); // 디버그용 (씬 뷰에 초록/빨강 선 표시)
    }

    private void HandleJumpMove()
    {
        if (target == null) return;

        jumpTimer -= Time.deltaTime; // 점프 타이머 감소

        // 지면에 닿아 있고, 점프 타이머가 0 이하일 때만 점프
        if (isGrounded && jumpTimer <= 0f)
        {
            SoundManager.Instance.PlaySlimeJump();
            Vector2 diff = target.position - transform.position;
            int dir = diff.x > 0 ? 1 : -1; // 타겟 방향 결정 (양수면 오른쪽, 음수면 왼쪽)

            // 슬라임 스프라이트 방향 전환
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = dir < 0; // dir이 음수이면 스프라이트 X축 반전
            }

            // Rigidbody의 linearVelocity를 직접 설정하여 점프
            // 수평 속도(moveSpeed * dir)와 수직 점프력(jumpForce) 적용
            rb.linearVelocity = new Vector2(moveSpeed * dir, jumpForce);

            jumpTimer = jumpInterval; // 점프 타이머 재설정

            // 애니메이션이 있다면 점프 애니메이션 트리거
            // if (animator != null) animator.SetTrigger("Jump");
        }
    }

    // 플레이어와 접촉 시 데미지 (슬라임의 공격)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player1"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
        if (collision.gameObject.CompareTag("Player2"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 로켓 불릿 피격 시
        if (collision.CompareTag("RocketBullet")) // 로켓 총알 태그
        {
            if (!isSplitSlime) // 아직 분열되지 않은 (크고 "Enemy" 태그를 가진) 슬라임만 분열
            {
                int splitCount = Random.Range(minSplitCount, maxSplitCount + 1); // 2~3개 랜덤 분열

                for (int i = 0; i < splitCount; i++)
                {
                    // 작은 슬라임 프리팹 생성
                    GameObject newSmallSlime = Instantiate(smallSlimePrefab, transform.position, Quaternion.identity);
                    SlimeController newSlimeController = newSmallSlime.GetComponent<SlimeController>();

                    if (newSlimeController != null)
                    {
                        newSlimeController.SetAsSplitSlime(); // 새로 생성된 슬라임을 '분열된 슬라임'으로 설정
                    }

                    // 분열 시 약간 퍼지도록 힘 가하기 (선택 사항, 통통 튀는 느낌)
                    Rigidbody2D newSlimeRb = newSmallSlime.GetComponent<Rigidbody2D>();
                    if (newSlimeRb != null)
                    {
                        Vector2 randomForce = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized * 3f; // 임의의 방향으로 힘
                        newSlimeRb.AddForce(randomForce, ForceMode2D.Impulse);
                    }
                }
                Destroy(gameObject); // 원본 슬라임 파괴 (분열되었으므로)
            }
            // 이미 분열된 작은 슬라임은 로켓에 맞아도 아무 일 없음 (청소기로만 처리)
            Destroy(collision.gameObject); // 로켓 총알 파괴
        }
    }
}