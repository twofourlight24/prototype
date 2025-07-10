using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // Rigidbody2D가 반드시 필요하도록 설정
public class SwordSkill : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform target; // 타겟 플레이어의 Transform
    [SerializeField] private float moveSpeed = 10f; // 검기 이동 속도
    [SerializeField] private int damageAmount = 50; // 검기 데미지

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (rb == null) Debug.LogError("SwordSkill: Rigidbody2D not found!");
        if (sr == null) Debug.LogWarning("SwordSkill: SpriteRenderer not found, flip may not work.");
    }

    private void Start()
    {
        Destroy(gameObject, 5f); // 5초 후에 자동으로 검기 삭제
    }

    // BossCtrl에서 타겟을 설정할 때 호출
    public void SetTargetPosition(Vector3 targetPosition)
    {
        // y좌표는 현재 검기 위치로 고정
        Vector3 fixedTarget = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
        Vector2 direction = (fixedTarget - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    void Update()
    {
        // 검기 플립 로직: x축 속도에 따라 스프라이트 방향 변경
        if (sr != null && rb.linearVelocity.x != 0)
        {
            // x 속도가 감소 (왼쪽으로 이동)하면 플립
            if (rb.linearVelocity.x < 0)
            {
                sr.flipX = true;
            }
            // x 속도가 증가 (오른쪽으로 이동)하면 플립 해제
            else
            {
                sr.flipX = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 피격 처리
        if (other.CompareTag("Player2")) // 'Player2' 태그에만 반응
        {
            // Player 스크립트는 Player2 오브젝트에 붙어있어야 합니다.
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
                Debug.Log($"[SwordSkill] Player2 hit! Took {damageAmount} damage.");
            }
            Destroy(gameObject); // 피격 후 검기 사라짐
        }
        // 'Katana' 태그 콜라이더에 닿으면 사라짐
        else if (other.CompareTag("Katana"))
        {
            Debug.Log("[SwordSkill] Hit Katana, destroying.");
            Destroy(gameObject);
        }
    }
}