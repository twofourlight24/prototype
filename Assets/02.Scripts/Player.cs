using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    // 플레이어 부활 시 호출될 이벤트 (Transform과 Player 컴포넌트 모두 전달)
    public static event System.Action<Transform, Player> OnPlayerRevived;

    public enum PlayerType { Player1, Player2 }
    public PlayerType playerType = PlayerType.Player1;

    // WeaponType에 Katana 추가
    public enum WeaponType { DefaultGun, RocketLauncher, VacuumCleaner, SawtoothGun, Katana }

    //--- 플레이어 변수
    float m_MaxHp = 100.0f;
    public float m_CurHp = 100.0f;
    public Image m_HpBar = null;
    public TextMeshProUGUI TextHp = null;
    public float m_DamageCool = 1.0f;
    public float m_LavaCool = 0.25f;

    //--- 플레이어 움직임 관련 변수
    float h = 0.0f;
    public float m_JumpForce = 10.0f;
    public float m_MoveSpeed = 2.6f;
    Vector3 m_DirVec;

    private Rigidbody2D rb;

    // --- 바닥 체크용 변수 추가 ---
    [Header("Ground Check")]
    public Transform groundCheck1;
    public Transform groundCheck2;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;
    public bool isGrounded = false;
    private bool isDoubleJumpAvailable = false;

    [Header("Layer Settings")]
    public string playerAliveLayerName = "Player"; // 살아있을 때의 레이어 이름
    public string playerDeadLayerName = "Player_Dead"; // 죽었을 때의 레이어 이름

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackUpForce = 3f;

    //--- 총 관련 변수 (카타나에서는 사용되지 않음)
    public GameObject m_BulletPrefab = null;
    public Transform m_ShootPos; // 총알/흡입 효과가 시작될 위치
    public Image m_ReloadImage = null;
    float reloadTimer = 0.0f;
    public float shootForce = 10.0f;
    public float m_ShootCool = 0.5f;
    float ShootTimer = 0.0f;

    // --- 각 총 종류별 스탯 (DefaultGun) ---
    [Header("Default Gun Stats")]
    public GameObject defaultGunPrefab; // 기본 총 모델 추가
    public Sprite defaultGunUIIcon; // 기본 총 UI 아이콘 추가
    public GameObject defaultBulletPrefab;
    public float defaultGunShootForce = 10.0f;
    public float defaultGunShootCool = 0.5f;
    public int defaultGunBulletMaxCount = 12;
    public float defaultGunReloadTime = 1.5f;

    // --- 각 총 종류별 스탯 (RocketLauncher) ---
    [Header("Rocket Launcher Stats")]
    public GameObject rocketLauncherPrefab; // 로켓 런처 모델 추가
    public Sprite rocketLauncherUIIcon; // 로켓 런처 UI 아이콘 추가
    public GameObject rocketPrefab;
    public float rocketShootForce = 15.0f;
    public float rocketFireRate = 2.0f;
    public int rocketMaxCount = 3;
    public float rocketReloadTime = 3.0f;

    // --- 각 총 종류별 스탯 (VacuumCleaner) ---
    [Header("Vacuum Cleaner Stats")]
    public GameObject vacuumCleanerPrefab; // 진공 청소기 모델 추가
    public Sprite vacuumCleanerUIIcon; // 진공 청소기 UI 아이콘 추가
    // 흡수 이미지를 가지고 있고 판정을 하는 게임 오브젝트 (VacuumObject)
    public GameObject vacuumObject; // <--- 변경: 이제 이 GameObject가 흡수 이미지와 콜라이더를 가짐
    public float suckRadius = 3f; // 흡입 범위 (Gizmo 용도로 유지, 실제 콜라이더 크기로 조절)
    public float suckForce = 10f; // 흡입력
    public float consumeDistance = 0.5f; // 소멸 거리
    public LayerMask smallMonsterLayer; // 흡수할 대상 레이어

    // --- 각 총 종류별 스탯 (SawtoothGun) ---
    [Header("Sawtooth Gun Stats")]
    public GameObject sawtoothGunPrefab; // 톱니 총 모델 추가
    public Sprite sawtoothGunUIIcon; // 톱니 총 UI 아이콘 추가
    public GameObject sawtoothBulletPrefab;
    public float sawtoothGunShootForce = 12.0f;
    public float sawtoothGunFireRate = 0.7f;
    public int sawtoothGunMaxCount = 5;
    public float sawtoothGunReloadTime = 1.8f;

    // --- 카타나 스탯 및 관련 변수 추가 ---
    [Header("Katana Stats")]
    public GameObject katanaPrefab; // 카타나 모델 추가
    public Sprite katanaUIIcon; // 카타나 UI 아이콘 추가
    public float katanaAttackCooldown = 0.7f; // 공격 후 쿨타임
    public float katanaColliderActiveDuration = 0.2f; // 카타나 콜라이더가 활성화될 지속 시간
    public Collider2D katanaAttackCollider; // 카타나의 공격 범위 Collider (Is Trigger 체크)
    private bool isAttacking = false; // 카타나 공격 중인지 여부 (코드로 제어)

    // --- 현재 무기 상태 ---
    private float currentShootTimer = 0.0f;
    private int currentBulletCount;
    private bool isReloading = false;
    private int currentMaxBulletCount;
    private float currentReloadTime;
    private float currentFireRate;

    [Header("Weapon Configuration")]
    public WeaponType currentWeaponType;
    [Tooltip("플레이어가 스왑할 수 있는 무기 목록. 첫 번째 무기가 시작 무기가 됩니다.")]
    public List<WeaponType> swappableWeapons = new List<WeaponType>();
    private int currentWeaponIndex = 0;
    private float weaponSwapCooldown = 1.0f;
    private float weaponSwapTimer = 0f;
    private Dictionary<WeaponType, int> weaponBulletDict = new Dictionary<WeaponType, int>();


    public TextMeshProUGUI BulletCount;
    [Header("UI References")]
    public Image currentWeaponUIIcon; // 현재 무기 UI 이미지를 표시할 Image 컴포넌트

    // --- 부활 관련 변수 추가 ---
    public bool isDead = false;
    private bool isBeingRevived = false; // 사용되지 않는 변수
    private float reviveProgress = 0f;
    public float reviveRequired = 10f;
    private Player otherPlayer;
    private bool isOverlappingWithOther = false;
    private Coroutine blinkCoroutine; // 깜빡임 코루틴 핸들

    // 부활 UI
    public Image reviveImage;
    public Image reviveBar;

    // Collider 참조
    public Collider2D mainPlayerCollider;
    public Collider2D reviveDetectionTrigger; // 부활 감지용 트리거

    //---애니메이션 관련 변수
    SpriteRenderer SpriteRenderer;
    Animator Anim;

    //--- 입력 키 설정
    private KeyCode leftKey;
    private KeyCode rightKey;
    private KeyCode jumpKey;
    private KeyCode shootKey;
    private KeyCode reloadKey;
    private KeyCode swapWeaponKey; // 무기 스왑 키

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        Anim = GetComponent<Animator>();

        if (mainPlayerCollider == null)
        {
            mainPlayerCollider = GetComponent<Collider2D>();
        }
        foreach (var otherPlayerComp in FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (otherPlayerComp == this) continue;

            if (mainPlayerCollider != null && otherPlayerComp.mainPlayerCollider != null)
            {
                // 두 플레이어의 메인 콜라이더는 서로 무시
                Physics2D.IgnoreCollision(mainPlayerCollider, otherPlayerComp.mainPlayerCollider, true);
            }
            otherPlayer = otherPlayerComp;
        }

        if (reviveBar != null)
        {
            reviveBar.fillAmount = 0f;
            reviveImage.gameObject.SetActive(false);
        }
        if (m_ReloadImage != null)
        {
            m_ReloadImage.fillAmount = 0f;
            m_ReloadImage.gameObject.SetActive(false);
        }

        // VacuumObject 초기 비활성화 (vacuumCleanerPrefab과 별개로 제어)
        if (vacuumObject != null)
        {
            vacuumObject.SetActive(false);
        }

        // 카타나 공격 콜라이더 초기 비활성화
        if (katanaAttackCollider != null)
        {
            katanaAttackCollider.enabled = false;
        }

        if (playerType == PlayerType.Player1)
        {
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
            jumpKey = KeyCode.W;
            shootKey = KeyCode.F;
            reloadKey = KeyCode.R;
            swapWeaponKey = KeyCode.LeftShift; // Player1 스왑 키
        }
        else if (playerType == PlayerType.Player2)
        {
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            jumpKey = KeyCode.UpArrow;
            shootKey = KeyCode.Return;
            reloadKey = KeyCode.RightControl;
            swapWeaponKey = KeyCode.RightShift; // Player2 스왑 키
        }

        // 스왑 가능한 무기 목록이 비어있지 않다면, 첫 번째 무기로 초기화
        if (swappableWeapons.Count > 0)
        {
            currentWeaponType = swappableWeapons[0];
            currentWeaponIndex = 0;
        }
        InitializeWeaponStats();
    }

    void InitializeWeaponStats()
    {
        // 모든 무기 오브젝트를 우선 비활성화
        if (defaultGunPrefab != null) defaultGunPrefab.SetActive(false);
        if (rocketLauncherPrefab != null) rocketLauncherPrefab.SetActive(false);
        if (vacuumCleanerPrefab != null) vacuumCleanerPrefab.SetActive(false);
        if (sawtoothGunPrefab != null) sawtoothGunPrefab.SetActive(false);
        if (katanaPrefab != null) katanaPrefab.SetActive(false);

        if (Anim != null)
        {
            Anim.SetBool("IsKatanaEquipped", false);
            Anim.SetFloat("Speed", 0);
            Anim.SetBool("speed", true);
        }

        Sprite newUIIcon = null; // 새로 설정할 UI 아이콘

        switch (currentWeaponType)
        {
            case WeaponType.DefaultGun:
                currentMaxBulletCount = defaultGunBulletMaxCount;
                currentFireRate = defaultGunShootCool;
                currentReloadTime = defaultGunReloadTime;
                if (defaultGunPrefab != null) defaultGunPrefab.SetActive(true); // 해당 총 모델 활성화
                newUIIcon = defaultGunUIIcon;
                break;
            case WeaponType.RocketLauncher:
                currentMaxBulletCount = rocketMaxCount;
                currentFireRate = rocketFireRate;
                currentReloadTime = rocketReloadTime;
                if (rocketLauncherPrefab != null) rocketLauncherPrefab.SetActive(true); // 해당 총 모델 활성화
                newUIIcon = rocketLauncherUIIcon;
                break;
            case WeaponType.VacuumCleaner:
                currentMaxBulletCount = 0; // 진공청소기는 총알 개념 없음
                currentFireRate = 0; // 쿨타임 개념 없음
                currentReloadTime = 0; // 재장전 개념 없음
                if (vacuumCleanerPrefab != null) vacuumCleanerPrefab.SetActive(true); // 해당 총 모델 활성화
                newUIIcon = vacuumCleanerUIIcon;
                break;
            case WeaponType.SawtoothGun:
                currentMaxBulletCount = sawtoothGunMaxCount;
                currentFireRate = sawtoothGunFireRate;
                currentReloadTime = sawtoothGunReloadTime;
                if (sawtoothGunPrefab != null) sawtoothGunPrefab.SetActive(true); // 해당 총 모델 활성화
                newUIIcon = sawtoothGunUIIcon;
                break;
            case WeaponType.Katana:
                currentMaxBulletCount = 0;
                currentFireRate = katanaAttackCooldown;
                currentReloadTime = 0;
                if (katanaPrefab != null) katanaPrefab.SetActive(true); // 카타나 모델 활성화
                newUIIcon = katanaUIIcon;
                if (Anim != null)
                {
                    Anim.SetBool("IsKatanaEquipped", true);
                }
                break;
        }
        currentBulletCount = currentMaxBulletCount;
        isReloading = false;
        isAttacking = false;
        currentShootTimer = 0;

        // UI 아이콘 업데이트
        if (currentWeaponUIIcon != null)
        {
            currentWeaponUIIcon.sprite = newUIIcon;
            currentWeaponUIIcon.gameObject.SetActive(newUIIcon != null); // 아이콘이 없으면 비활성화
        }

        // VacuumObject는 VacuumCleaner가 아닐 때 비활성화
        if (vacuumObject != null)
        {
            vacuumObject.SetActive(currentWeaponType == WeaponType.VacuumCleaner);
        }
        if (m_ReloadImage != null)
        {
            m_ReloadImage.gameObject.SetActive(false);
        }
        if (katanaAttackCollider != null && currentWeaponType != WeaponType.Katana)
        {
            katanaAttackCollider.enabled = false;
        }
        UpdateBulletUI();
    }

    void Update()
    {
        if (isDead)
        {
            if (isOverlappingWithOther && otherPlayer != null && !otherPlayer.isDead)
            {
                if (reviveImage != null && !reviveImage.gameObject.activeSelf)
                {
                    reviveImage.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    reviveProgress += 1f;
                    if (reviveBar != null)
                        reviveBar.fillAmount = reviveProgress / reviveRequired;
                }
                if (reviveProgress >= reviveRequired)
                {
                    Revive();
                }
            }
            else // 다른 플레이어가 근처에 없거나, 다른 플레이어가 죽었을 때
            {
                if (reviveImage != null && reviveImage.gameObject.activeSelf)
                {
                    reviveImage.gameObject.SetActive(false);
                    reviveBar.fillAmount = 0f;
                }
            }

            // 죽은 상태에서는 무기 및 이동 관련 로직을 스킵
            return;
        }

        // isDead가 false일 때만 실행되는 부분
        if (currentWeaponType != WeaponType.Katana)
        {
            if (isReloading && m_ReloadImage != null)
            {
                reloadTimer += Time.deltaTime;
                m_ReloadImage.fillAmount = Mathf.Clamp01(reloadTimer / currentReloadTime);
                if (reloadTimer >= currentReloadTime)
                {
                    m_ReloadImage.fillAmount = 1f;
                }
            }
            else if (m_ReloadImage != null && !isReloading)
            {
                m_ReloadImage.fillAmount = 0f;
            }
        }
        else
        {
            if (m_ReloadImage != null)
            {
                m_ReloadImage.gameObject.SetActive(false);
            }
        }

        // VacuumObject 활성화/비활성화 및 위치/스케일 조정
        if (vacuumObject != null)
        {
            if (currentWeaponType == WeaponType.VacuumCleaner && !isDead)
            {
                bool isActive = Input.GetKey(shootKey);
                vacuumObject.SetActive(isActive);

                if (isActive)
                {
                    // VacuumObject의 스케일 (방향 뒤집기)
                    Vector3 scale = vacuumObject.transform.localScale;
                    scale.x = (SpriteRenderer != null && SpriteRenderer.flipX) ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                    vacuumObject.transform.localScale = scale;

                    // VacuumObject의 위치 (m_ShootPos에 따라) - m_ShootPos의 자식이라면 이 코드는 불필요함
                    // vacuumObject.transform.position = m_ShootPos.position;
                }
            }
            else
            {
                vacuumObject.SetActive(false);
            }
        }


        bool wasGrounded = isGrounded;
        bool grounded1 = Physics2D.Raycast(groundCheck1.position, Vector2.down, groundCheckDistance, groundLayer);
        bool grounded2 = Physics2D.Raycast(groundCheck2.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = grounded1 || grounded2;

        if (!wasGrounded && isGrounded)
        {
            isDoubleJumpAvailable = true;
        }

        Move();
        Animation();

        if (currentShootTimer > 0)
        {
            currentShootTimer -= Time.deltaTime;
        }

        if (weaponSwapTimer > 0f)
            weaponSwapTimer -= Time.deltaTime;

        HandleWeaponInput();
        HandleWeaponSwapInput(); // 무기 스왑 입력 처리 추가

        if (m_HpBar != null)
            m_HpBar.fillAmount = m_CurHp / m_MaxHp;

        UpdateBulletUI();
        TextHp.text = m_MaxHp.ToString("F0") + " / " + m_CurHp.ToString("F0");

        m_DamageCool -= Time.deltaTime;
        m_LavaCool -= Time.deltaTime;
    }

    void UpdateBulletUI()
    {
        if (BulletCount != null)
        {
            if (currentWeaponType == WeaponType.VacuumCleaner || currentWeaponType == WeaponType.Katana)
            {
                BulletCount.text = "∞";
            }
            else
            {
                BulletCount.text = currentBulletCount + " / " + currentMaxBulletCount;
            }
        }
    }

    void HandleWeaponInput()
    {
        if (isDead || isReloading) return;

        Vector2 shootDir = (SpriteRenderer != null && SpriteRenderer.flipX) ? Vector2.left : Vector2.right;

        switch (currentWeaponType)
        {
            case WeaponType.DefaultGun:
                if (Input.GetKey(shootKey) && currentShootTimer <= 0f && currentBulletCount > 0)
                {
                    FireDefaultGun(shootDir);
                }
                if (Input.GetKeyDown(reloadKey) || (currentBulletCount <= 0 && !isReloading))
                {
                    StartReload();
                }
                break;

            case WeaponType.RocketLauncher:
                if (Input.GetKeyDown(shootKey) && currentShootTimer <= 0f && currentBulletCount > 0)
                {
                    FireRocketLauncher(shootDir);
                }
                if (Input.GetKeyDown(reloadKey) || (currentBulletCount <= 0 && !isReloading))
                {
                    StartReload();
                }
                break;

            case WeaponType.VacuumCleaner:
                // 흡수 로직은 OnTriggerStay2D에서 직접 처리하므로 여기서는 Input.GetKey(shootKey)만 감지합니다.
                // Input.GetKey(shootKey)가 true일 때 vacuumObject가 활성화되므로 별도의 호출이 필요 없습니다.
                break;

            case WeaponType.SawtoothGun:
                if (Input.GetKey(shootKey) && currentShootTimer <= 0f && currentBulletCount > 0)
                {
                    FireSawtoothGun(shootDir);
                }
                if (Input.GetKeyDown(reloadKey) || (currentBulletCount <= 0 && !isReloading))
                {
                    StartReload();
                }
                break;
            case WeaponType.Katana:
                if (Input.GetKeyDown(shootKey) && currentShootTimer <= 0f && !isAttacking)
                {
                    AttackKatana();
                }
                break;
        }
    }

    void HandleWeaponSwapInput()
    {
        if (isDead) return;

        if (weaponSwapTimer > 0f) return;

        if (Input.GetKeyDown(swapWeaponKey) && swappableWeapons.Count > 1)
        {
            // 현재 무기 탄환 저장
            if (currentWeaponType != WeaponType.VacuumCleaner && currentWeaponType != WeaponType.Katana)
                weaponBulletDict[currentWeaponType] = currentBulletCount;

            currentWeaponIndex = (currentWeaponIndex + 1) % swappableWeapons.Count;
            ChangeWeapon(swappableWeapons[currentWeaponIndex]);

            // 스왑 쿨타임 시작
            weaponSwapTimer = weaponSwapCooldown;
        }
    }



    void FireDefaultGun(Vector2 direction)
    {
        if (defaultBulletPrefab != null && m_ShootPos != null)
        {
            GameObject bullet = Instantiate(defaultBulletPrefab, m_ShootPos.position, Quaternion.identity);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * defaultGunShootForce;
            }
            currentBulletCount--;
            currentShootTimer = defaultGunShootCool;

            if (currentBulletCount <= 0)
            {
                StartReload();
            }
        }
    }

    void FireRocketLauncher(Vector2 direction)
    {
        if (rocketPrefab != null && m_ShootPos != null)
        {
            GameObject rocket = Instantiate(rocketPrefab, m_ShootPos.position, Quaternion.identity);
            Rigidbody2D rocketRb = rocket.GetComponent<Rigidbody2D>();
            if (rocketRb != null)
            {
                rocketRb.linearVelocity = direction * rocketShootForce;
            }
            currentBulletCount--;
            currentShootTimer = rocketFireRate;

            if (currentBulletCount <= 0)
            {
                StartReload();
            }
        }
    }

    void FireSawtoothGun(Vector2 direction)
    {
        if (sawtoothBulletPrefab != null && m_ShootPos != null)
        {
            GameObject bladeBullet = Instantiate(sawtoothBulletPrefab, m_ShootPos.position, Quaternion.identity);
            Rigidbody2D bladeRb = bladeBullet.GetComponent<Rigidbody2D>();
            if (bladeRb != null)
            {
                bladeRb.linearVelocity = direction * sawtoothGunShootForce;
            }
            currentBulletCount--;
            currentShootTimer = sawtoothGunFireRate;

            if (currentBulletCount <= 0)
            {
                StartReload();
            }
        }
    }

    void AttackKatana()
    {
        currentShootTimer = katanaAttackCooldown;

        if (Anim != null)
        {
            Anim.SetTrigger("KatanaAttack");
        }

        StartCoroutine(KatanaAttackRoutine());
    }

    IEnumerator KatanaAttackRoutine()
    {
        isAttacking = true;
        if (katanaAttackCollider != null)
        {
            katanaAttackCollider.enabled = true;
        }

        yield return new WaitForSeconds(katanaColliderActiveDuration);

        if (katanaAttackCollider != null)
        {
            katanaAttackCollider.enabled = false;
        }
        isAttacking = false;
    }

    void StartReload()
    {
        if (currentWeaponType == WeaponType.VacuumCleaner || currentWeaponType == WeaponType.Katana) return;

        if (!isReloading && currentBulletCount < currentMaxBulletCount)
        {
            isReloading = true;
            reloadTimer = 0.0f;
            Invoke("ReloadComplete", currentReloadTime);
            if (m_ReloadImage != null)
            {
                m_ReloadImage.gameObject.SetActive(true);
            }
        }
    }

    void ReloadComplete()
    {
        currentBulletCount = currentMaxBulletCount;
        isReloading = false;
        if (m_ReloadImage != null)
        {
            m_ReloadImage.gameObject.SetActive(false);
            m_ReloadImage.fillAmount = 0f;
            reloadTimer = 0f;
        }
        UpdateBulletUI();
    }

    void Die()
    {
        isDead = true;
        m_CurHp = 0.0f;
        if (Anim != null)
            Anim.SetTrigger("Die");

        reviveProgress = 0f;
        if (reviveBar != null)
            reviveBar.fillAmount = 0f;

        TextHp.text = m_MaxHp.ToString("F0") + " / " + m_CurHp.ToString("F0");

        // --- 사망 시 물리적 처리 (낙하 및 충돌 레이어 변경) ---
        rb.linearVelocity = Vector2.zero; // 현재 선형 속도를 0으로 초기화
        rb.angularVelocity = 0f;          // 현재 각속도를 0으로 초기화
        rb.simulated = true; // Rigidbody2D 시뮬레이션 활성화 (Dynamic일 경우 기본적으로 true)

        // 죽었을 때 플레이어 레이어를 변경하여 몬스터와 충돌하지 않도록 함
        gameObject.layer = LayerMask.NameToLayer(playerDeadLayerName);

        // 메인 콜라이더는 활성 상태 유지 (바닥 충돌을 위해)
        if (mainPlayerCollider != null)
        {
            mainPlayerCollider.enabled = true;
        }

        // 부활 감지 트리거는 활성화 유지
        if (reviveDetectionTrigger != null)
        {
            reviveDetectionTrigger.enabled = true;
        }

        // --- 사망 시 모든 관련 이벤트 중단 ---
        CancelInvoke("ReloadComplete");
        isReloading = false;
        if (m_ReloadImage != null)
        {
            m_ReloadImage.gameObject.SetActive(false);
            m_ReloadImage.fillAmount = 0f;
            reloadTimer = 0f;
        }
        // 사망 시 모든 무기 오브젝트 비활성화
        if (defaultGunPrefab != null) defaultGunPrefab.SetActive(false);
        if (rocketLauncherPrefab != null) rocketLauncherPrefab.SetActive(false);
        if (vacuumCleanerPrefab != null) vacuumCleanerPrefab.SetActive(false);
        if (sawtoothGunPrefab != null) sawtoothGunPrefab.SetActive(false);
        if (katanaPrefab != null) katanaPrefab.SetActive(false);
        if (vacuumObject != null)
        {
            vacuumObject.SetActive(false);
        }
        isAttacking = false;
        if (katanaAttackCollider != null)
        {
            katanaAttackCollider.enabled = false;
        }

        // UI 아이콘도 비활성화 (선택 사항, 아이콘이 없으면 사라지게 할 경우)
        if (currentWeaponUIIcon != null)
        {
            currentWeaponUIIcon.gameObject.SetActive(false);
        }


        if (GameMgr.Inst != null)
            GameMgr.Inst.OnPlayerDead();

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkOnDeath());
    }

    void Revive()
    {
        isDead = false;
        m_CurHp = m_MaxHp * 0.5f;
        reviveProgress = 0f;

        if (reviveBar != null)
            reviveBar.fillAmount = 0f;
        if (reviveImage != null)
            reviveImage.gameObject.SetActive(false);

        // 부활 시 현재 무기 다시 활성화 (UI 포함)
        InitializeWeaponStats();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;

        // 부활 시 원래 플레이어 레이어로 되돌림
        gameObject.layer = LayerMask.NameToLayer(playerAliveLayerName);

        if (mainPlayerCollider != null)
        {
            mainPlayerCollider.enabled = true;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        SetSpriteAlpha(1f);

        // 플레이어가 부활했음을 알리는 이벤트 호출
        OnPlayerRevived?.Invoke(this.transform, this); // Transform과 Player 컴포넌트 모두 전달

        if (GameMgr.Inst != null)
            GameMgr.Inst.OnPlayerRevive();
    }
    void SetSpriteAlpha(float alpha)
    {
        if (SpriteRenderer != null)
        {
            Color c = SpriteRenderer.color;
            c.a = alpha;
            SpriteRenderer.color = c;
        }
    }

    IEnumerator BlinkOnDeath()
    {
        while (isDead)
        {
            SetSpriteAlpha(0.3f);
            yield return new WaitForSeconds(0.15f);
            SetSpriteAlpha(1f);
            yield return new WaitForSeconds(0.15f);
        }
    }

    void Move()
    {
        if (isDead || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        h = 0.0f;
        if (Input.GetKey(leftKey)) h = -1.0f;
        if (Input.GetKey(rightKey)) h = 1.0f;
        rb.linearVelocity = new Vector2(h * m_MoveSpeed, rb.linearVelocity.y);

        if (Input.GetKeyDown(jumpKey))
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, m_JumpForce);
                isDoubleJumpAvailable = true;
            }
            else if (isDoubleJumpAvailable)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, m_JumpForce);
                isDoubleJumpAvailable = false;
            }
        }
    }

    void Animation()
    {
        if (isDead || isAttacking)
        {
            Anim.SetFloat("Speed", 0);
            Anim.SetBool("speed", true);
            return;
        }

        Anim.SetFloat("Speed", Mathf.Abs(h));
        bool t = h == 0.0f;
        Anim.SetBool("speed", t);

        if (Anim != null)
        {
            Anim.SetBool("IsKatanaEquipped", currentWeaponType == WeaponType.Katana);
        }

        if (h != 0.0f)
        {
            SpriteRenderer.flipX = h < 0.0f;

            // m_ShootPos의 로컬 위치 조정 (총구 위치)
            Vector3 shootPos = m_ShootPos.localPosition;
            shootPos.x = h > 0f ? Mathf.Abs(shootPos.x) : -Mathf.Abs(shootPos.x);
            m_ShootPos.localPosition = shootPos;

            // 현재 활성화된 총 모델의 스케일과 로컬 위치 조정 (추가된 부분)
            GameObject activeGunObject = null;
            switch (currentWeaponType)
            {
                case WeaponType.DefaultGun: activeGunObject = defaultGunPrefab; break;
                case WeaponType.RocketLauncher: activeGunObject = rocketLauncherPrefab; break;
                case WeaponType.VacuumCleaner: activeGunObject = vacuumCleanerPrefab; break;
                case WeaponType.Katana: activeGunObject = katanaPrefab; break;
                case WeaponType.SawtoothGun: activeGunObject = sawtoothGunPrefab; break;
            }

            if (activeGunObject != null)
            {
                Vector3 gunScale = activeGunObject.transform.localScale;
                gunScale.x = h > 0f ? Mathf.Abs(gunScale.x) : -Mathf.Abs(gunScale.x);
                activeGunObject.transform.localScale = gunScale;

                Vector3 gunPosition = activeGunObject.transform.localPosition;
                gunPosition.x = h > 0f ? Mathf.Abs(gunPosition.x) : -Mathf.Abs(gunPosition.x);
                activeGunObject.transform.localPosition = gunPosition;
            }

            // VacuumObject의 방향도 플레이어 방향에 따라 뒤집기 (VacuumCleaner 무기일 경우에만)
            if (vacuumObject != null && currentWeaponType == WeaponType.VacuumCleaner)
            {
                Vector3 vacuumScale = vacuumObject.transform.localScale;
                vacuumScale.x = h > 0f ? Mathf.Abs(vacuumScale.x) : -Mathf.Abs(vacuumScale.x);
                vacuumObject.transform.localScale = vacuumScale;
            }
        }
    }

    public void TakeDamage(float a_Value)
    {
        if (m_CurHp <= 0.0f || m_DamageCool > 0f)
            return;

        m_CurHp -= a_Value;
        if (m_CurHp < 0.0f)
            m_CurHp = 0.0f;

        ApplyKnockback();

        m_DamageCool = 0.5f; // 0.5초 쿨타임 설정

        if (m_CurHp <= 0.0f)
        {
            Die();
        }
    }

    void ApplyKnockback()
    {
        if (rb == null) return;

        float dir = (SpriteRenderer != null && SpriteRenderer.flipX) ? 1f : -1f;
        Vector2 knockback = new Vector2(dir * knockbackForce, knockbackUpForce);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    public void ChangeWeapon(WeaponType newWeapon)
    {
        currentWeaponType = newWeapon;
        CancelInvoke("ReloadComplete");
        InitializeWeaponStats();

        // 탄환 복원 (총알 개념이 있는 무기만)
        if (currentWeaponType != WeaponType.VacuumCleaner && currentWeaponType != WeaponType.Katana)
        {
            if (weaponBulletDict.TryGetValue(currentWeaponType, out int savedBullet))
            {
                currentBulletCount = Mathf.Clamp(savedBullet, 0, currentMaxBulletCount);
            }
        }
        UpdateBulletUI();
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.CompareTag("EnemyBullet"))
        {
            TakeDamage(10);
            Destroy(coll.gameObject);
        }
        else if (coll.CompareTag("Fire"))
        {
            TakeDamage(30);
        }
        else if (coll == otherPlayer?.reviveDetectionTrigger)
        {
            if (isDead)
            {
                isOverlappingWithOther = true;
                if (reviveImage != null)
                {
                    reviveImage.gameObject.SetActive(true);
                    reviveBar.fillAmount = reviveProgress / reviveRequired;
                }
            }
        }
        else if (coll.CompareTag("JumpBoost"))
        {
            m_JumpForce += 5.0f;
        }
        else if (currentWeaponType == WeaponType.Katana && isAttacking)
        {
            if (coll.CompareTag("BlockVine"))
            {
                Destroy(coll.gameObject, 0.2f);
            }
        }
        else if(coll.CompareTag("Thorn"))
        {
            TakeDamage(30);
        }
    }

    private void OnTriggerExit2D(Collider2D coll)
    {
        if (coll == otherPlayer?.reviveDetectionTrigger)
        {
            isOverlappingWithOther = false;
            reviveProgress = 0f;
            if (reviveBar != null)
                reviveBar.fillAmount = 0f;
            if (reviveImage != null)
                reviveImage.gameObject.SetActive(false);
        }
        else if (coll.CompareTag("JumpBoost"))
        {
            m_JumpForce -= 5.0f;
        }
    }

    private void OnTriggerStay2D(Collider2D coll)
    {
        if (m_LavaCool < 0 && coll.CompareTag("Lava"))
        {
            TakeDamage(10);
            m_LavaCool = 0.25f;
        }
        else if (coll.CompareTag("MiddleBoss"))
        {
            TakeDamage(50);
            isDoubleJumpAvailable = true;
        }
        // Vacuum Cleaner 흡수 로직 (Player 스크립트에서 직접 처리)
        // Vacuum Cleaner 흡수 로직
        else if (currentWeaponType == WeaponType.VacuumCleaner && vacuumObject != null && vacuumObject.activeSelf)
        {
            if (((1 << coll.gameObject.layer) & smallMonsterLayer) != 0)
            {
                if (coll.CompareTag("SmallMonster")||coll.CompareTag("HealSlime"))
                {
                    Rigidbody2D monsterRb = coll.GetComponent<Rigidbody2D>();
                    if (monsterRb != null)
                    {
                        // 플레이어의 ShootPos 방향으로 끌어당김
                        Vector2 directionToPlayer = (m_ShootPos.position - coll.transform.position).normalized;
                        // 적용될 힘의 크기 계산
                        Vector2 forceToApply = directionToPlayer * suckForce * Time.deltaTime;

                        monsterRb.AddForce(forceToApply, ForceMode2D.Force);

                        // 특정 거리 안에 들어오면 소멸
                        if (Vector2.Distance(m_ShootPos.position, coll.transform.position) < consumeDistance)
                        {
                            Destroy(coll.gameObject);
                        }
                    }
                }
            }
        }
    }

}