using UnityEngine;

public class MusicController : MonoBehaviour
{
    public static MusicController Instance;

    [SerializeField] private AudioClip mainClip;
    [SerializeField] private AudioClip remixClip;
    [SerializeField] private AudioClip menuClip;
    
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayMain()
    {
        audioSource.clip = mainClip;
        audioSource.Play();
    }
    
    public void PlayRemix()
    {
        audioSource.clip = remixClip;
        audioSource.Play();
    }
    
    public void PlayMenu()
    {
        audioSource.clip = menuClip;
        audioSource.Play();
    }


    public void StopMusic() => audioSource.Stop();
    public void PauseMusic() => audioSource.Pause();
    public void ResumeMusic() => audioSource.UnPause();
}
