using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}