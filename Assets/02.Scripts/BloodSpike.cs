using UnityEngine;

public class BloodSpike : MonoBehaviour
{
    [SerializeField] private Collider2D spikeCollider;

    void Awake()
    {
        if (spikeCollider == null)
            spikeCollider = GetComponent<Collider2D>();
        if (spikeCollider != null)
            spikeCollider.enabled = false;
    }

    // 1. 애니메이션 이벤트: 콜라이더 활성화
    public void AnimationEvent_EnableCollider()
    {
        if (spikeCollider != null)
            spikeCollider.enabled = true;
    }

    // 2. 애니메이션 이벤트: 콜라이더 비활성화
    public void AnimationEvent_DisableCollider()
    {
        if (spikeCollider != null)
            spikeCollider.enabled = false;
    }

    // 3. 애니메이션 이벤트: 자기 자신 파괴
    public void AnimationEvent_DestroySelf()
    {
        Destroy(gameObject);
    }

    // 플레이어 충돌 시 데미지
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!spikeCollider || !spikeCollider.enabled) return;

        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(70f);
            }
        }
    }
}