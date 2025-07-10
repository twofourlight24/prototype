using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMgr : MonoBehaviour
{
    [SerializeField] private GameObject gateObject;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gamePausePanel;

    // --- 스테이지별 팁 패널 ---
    public GameObject stage1TipPanel;
    public GameObject stage2TipPanel;
    public GameObject stage3TipPanel;
    public GameObject stage4TipPanel;

    private bool isStage1TipActive = false;
    private bool isStage2TipActive = false;
    private bool isStage3TipActive = false;
    private bool isStage4TipActive = false;

    private static bool isStage1FirstLoad = true;
    private static bool isStage2FirstLoad = true;
    private static bool isStage3FirstLoad = true;
    private static bool isStage4FirstLoad = true;

    //--- 싱글턴 패턴
    public static GameMgr Inst = null;

    private int playerInGateCount = 0;
    private int totalPlayers = 2; // 플레이어 수
    private int deadPlayerCount = 0;

    public int currentStage = 1;

    public Button restartButton;
    public Button exitButton;
    private void Awake()
    {
        Inst = this;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); // 시작 시 비활성화
    }
    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);

        // 스테이지별 팁 패널 최초 진입 시만 보여주기
        if (currentStage == 1 && isStage1FirstLoad && stage1TipPanel != null)
        {
            stage1TipPanel.SetActive(true);
            Time.timeScale = 0.0f;
            isStage1TipActive = true;
            isStage1FirstLoad = false;
        }
        else if (currentStage == 2 && isStage2FirstLoad && stage2TipPanel != null)
        {
            stage2TipPanel.SetActive(true);
            Time.timeScale = 0.0f;
            isStage2TipActive = true;
            isStage2FirstLoad = false;
        }
        else if (currentStage == 3 && isStage3FirstLoad && stage3TipPanel != null)
        {
            stage3TipPanel.SetActive(true);
            Time.timeScale = 0.0f;
            isStage3TipActive = true;
            isStage3FirstLoad = false;
        }
        else if (currentStage == 4 && isStage4FirstLoad && stage4TipPanel != null)
        {
            stage4TipPanel.SetActive(true);
            Time.timeScale = 0.0f;
            isStage4TipActive = true;
            isStage4FirstLoad = false;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);
    }

    void Update()
    {
        // 팁 패널이 열려있으면 Space로 닫기
        if (isStage1TipActive && stage1TipPanel != null && stage1TipPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                stage1TipPanel.SetActive(false);
                Time.timeScale = 1.0f;
                isStage1TipActive = false;
            }
            return;
        }
        if (isStage2TipActive && stage2TipPanel != null && stage2TipPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                stage2TipPanel.SetActive(false);
                Time.timeScale = 1.0f;
                isStage2TipActive = false;
            }
            return;
        }
        if (isStage3TipActive && stage3TipPanel != null && stage3TipPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                stage3TipPanel.SetActive(false);
                Time.timeScale = 1.0f;
                isStage3TipActive = false;
            }
            return;
        }
        if (isStage4TipActive && stage4TipPanel != null && stage4TipPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                stage4TipPanel.SetActive(false);
                Time.timeScale = 1.0f;
                isStage4TipActive = false;
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
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (gamePausePanel != null)
            {
                gamePausePanel.SetActive(!gamePausePanel.activeSelf);
                Time.timeScale = gamePausePanel.activeSelf ? 0.0f : 1.0f; 
            }
        }
    }

    // 게이트에 플레이어가 닿았을 때 호출
    public void OnPlayerEnterGate(GameObject player)
    {
        if (player != null)
        {
            player.SetActive(false); // 플레이어 비활성화(사라짐)
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

    // 플레이어가 죽었을 때 호출
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
            gameOverPanel.SetActive(true); // GameOver 패널 활성화
            Time.timeScale = 0.0f; // 게임 일시정지
        }
    }

    // RE 버튼 (Restart)
    public void OnClickRestart()
    {
        SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Exit 버튼 (TitleScene으로)
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
        Time.timeScale = 1.0f; // 게임 시작 시 시간 흐름 재개
    }

}

