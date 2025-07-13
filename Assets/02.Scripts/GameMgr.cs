using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMgr : MonoBehaviour
{
    [SerializeField] private GameObject gateObject;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gamePausePanel;

    // --- 팁/가이드 패널 통합 ---
    public GameObject tipPanel;
    public GameObject[] guidePanels;

    private bool isTipActive = false;
    private bool isGuideActive = false;
    private int currentGuideIndex = 0;
    private static bool isFirstLoad = true;

    //--- 싱글턴 패턴
    public static GameMgr Inst = null;

    private int playerInGateCount = 0;
    private int totalPlayers = 2;
    private int deadPlayerCount = 0;

    public int currentStage = 1;

    public Button restartButton;
    public Button exitButton;

    // --- 볼륨 슬라이더 관련 ---
    [Header("Audio Control")]
    public Slider bgmSlider;
    public Slider soundSlider;
    public AudioSource bgmSource;    // BGMPlayer의 AudioSource
    public AudioSource soundSource;  // SoundPlayer의 AudioSource

    private void Awake()
    {
        Inst = this;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);

        // 볼륨 슬라이더 초기화 및 이벤트 연결
        if (bgmSlider != null && bgmSource != null)
        {
            bgmSlider.value = bgmSource.volume;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (soundSlider != null && soundSource != null)
        {
            soundSlider.value = soundSource.volume;
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        if (isFirstLoad)
        {
            if (tipPanel != null)
            {
                tipPanel.SetActive(true);
                Time.timeScale = 0.0f;
                isTipActive = true;
            }
            else if (guidePanels != null && guidePanels.Length > 0)
            {
                ShowGuidePanels();
            }
            isFirstLoad = false;
        }
    }

    void Update()
    {
        // --- 팁 패널: 어떤 키든 입력 시 닫힘 ---
        if (isTipActive && tipPanel != null && tipPanel.activeSelf)
        {
            if (Input.anyKeyDown)
            {
                tipPanel.SetActive(false);
                isTipActive = false;
                ShowGuidePanels();
            }
            return;
        }

        // --- 가이드 패널: 스페이스바로만 넘김 ---
        if (isGuideActive && guidePanels != null && guidePanels.Length > 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                guidePanels[currentGuideIndex].SetActive(false);
                currentGuideIndex++;
                if (currentGuideIndex < guidePanels.Length)
                {
                    guidePanels[currentGuideIndex].SetActive(true);
                }
                else
                {
                    isGuideActive = false;
                    Time.timeScale = 1.0f;
                }
            }
            return;
        }

        // "Enemy" 태그를 가진 오브젝트가 없으면 게이트 오브젝트 활성화
        if (gateObject != null && GameObject.FindGameObjectsWithTag("Enemy").Length == 0 &&
            GameObject.FindGameObjectsWithTag("SmallMonster").Length == 0 &&
            GameObject.FindGameObjectsWithTag("MiddleBoss").Length == 0)
        {
            if (!gateObject.activeSelf)
                gateObject.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gamePausePanel != null)
            {
                gamePausePanel.SetActive(!gamePausePanel.activeSelf);
                Time.timeScale = gamePausePanel.activeSelf ? 0.0f : 1.0f;
            }
        }
    }

    // --- 볼륨 조절 함수 ---
    public void SetBGMVolume(float value)
    {
        if (bgmSource != null)
            bgmSource.volume = value;
    }
    public void SetSoundVolume(float value)
    {
        if (soundSource != null)
            soundSource.volume = value;
    }

    private void ShowGuidePanels()
    {
        if (guidePanels != null && guidePanels.Length > 0)
        {
            currentGuideIndex = 0;
            isGuideActive = true;
            Time.timeScale = 0.0f;
            for (int i = 0; i < guidePanels.Length; i++)
                guidePanels[i].SetActive(false);
            guidePanels[0].SetActive(true);
        }
        else
        {
            Time.timeScale = 1.0f;
        }
    }

    public void OnPlayerEnterGate(GameObject player)
    {
        if (player != null)
        {
            player.SetActive(false);
            playerInGateCount++;

            if (playerInGateCount >= totalPlayers && currentStage == 1)
            {
                SceneManager.LoadScene("Stage_2");
                currentStage = 2;
            }
            else if (playerInGateCount >= totalPlayers && currentStage == 2)
            {
                SceneManager.LoadScene("Stage_3");
                currentStage = 3;
            }
            else if (playerInGateCount >= totalPlayers && currentStage == 3)
            {
                SceneManager.LoadScene("Stage_4");
                currentStage = 4;
            }
            else if (playerInGateCount >= totalPlayers && currentStage == 4)
            {
                SceneManager.LoadScene("TitleScene");
            }
        }
    }

    public void OnPlayerDead()
    {
        deadPlayerCount++;
        if (deadPlayerCount >= 2)
        {
            GameOver();
        }
    }

    public void OnPlayerRevive()
    {
        if (deadPlayerCount > 0)
            deadPlayerCount--;
    }
    private void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0.0f;
        }
    }

    public void OnClickRestart()
    {
        SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickExit()
    {
        SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("TitleScene");
    }

    public void OnStart()
    {
        if (gamePausePanel != null)
            gamePausePanel.SetActive(false);
        Time.timeScale = 1.0f;
    }
}