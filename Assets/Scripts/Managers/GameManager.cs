using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//Estados posibles del juego
public enum GameState
{
    TRAVEL, //El player está moviéndose por el mundo
    BATTLE, //El player está en una batalla
    DIALOG, //El player está dentro de un diálogo con un NPC o algún objeto interactivo
    CUTSCENE //El player ha activado una secuencia cinemática
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

    [SerializeField] [Tooltip("Panel de la transición animada cuando haya un cambio de escena")]
    private Image transitionPanel;
    
    
    private GameState _gameState;

    private TrainerController trainerInBattle;//Para guardar el entrenador rival de una batalla contra entrenador
    
    //Singleton
    public static GameManager SharedInstance;

    private void Awake()
    {
        _gameState = GameState.TRAVEL;

        //Singleton
        if (SharedInstance == null)
        {
            SharedInstance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    
  
    private void Start()
    {
        //Suscripción al evento del playerController para dar inicio una batalla
        playerController.OnPokemonEncountered += StartPokemonBattle;
        
        //Suscripción al evento del battleManager para conocer cuándo finaliza una batalla, y con qué resultado
        battleManager.OnBattleFinish += FinishPokemonBattle;
        
        //Suscripción al evento del DialogManager para conocer cuándo comienza un diálogo con NPC u objeto
        //Se define aquí directamente el código que se deberá ejecutar cuando el evento se active
        DialogManager.SharedInstance.OnDialogStart += () =>
        {
            _gameState = GameState.DIALOG;//Cambia el estado del juego al de diálogo
        };
        
        //Suscripción al evento del DialogManager para conocer cuándo finaliza un diálogo con NPC u objeto
        //Se define aquí directamente el código que se deberá ejecutar cuando el evento se active
        DialogManager.SharedInstance.OnDialogFinish += () =>
        {
            if(_gameState == GameState.DIALOG)
                _gameState = GameState.TRAVEL;//Cambia el estado del juego del diálogo a de andar por el mundo
            //TODO: si el diálogo es con un entrenador pokemon, hay que volver al estado Battle en vez de a Travel
        };
        
        //Suscripción al evento del Playercontroller que indica que el player ha entrado dentro del área de visión
        //de un entrenador pokemon, que activará el código que inicia una batalla con ese entrenador
        playerController.OnEnterTrainersFoV += (Collider2D trainerFovCollider) =>
        {
            //Cambia el estado de juego al de cinemática para que el player no pueda moverse hasta que finalice la misma
            _gameState = GameState.CUTSCENE;
            //Inicia la cinemática del entrenador dirigiéndose hacia el player para iniciar una batalla
            //El Fov que tiene el collider es hijo del mismo, por lo que primero captura al entrenador
            var trainer = trainerFovCollider.GetComponentInParent<TrainerController>();
            if (trainer != null)
            {
                StartCoroutine(trainer.TriggerTrainerBattle(playerController));
            }
        };
        
        SoundManager.SharedInstance.PlayMusic(worldAudioClip);
        
        //Inicializa la factoría de estados alterados de los pokemon
        StatusConditionFactory.InitFactory();
    }

    private void Update()
    {
        //Según el estado en que se encuentre el juego, se llevará a cabo la acción que corresponda
        if (_gameState == GameState.TRAVEL)
        {
            playerController.HandleUpdate();
        }
        else if (_gameState == GameState.BATTLE)
        {
            battleManager.HandleUpdate();
        }
        else if (_gameState == GameState.DIALOG)
        {
            DialogManager.SharedInstance.HandleUpdate();
        }
    }

    /// <summary>
    /// Inicia una batalla contra un pokemon salvaje
    /// </summary>
    private void StartPokemonBattle()
    {
        StartCoroutine(FadeInBattle());
    }
    
    
    /// <summary>
    /// Inicia un batalla contra un entrenador pokemon
    /// </summary>
    /// <param name="trainer">El entrenador contra el que se inicia la batalla</param>
    public void StartTrainerBattle(TrainerController trainer)
    {
        //Guarda el entrenador rival para poder realizar acciones adicionales sobre él al finalizar la batalla
        trainerInBattle = trainer;
        
        StartCoroutine(FadeInTrainerBattle(trainer));
    }

    /// <summary>
    /// Finaliza la batalla pokemon
    /// </summary>
    /// <param name="playerWin">Resultado que devuelve el evento OnBattleFinish del BattleManager</param>
    private void FinishPokemonBattle(bool playerWin)
    {
        if (trainerInBattle != null && playerWin)//Si la batalla era con un entrenador y el player ha vencido
        {
            trainerInBattle.AfterTrainerLostBattle();//Acciones adicionales sobre el entrenador rival
            trainerInBattle = null;//Resetea el entrenador para que quede limpio de cara a futuras batallas
        }
        StartCoroutine(FadeOutBattle());
    }
    

    /// <summary>
    /// Reproduce una animación de transición de escena al entrar en una batalla pokemon e inicia la misma
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeInBattle()
    {
        //Comienza a reproducir la música de batalla
        SoundManager.SharedInstance.PlayMusic(battleAudioClip);
        
        //Cambia el estado del juego
        _gameState = GameState.BATTLE;
        
        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(1.0f, 1.5f).WaitForCompletion();
        
        //Hace una pequeña pausa
        yield return new WaitForSeconds(0.5f);
        
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
        
        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(0f, 1.5f).WaitForCompletion();
    }
    
    
    /// <summary>
    /// Reproduce una animación de entrada e inicia la batalla contra un entrenador pokemon
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeInTrainerBattle(TrainerController trainer)
    {
        //Comienza a reproducir la música de batalla
        SoundManager.SharedInstance.PlayMusic(battleAudioClip);
        
        //Cambia el estado del juego
        _gameState = GameState.BATTLE;
        
        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(1.0f, 1.5f).WaitForCompletion();
        
        //Hace una pequeña pausa
        yield return new WaitForSeconds(0.5f);
        
        //Activa el gameObject de la batalla
        battleManager.gameObject.SetActive(true);
        
        //Desactiva la cámara que funciona cuando el player está moviéndose por el mundo
        worlMainCamera.gameObject.SetActive(false);
        
        //Captura la party de pokemons del player
        PokemonParty playerParty = playerController.GetComponent<PokemonParty>();
        
        //Captura la party de pokemons del entrenador rival
        PokemonParty trainerParty = trainer.GetComponent<PokemonParty>();
        
        //Inicia la batalla con el entrenador            
        battleManager.HandleStartTrainerBatlle(playerParty, trainerParty);
        
        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(0f, 1.5f).WaitForCompletion();
    }
    


    /// <summary>
    /// Reproduce una animación de transición de escena al salir de una batalla pokemon
    /// </summary> 
    /// <returns></returns>
    private IEnumerator FadeOutBattle()
    {
        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(1.0f, 1.5f).WaitForCompletion();
        //Hace una pequeña pausa
        yield return new WaitForSeconds(0.5f);
        
        //Comienza a reproducir la música del mundo
        SoundManager.SharedInstance.PlayMusic(worldAudioClip);
        
        //Desactiva el gameObject de la batalla
        battleManager.gameObject.SetActive(false);
        
        //Activa la cámara que funciona cuando el player está moviéndose por el mundo
        worlMainCamera.gameObject.SetActive(true);


        //Reproduce la transición de cambio de escena esperando a que finalice
        yield return transitionPanel.DOFade(0f, 1.5f).WaitForCompletion();
        
        //Cambia el estado del juego
        _gameState = GameState.TRAVEL;
    }
}
