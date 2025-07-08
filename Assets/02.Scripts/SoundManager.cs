using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public AudioSource sfxSource;
    public AudioClip buttonClickClip;
    public AudioClip fireTrapClip;
    public AudioClip slimeJumpClip;
    // 필요한 효과음 클립을 추가 선언

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // 자주 쓰는 효과음은 별도 메서드로 만들어도 됨
    public void PlayButtonClick() => PlaySFX(buttonClickClip);
    public void PlayFireTrap() => PlaySFX(fireTrapClip);
    public void PlaySlimeJump() => PlaySFX(slimeJumpClip);
}
