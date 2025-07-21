using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UI 관련 컴포넌트를 사용하기 위해 추가

public class MiddleBossCtrl : MonoBehaviour
{
    GameObject player1;
    GameObject player2;
    public float speed = 1.0f;
    public float distanceItv = 10.0f;

    public float m_CurBossHp = 200.0f;
    public float m_MaxBossHp = 200.0f;
    public Image m_HpBar;

    public GameObject m_tentacle;
    public GameObject m_boom;

    public GameObject[] m_tentacleSpawnPoint;

    public float tentacleSpawnInterval = 2.0f;
    private float tentacleTimer = 0f;

    public Image tentacleWarningPrefab;
    public Canvas uiCanvas; // 씬에 있는 UI Canvas 참조를 받아올 변수

    public float boomSpawnInterval = 3.0f;
    private float boomTimer = 0f;



    // 씬 최초 진입 체크용(정적 변수)
    private static bool isFirstLoad = true;

    void Start()
    {
        player1 = GameObject.Find("Player1");
        player2 = GameObject.Find("Player2");

        if (uiCanvas == null)
        {
            uiCanvas = FindAnyObjectByType<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogError("UI Canvas를 찾을 수 없습니다. tentacleWarningPrefab이 올바르게 표시되지 않을 수 있습니다.");
            }
        }


    }

    void Update()
    {

        if (m_HpBar != null)
        {
            m_HpBar.fillAmount = m_CurBossHp / m_MaxBossHp;
        }

        float a_FollowHeight = player1.transform.position.y - distanceItv;
        if (player1.transform.position.y < player2.transform.position.y)
        {
            a_FollowHeight = player1.transform.position.y - distanceItv;
        }
        else
            a_FollowHeight = player2.transform.position.y - distanceItv;

        if (transform.position.y < a_FollowHeight)
            transform.position = new Vector3(0.0f, a_FollowHeight, 0.0f);

        transform.Translate(new Vector3(0.0f, speed * Time.deltaTime, 0.0f));

        tentacleTimer += Time.deltaTime;
        if (tentacleTimer > tentacleSpawnInterval)
        {
            tentacleTimer = 0f;
            int idx = Random.Range(0, m_tentacleSpawnPoint.Length);
            StartCoroutine(ShowTentacleWarningAndSpawn(m_tentacleSpawnPoint[idx].transform.position));
        }
        boomTimer += Time.deltaTime;
        if (boomTimer > boomSpawnInterval)
        {
            boomTimer = 0f;
            float x = Random.Range(-6f, 6f);
            Vector3 spawnPos = new Vector3(x, transform.position.y, 0f);
            GameObject boom = Instantiate(m_boom, spawnPos, Quaternion.identity);
            boom.AddComponent<BossBoomCtrl>(); // 아래 스크립트 참고
        }
    }

    IEnumerator ShowTentacleWarningAndSpawn(Vector3 spawnPos)
    {
        if (uiCanvas == null)
        {
            Debug.LogError("UI Canvas가 없어서 경고 이미지를 생성할 수 없습니다.");
            yield break;
        }

        Image warning = Instantiate(tentacleWarningPrefab, uiCanvas.transform); // Canvas의 transform을 부모로 지정
        Vector3 screenPos = Camera.main.WorldToScreenPoint(spawnPos);
        warning.transform.position = screenPos;
        warning.gameObject.SetActive(true);

        float waitTime = 1.0f;
        float alpha = -6.0f;
        Color color = warning.color;

        while (waitTime > 0f)
        {
            waitTime -= Time.deltaTime;
            if (color.a >= 1.0f)
                alpha = -6.0f;
            else if (color.a <= 0.0f)
                alpha = 6.0f;
            color.a = Mathf.Clamp01(color.a + alpha * Time.deltaTime);
            warning.color = color;
            yield return null;
        }

        warning.gameObject.SetActive(false);
        Destroy(warning.gameObject);

        GameObject tentacle = Instantiate(m_tentacle, spawnPos, Quaternion.Euler(0, 0, 90));
        StartCoroutine(TentacleScaleRoutine(tentacle));
    }

    IEnumerator TentacleScaleRoutine(GameObject tentacle)
    {
        float scaleTime = 0.3f;
        float shrinkTime = 0.3f;
        float timer = 0f;

        tentacle.transform.localScale = new Vector3(0.1f, tentacle.transform.localScale.y, tentacle.transform.localScale.z);

        while (timer < scaleTime)
        {
            float t = timer / scaleTime;
            float scaleX = Mathf.Lerp(0.1f, 1f, t);
            tentacle.transform.localScale = new Vector3(scaleX, tentacle.transform.localScale.y, tentacle.transform.localScale.z);
            timer += Time.deltaTime;
            yield return null;
        }
        tentacle.transform.localScale = new Vector3(1f, tentacle.transform.localScale.y, tentacle.transform.localScale.z);

        yield return new WaitForSeconds(0.3f);

        timer = 0f;
        while (timer < shrinkTime)
        {
            float t = timer / shrinkTime;
            float scaleX = Mathf.Lerp(1f, 0.1f, t);
            tentacle.transform.localScale = new Vector3(scaleX, tentacle.transform.localScale.y, tentacle.transform.localScale.z);
            timer += Time.deltaTime;
            yield return null;
        }
        tentacle.transform.localScale = new Vector3(0.1f, tentacle.transform.localScale.y, tentacle.transform.localScale.z);

        Destroy(tentacle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Stone"))
        {
            m_CurBossHp -= 10.0f;
            if (m_CurBossHp <= 0.0f)
            {
                Clear();
            }
        }
    }
    void Clear()
    {
        PlayerPrefs.SetInt("LastClearedStageIndex", 3);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Stage_Menu");
    }
}