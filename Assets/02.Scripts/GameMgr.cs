using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMgr : MonoBehaviour
{
    [SerializeField] private GameObject gateObject;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gamePausePanel;

    // --- 스테이지별 팁/가이드 패널 ---
    public GameObject stage1TipPanel;
    public GameObject[] stage1GuidePanels;
    public GameObject stage2TipPanel;
    public GameObject[] stage2GuidePanels;
    public GameObject stage3TipPanel;
    public GameObject[] stage3GuidePanels;
    public GameObject stage4TipPanel;
    public GameObject[] stage4GuidePanels;

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

    [Header("Audio Control")]
    public Slider bgmSlider;
    public Slider soundSlider;
    public AudioSource bgmSource;
    public AudioSource soundSource;

    private GameObject currentTipPanel;
    private GameObject[] currentGuidePanels;

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

        // 스테이지별 패널 할당
        switch (currentStage)
        {
            case 1:
                currentTipPanel = stage1TipPanel;
                currentGuidePanels = stage1GuidePanels;
                break;
            case 2:
                currentTipPanel = stage2TipPanel;
                currentGuidePanels = stage2GuidePanels;
                break;
            case 3:
                currentTipPanel = stage3TipPanel;
                currentGuidePanels = stage3GuidePanels;
                break;
            case 4:
                currentTipPanel = stage4TipPanel;
                currentGuidePanels = stage4GuidePanels;
                break;
        }

        if (isFirstLoad)
        {
            if (currentTipPanel != null)
            {
                currentTipPanel.SetActive(true);
                Time.timeScale = 0.0f;
                isTipActive = true;
            }
            else if (currentGuidePanels != null && currentGuidePanels.Length > 0)
            {
                ShowGuidePanels();
            }
            isFirstLoad = false;
        }
    }

    void Update()
    {
        // 팁 패널: 어떤 키든 입력 시 닫힘
        if (isTipActive && currentTipPanel != null && currentTipPanel.activeSelf)
        {
            if (Input.anyKeyDown)
            {
                currentTipPanel.SetActive(false);
                isTipActive = false;
                ShowGuidePanels();
            }
            return;
        }

        // 가이드 패널: 스페이스바로만 넘김
        if (isGuideActive && currentGuidePanels != null && currentGuidePanels.Length > 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                currentGuidePanels[currentGuideIndex].SetActive(false);
                currentGuideIndex++;
                if (currentGuideIndex < currentGuidePanels.Length)
                {
                    currentGuidePanels[currentGuideIndex].SetActive(true);
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

    // 볼륨 조절 함수
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
        if (currentGuidePanels != null && currentGuidePanels.Length > 0)
        {
            currentGuideIndex = 0;
            isGuideActive = true;
            Time.timeScale = 0.0f;
            for (int i = 0; i < currentGuidePanels.Length; i++)
                currentGuidePanels[i].SetActive(false);
            currentGuidePanels[0].SetActive(true);
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

            if (playerInGateCount >= totalPlayers)
            {
                PlayerPrefs.SetInt("LastClearedStageIndex", currentStage);
                PlayerPrefs.Save(); // 변경사항을 즉시 저장합니다.

                // 챕터 선택씬으로 이동
                SceneManager.LoadScene("Stage_Menu");
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