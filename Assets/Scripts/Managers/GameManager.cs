using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

//Estados posibles del juego
public enum GameState
{
    TRAVEL, //El player está moviéndose por el mundo
    BATTLE //El player está en una batalla
}

[RequireComponent(typeof(ColorManager))]

public class GameManager : MonoBehaviour
{
    [SerializeField] [Tooltip("Controlador del player")]
    private PlayerController playerController;

    [SerializeField] [Tooltip("Controlador de las batallas")]
    private BattleManager battleManager;

    [SerializeField] [Tooltip("Cámara del mundo (no de la batalla)")]
    private Camera worlMainCamera;

    [SerializeField] [Tooltip("Música del mundo (no de la batalla)")]
    private AudioClip worldAudioClip;
    
    [SerializeField] [Tooltip("Música de la batalla (no del mundo)")]
    private AudioClip battleAudioClip;
    
    
    private GameState _gameState;
    
    

    private void Awake()
    {
        _gameState = GameState.TRAVEL;
    }
    
  
    private void Start()
    {
        //Suscripción al evento del playerController para dar inicio una batalla
        playerController.OnPokemonEncountered += StartPokemonBattle;
        
        //Suscripción al evento del battleManager para conocer cuándo finaliza una batalla, y con qué resultado
        battleManager.OnBattleFinish += FinishPokemonBattle;
        
        SoundManager.SharedInstance.PlayMusic(worldAudioClip);
        
        //Inicializa la factoría de estados alterados de los pokemon
        StatusConditionFactory.InitFactory();
    }

    private void Update()
    {
        if (_gameState == GameState.TRAVEL)
        {
            playerController.HandleUpdate();
        }
        else if (_gameState == GameState.BATTLE)
        {
            battleManager.HandleUpdate();
        }
    }

    /// <summary>
    /// Inicia una batalla pokemon
    /// </summary>
    private void StartPokemonBattle()
    {
        //Comienza a reproducir la música de batalla
        SoundManager.SharedInstance.PlayMusic(battleAudioClip);
        
        //Cambia el estado del juego
        _gameState = GameState.BATTLE;
        
        //Activa el gameObject de la batalla
        battleManager.gameObject.SetActive(true);
        
        //Desactiva la cámara que funciona cuando el player está moviéndose por el mundo
        worlMainCamera.gameObject.SetActive(false);
        
        //Captura la party de pokemons del player
        PokemonParty playerParty = playerController.GetComponent<PokemonParty>();
        //Captura el pokemon salvaje del área de pokemons
        Pokemon wildPokemon = FindObjectOfType<PokemonMapArea>().GetComponent<PokemonMapArea>().GetRandomWildPokemon();
        
        //Se hace una copia del pokemon salvaje. Cuando el player vuelva a entrar más veces en un área pokemon,
        //se volverá a obtener un pokemon al azar de dicho área. Ahí puede surgir un problema: que dos o más veces
        //se obtenga exactamente el mismo pokemon de entre los posibles, de forma que siempre tendremos una referencia
        //al mismo objeto en memoria: si uno de los pokemon sube de nivel, varía su vida, etc. el/los otro/s también
        //lo harán. De ahí que sea necesario hacer una copia del pokemon obtenido cada vez en un nuevo pokemon,
        //pues asegura que son objetos diferentes e independientes
        Pokemon newWildPokemon = new Pokemon(wildPokemon.Base, wildPokemon.Level);

        //Inicia la batalla con el nuevo pokemon salvaje            
        battleManager.HandleStartBattle(playerParty, newWildPokemon);
    }

    /// <summary>
    /// Finaliza la batalla pokemon
    /// </summary>
    /// <param name="playerWin">Resultado que devuelve el evento OnBattleFinish del BattleManager</param>
    private void FinishPokemonBattle(bool playerWin)
    {
        //Comienza a reproducir la música del mundo
        SoundManager.SharedInstance.PlayMusic(worldAudioClip);
        
        //Cambia el estado del juego
        _gameState = GameState.TRAVEL;
        
        //Desactiva el gameObject de la batalla
        battleManager.gameObject.SetActive(false);
        
        //Activa la cámara que funciona cuando el player está moviéndose por el mundo
        worlMainCamera.gameObject.SetActive(true);

        if (!playerWin)
        {
            //TODO: Faltaría diferenciar entre victoria / derrota del player
        }

    }
}
