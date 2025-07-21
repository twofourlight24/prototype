using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryMgr : MonoBehaviour
{
    public TextMeshProUGUI fatherText;
    public TextMeshProUGUI sonText;
    public TextMeshProUGUI endingText;
    public float typingSpeed = 0.1f;
    public Image FadeImage; // 검은색 이미지(알파 1)로 캔버스에 추가 필요
    public Button skipButton; // 스킵 버튼 추가 필요   
    public Image cutsceneImage; // 컷씬 이미지를 보여줄 UI Image
    public Sprite[] cutsceneSprites; // 컷씬별 스프라이트 배열
    // 대사 인덱스별로 컷씬 인덱스를 지정
    public int[] cutsceneChangeIndices; // 예: [0, 3, 6]
    private int currentCutscene = 0;

    // 화자와 대사를 함께 저장
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker; // "father" or "son"
        public string text;
    }

    public DialogueLine[] lines;

    private int index = 0;
    private bool isTyping = false;
    private bool isLineFullyShown = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        StartCoroutine(FadeIn());

        // Ending씬에서는 skipButton 비활성화 및 리스너 연결 X
        if (skipButton != null)
        {
            if (SceneManager.GetActiveScene().name == "Ending")
            {
                skipButton.gameObject.SetActive(false);
            }
            else
            {
                skipButton.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene("Stage_Menu");
                });
            }
        }

        if (cutsceneSprites.Length > 0 && cutsceneImage != null)
            cutsceneImage.sprite = cutsceneSprites[0];
    }

    IEnumerator FadeIn()
    {
        float duration = 1.0f;
        float elapsed = 0f;
        Color color = FadeImage.color;
        color.a = 1f;
        FadeImage.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            FadeImage.color = color;
            yield return null;
        }
        color.a = 0f;
        FadeImage.color = color;

        StartTypingLine();
    }



    void Update()
    {
        if (Input.anyKeyDown)
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                ShowFullLine();
            }
            else if (isLineFullyShown)
            {
                NextLine();
            }
        }
    }

    void StartTypingLine()
    {
        typingCoroutine = StartCoroutine(TypeLine(lines[index]));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        isLineFullyShown = false;

        // 텍스트 비우기
        fatherText.text = "";
        sonText.text = "";
        endingText.text = "";

        TextMeshProUGUI targetText = GetTargetText(line.speaker);

        foreach (char c in line.text)
        {
            targetText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        isLineFullyShown = true;
    }

    void ShowFullLine()
    {
        // 즉시 전체 문장 보여주기
        DialogueLine line = lines[index];
        GetTargetText(line.speaker).text = line.text;

        isTyping = false;
        isLineFullyShown = true;
    }

    void NextLine()
    {
        index++;
        if (currentCutscene + 1 < cutsceneChangeIndices.Length && index >= cutsceneChangeIndices[currentCutscene + 1])
        {
            currentCutscene++;
            if (currentCutscene < cutsceneSprites.Length)
                cutsceneImage.sprite = cutsceneSprites[currentCutscene];
        }

        if (index < lines.Length)
        {
            StartTypingLine();
        }
        else
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        float duration = 1.0f;
        float elapsed = 0f;
        Color color = FadeImage.color;
        color.a = 0f;
        FadeImage.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            FadeImage.color = color;
            yield return null;
        }

        // 엔딩씬이면 TitleScene, 아니면 Stage_1로
        if (SceneManager.GetActiveScene().name == "Ending")
            SceneManager.LoadScene("TitleScene");
        else
            SceneManager.LoadScene("Stage_Menu");
    }

    TextMeshProUGUI GetTargetText(string speaker)
    {
        if (speaker.ToLower() == "father")
            return fatherText;
        else if (speaker.ToLower() == "son")
            return sonText;
        else if (speaker.ToLower() == "ending")
            return endingText;
        else
            return fatherText; // 기본값
    }
}
