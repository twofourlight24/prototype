using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TitleMgr : MonoBehaviour
{
    public Button StartBtn;
    public Button GuideBtn;
    public Button ExitBtn;
    public Button GuideBtn2;
    public GameObject GuidePanel;
    public Image FadeImage; // 검은색 이미지(알파 0)로 캔버스에 추가 필요

    private void Start()
    {
        StartBtn.onClick.AddListener(OnClickStartBtn);
        GuideBtn.onClick.AddListener(OnClickGuideBtn);
        ExitBtn.onClick.AddListener(OnClickExitBtn);
        GuideBtn2.onClick.AddListener(OnClickGuidBtn2);
    }

    private void OnClickExitBtn()
    {
        SoundManager.Instance.PlayButtonClick();
        Application.Quit();
    }

    private void OnClickGuideBtn()
    {
        SoundManager.Instance.PlayButtonClick();
        GuidePanel.SetActive(true);
    }

    void OnClickGuidBtn2()
    {
        SoundManager.Instance.PlayButtonClick();
        GuidePanel.SetActive(false);
    }

    private void OnClickStartBtn()
    {
        SoundManager.Instance.PlayButtonClick();
        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        float duration = 1.0f;
        float elapsed = 0f;
        Color color = FadeImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            FadeImage.color = color;
            yield return null;
        }
        SceneManager.LoadScene("Story_1");
    }

    private void Update()
    {

    }
}
