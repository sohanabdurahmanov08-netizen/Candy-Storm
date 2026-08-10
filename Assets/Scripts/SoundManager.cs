using UnityEngine;

public enum SoundType
{
    TypeSelect,
    TypeMove,
    TypePop,
    TypeGameOver
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public AudioClip SelectClip;
    public AudioClip MoveClip;
    public AudioClip PopClip;
    public AudioClip GameOverClip;

    private AudioSource _audioSource;

    void Awake()
    {
        Instance = this;
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            Debug.LogWarning("SoundManager: на объекте нет компонента AudioSource!");
    }

    public void PlaySound(SoundType type)
    {
        if (_audioSource == null)
            return;

        AudioClip clip = null;
        switch (type)
        {
            case SoundType.TypeSelect: clip = SelectClip; break;
            case SoundType.TypeMove: clip = MoveClip; break;
            case SoundType.TypePop: clip = PopClip; break;
            case SoundType.TypeGameOver: clip = GameOverClip; break;
        }

        if (clip != null)
            _audioSource.PlayOneShot(clip);
        else
            Debug.LogWarning("SoundManager: клип для " + type + " не назначен в инспекторе!");
    }
}