using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AudioData
{
    public float pitch;
    public float volume;
}

public class RandomAudio : MonoBehaviour {
    [SerializeField] AudioSource m_AudioSource;
    public AudioClip[] clips;

    AudioData m_Default;

    void Start()
    {
        m_Default.pitch = m_AudioSource.pitch;
        m_Default.volume = m_AudioSource.volume;
    }

    public void Play()
    {
        Play(m_Default);
    }

    public void Play(AudioData data)
    {
        m_AudioSource.pitch = data.pitch;
        m_AudioSource.volume = data.volume;

        m_AudioSource.clip = clips[Random.Range(0, clips.Length)];
        m_AudioSource.Play();
    }
}
