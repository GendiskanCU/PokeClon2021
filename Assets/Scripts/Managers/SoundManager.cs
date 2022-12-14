using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;


public class SoundManager : MonoBehaviour
{
    [SerializeField] [Tooltip("AudioSource de los efectos de sonido")]
    private AudioSource effectsSource;

    [SerializeField] [Tooltip("AudioSource de la música del juego")]
    private AudioSource musicSource;

    [SerializeField] [Tooltip("Rango (mínimo y máximo) del pitch (tono) del audio")]
    private Vector2 pitchRange = Vector2.zero;
    
    [SerializeField] [Tooltip("Lista de sonidos aleatorios que se escucharán al escribir caracteres" +
                              "en las cajas de diálogo")]
    private AudioClip[] characterSounds;

    public static SoundManager SharedInstance;//Singleton

    private void Awake()
    {
        //Singleton permanente entre escenas
        if (SharedInstance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            SharedInstance = this;
            DontDestroyOnLoad(gameObject);
        }
    }


    /// <summary>
    /// Reproduce un efecto de sonido, deteniendo antes el que pudiera estar reproduciéndose ya
    /// </summary>
    /// <param name="clipToPlay">clip de sonido a reproducir</param>
    public void PlaySound(AudioClip clipToPlay)
    {
        effectsSource.Stop();
        effectsSource.clip = clipToPlay;
        effectsSource.pitch = 1;
        effectsSource.Play();
    }
    
    /// <summary>
    /// Reproduce un clip de música, deteniendo antes el que pudiera estar reproduciéndose ya
    /// </summary>
    /// <param name="clipToPlay">clip de música a reproducir</param>
    public void PlayMusic(AudioClip clipToPlay)
    {
        musicSource.Stop();
        musicSource.clip = clipToPlay;
        musicSource.Play();
    }

    /// <summary>
    /// Reproduce aleatoriamente uno de los efectos de sonidos de escribir una letra de la lista interna characterSounds
    /// </summary>
    public void PlayRandomCharacterSound()
    {
        RandomSoundEffect(characterSounds);
    }
    

    /// <summary>
    /// Reproduce aleatoriamente un efecto de sonido de una lista, con un pitch (tono) aleatorio
    /// </summary>
    /// <param name="clips">La lista (array) con efectos de sonido</param>
    private void RandomSoundEffect(params AudioClip [] clips)
    {
        int randomIndex = UnityEngine.Random.Range(0, clips.Length);
        float randomPitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);

        effectsSource.pitch = randomPitch;
        PlaySound(clips[randomIndex]);
    }

}
