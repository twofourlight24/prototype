using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ChapterManager : MonoBehaviour
{
    [System.Serializable]
    public class StageSlot
    {
        public GameObject slotObject;
        public Image lockImage;
        public string sceneName;
        public Image selectImage; // 선택 표시용 오브젝트(Inspector에서 할당)
    }
    public static ChapterManager Inst { get; private set; } // 싱글톤

    public StageSlot[] stages;
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;

    // ESC 메뉴 관련
    public GameObject escMenuPanel;
    public Button exitButton;
    [Header("Audio Control")]
    public Slider bgmSlider;
    public Slider soundSlider;
    public AudioSource bgmSource;
    public AudioSource soundSource;

    private int selectedIndex = 0;
    private bool isConfirming = false;
    private bool[] unlockedStages;

    void Awake()
    {
        if (Inst == null)
        {
            Inst = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 챕터 선택 씬이 아니면 자동 파괴
        if (SceneManager.GetActiveScene().name != "Stage_Menu")
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        unlockedStages = new bool[stages.Length];
        for (int i = 0; i < stages.Length; i++)
        {
            unlockedStages[i] = IsStageCleared(i);
        }
        UpdateStageLocks();
        UpdateSelectionUI();
        if (confirmPanel != null) confirmPanel.SetActive(false);

        // ESC 메뉴 초기화
        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);

        // 볼륨 슬라이더 초기화
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

        if (PlayerPrefs.HasKey("LastClearedStageIndex"))
        {
            int clearedStageNumber = PlayerPrefs.GetInt("LastClearedStageIndex"); // 1부터 시작하는 스테이지 번호
            PlayerPrefs.DeleteKey("LastClearedStageIndex"); // 사용 후 즉시 삭제하여 중복 실행 방지
            UnlockNextStage(clearedStageNumber - 1);

        }
    }

    void Update()
    {
        // ESC 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (escMenuPanel != null)
            {
                escMenuPanel.SetActive(!escMenuPanel.activeSelf);
                Time.timeScale = escMenuPanel.activeSelf ? 0.0f : 1.0f;
                SoundManager.Instance.PlayButtonClick();
            }
        }

        if (isConfirming)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                SoundManager.Instance.PlayButtonClick();
                SceneManager.LoadScene(stages[selectedIndex].sceneName);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                SoundManager.Instance.PlayButtonClick();
                isConfirming = false;
                if (confirmPanel != null) confirmPanel.SetActive(false);
            }
            return;
        }

        if (escMenuPanel != null && escMenuPanel.activeSelf)
            return; // ESC 메뉴가 열려있으면 챕터 선택 입력 무시

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex--;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, stages.Length - 1);
            UpdateSelectionUI();
            SoundManager.Instance.PlayButtonClick();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int nextIndex = selectedIndex + 1;
            if (nextIndex < stages.Length && unlockedStages[nextIndex])
            {
                selectedIndex = nextIndex;
                UpdateSelectionUI();
                SoundManager.Instance.PlayButtonClick();
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.deniedClip);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (unlockedStages[selectedIndex])
            {
                SoundManager.Instance.PlayButtonClick();
                isConfirming = true;
                if (confirmPanel != null)
                {
                    confirmPanel.SetActive(true);
                    int selectedIndexname = selectedIndex+1;
                    if (confirmText != null)
                        confirmText.text = "Chapter " + selectedIndexname + " 스테이지로 이동할까요?\n[Enter/Space: 확인 / ESC: 취소]";
                }
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.deniedClip);
            }
        }
    }

    public void OnStart()
    {
        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
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

    public void OnClickExit()
    {
        SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("TitleScene");
    }

    public void UnlockNextStage(int clearedStageIdx)
    {
        int nextIdx = clearedStageIdx + 1;
        if (nextIdx < stages.Length && !unlockedStages[nextIdx])
        {
            unlockedStages[nextIdx] = true;
            PlayerPrefs.SetInt($"Stage{nextIdx}_Clear", 1);
            StartCoroutine(FadeOutLockImage(stages[nextIdx].lockImage));
            SoundManager.Instance.PlaySFX(SoundManager.Instance.unlockClip);
            UpdateStageLocks(); // 락 UI 즉시 갱신
        }
    }

    IEnumerator FadeOutLockImage(Image lockImg)
    {
        if (lockImg == null) yield break;
        float t = 0f;
        Color c = lockImg.color;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            c.a = Mathf.Lerp(1f, 0f, t);
            lockImg.color = c;
            yield return null;
        }
        lockImg.gameObject.SetActive(false);
    }

    void UpdateStageLocks()
    {
        for (int i = 0; i < stages.Length; i++)
        {
            bool unlocked = unlockedStages[i];
            if (stages[i].lockImage != null)
                stages[i].lockImage.gameObject.SetActive(!unlocked);
        }
    }

    void UpdateSelectionUI()
    {
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].selectImage != null)
                stages[i].selectImage.gameObject.SetActive(i == selectedIndex);
        }
    }

    private bool IsStageCleared(int idx)
    {
        if (idx == 0) return true;
        return PlayerPrefs.GetInt($"Stage{idx}_Clear", 0) == 1;
    }
}