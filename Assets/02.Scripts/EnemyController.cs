using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 100f;
    private float curHp;
    public Image hpBar;

    [Header("Target & Range")]
    public Transform player1;
    public Transform player2;
    private Transform target;
    public float traceRangeX = 10f;
    public float traceRangeY = 3f;
    public float attackRange = 5f;

    [Header("Move")]
    public float moveSpeed = 2f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // 추가

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Shoot")]
    public Transform shootPos;
    public GameObject bulletPrefab;
    public float fireRate = 2f;
    private float fireCooldown = 0f;

    [Header("Mark")]
    private bool isMarked = false;
    private float markTimer = 0f;
    public float markDuration = 3f;

    [Header("Mark Sprites")]
    public Sprite normalSprite;
    public Sprite markedSprite;

    private void Start()
    {
        curHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>(); // 추가

        if (spriteRenderer != null && normalSprite != null)
            spriteRenderer.sprite = normalSprite;

        if (animator != null)
            animator.enabled = true; // 기본 상태에서 애니메이션 활성화
    }

    private void Update()
    {
        UpdateTarget();
        HandleMark();
        FacePlayer();
        MoveAndTrack();
        HandleShooting();
        UpdateHpBar();
    }

    private void UpdateTarget()
    {
        float d1 = Vector2.Distance(transform.position, player1.position);
        float d2 = Vector2.Distance(transform.position, player2.position);
        target = d1 < d2 ? player1 : player2;
    }

    private void HandleMark()
    {
        if (isMarked)
        {
            markTimer -= Time.deltaTime;

            if (animator != null)
                animator.enabled = false; // 마크드 상태에서 애니메이션 비활성화

            if (spriteRenderer != null && markedSprite != null)
                spriteRenderer.sprite = markedSprite;

            if (markTimer <= 0f)
            {
                isMarked = false;

                if (spriteRenderer != null && normalSprite != null)
                    spriteRenderer.sprite = normalSprite;

                if (animator != null)
                    animator.enabled = true; // 마크 해제 시 애니메이션 다시 활성화
            }
        }
    }
    private void FacePlayer()
    {
        if (target == null || spriteRenderer == null) return;
        spriteRenderer.flipX = (target.position.x > transform.position.x);
    }
    private void MoveAndTrack()
    {
        if (target == null) return;

        Vector2 diff = target.position - transform.position;
        float dx = Mathf.Abs(diff.x);
        float dy = Mathf.Abs(diff.y);
        bool shouldChase = dx <= traceRangeX && dy <= traceRangeY;

        if (shouldChase)
        {
            int dir = diff.x > 0 ? 1 : -1;

            // 낭떠러지 체크
            Vector2 groundCheckPos = groundCheck.position + Vector3.right * dir * 0.3f;
            bool isGroundAhead = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistance, groundLayer);

            if (isGroundAhead)
            {
                rb.linearVelocity = new Vector2(moveSpeed * dir, rb.linearVelocity.y);
                spriteRenderer.flipX = dir < 0;
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // 낭떠러지면 정지
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void HandleShooting()
    {
        if (target == null) return;
        if (isMarked) return; // 마크 상태면 총알을 쏘지 않음

        float dx = Mathf.Abs(target.position.x - transform.position.x);
        if (dx <= attackRange)
        {
            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                Vector2 shootDir = (target.position.x < transform.position.x) ? Vector2.left : Vector2.right;
                GameObject bullet = Instantiate(bulletPrefab, shootPos.position, Quaternion.identity);
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = shootDir * 8f;
                }
                fireCooldown = fireRate;

                // 공격 애니메이션 트리거
                if (animator != null)
                    animator.SetBool("isAttack", true); // 애니메이션에서 이벤트로 false로 꺼줌
            }
        }
    }

    // 아래 메서드를 추가하세요 (애니메이션 이벤트에서 호출)
    public void OnAttackAnimationEnd()
    {
        if (animator != null)
            animator.SetBool("isAttack", false);
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = curHp / maxHp;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!isMarked) return;

        curHp -= damage;
        if (curHp <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("MarkBullet"))
        {
            isMarked = true;
            markTimer = markDuration;
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("AllyBullet"))
        {
            Destroy(collision.gameObject);
            TakeDamage(20f);
        }
    }
    public void FlipAllChildrenHorizontally()
    {
        foreach (Transform child in transform)
        {
            Vector3 localPos = child.localPosition;
            localPos.x = -localPos.x;
            child.localPosition = localPos;

            Vector3 localScale = child.localScale;
            localScale.x = -localScale.x;
            child.localScale = localScale;
        }
    }
}
