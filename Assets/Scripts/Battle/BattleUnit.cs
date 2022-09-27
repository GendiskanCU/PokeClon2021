using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;//Importa la librería del paquete de la Asset Store "DOTween (HOTween v2)" para las animaciones
using Unity.VisualScripting; 

//GESTIONA LOS POKEMON DE CADA BATALLA (DEL PLAYER O DEL ENEMIGO) Y RELLENA SU UI. SE COLOCARÁ COMO COMPONENTE
//DEL OBJETO IMAGE QUE REPRESENTA AL POKEMON EN EL CANVAS

[RequireComponent(typeof(Image))]
public class BattleUnit : MonoBehaviour
{
    [SerializeField][Tooltip("Pokemon base del pokemon")]
    private PokemonBase _base;

    //Nivel del pokemon
    [SerializeField][Tooltip("Nivel en el que se encuentra el pokemon")]
    private int _level;
    
    [SerializeField] [Tooltip("HUD del pokemon que participa en la batalla")]
    private BattleHUD hud;
    public BattleHUD HUD => hud;

    //Para controlar si el pokemon es el de player o del enemigo
    [SerializeField][Tooltip("¿Es el pokemon del player? (¿o del enemigo?")]
    private bool isPlayer;
    public bool IsPlayer => isPlayer;

    //Pokemon que participa en la batalla (público para ser accesible desde el BattleManager)
    public Pokemon Pokemon { get; set; }
    
    //Imagen que representa al pokemon
    private Image pokemonImage;


    //Para las animaciones del pokemon
    //Posición inicial del sprite pokemon
    private Vector3 initialPosition;
    
    //Color inicial del pokemon
    private Color initialColor;
    
    [SerializeField] [Tooltip("Duración de la animación de entrada en escena")]
    private float startTimeAnimation = 1f;

    [SerializeField] [Tooltip("Duración de la animación de ataque")]
    private float attackTimeAnimation = 0.3f;

    [SerializeField] [Tooltip("Duración de la animación de derrota")]
    private float loseTimeAnimation = 1f;

    [SerializeField] [Tooltip("Duración de la animación de sufrir daño")]
    private float hitTimeAnimation = 0.15f;

    [SerializeField] [Tooltip("Duración de la animación de ser capturado por una pokeball")]
    private float capturedTimeAnimation = 0.5f;

    private void Awake()
    {
        pokemonImage = GetComponent<Image>();
        
        //Guarda la posición inicial del sprite del pokemon (se usa localPosition porque es con respecto a su padre)
        initialPosition = pokemonImage.transform.localPosition;
        
        //Guarda el color inicial
        initialColor = pokemonImage.color;
    }


    /// <summary>
    /// Inicializa el pokemon de batalla
    /// </summary>
    /// <param name="pokemonInBattle">Pokemon del player o del enemigo</param>
    public void SetupPokemon(Pokemon pokemonInBattle)
    {
        //Asigna valor al pokemon que participa en la batalla
        Pokemon = pokemonInBattle;

        //Establece la imagen que se debe mostrar según si es el pokemon del player o del enemigo
        pokemonImage.sprite = isPlayer ? Pokemon.Base.BackSprite : Pokemon.Base.FrontSprite;

        //Reinicia el color del pokemon
        pokemonImage.color = initialColor;
        
        //Restablece el tamaño del pokemon, que ha podido ser modificado al lanzar una pokeball
        transform.localScale = new Vector3(1, 1, 1);
        
        //Actualiza el HUD con la información del pokemon
        hud.SetPokemonData(Pokemon);
        
        //Animación de entrada
        PlayStartAnimation();
    }

    /// <summary>
    /// Reproduce la animación inicial de entrada del pokemon en la escena de batalla
    /// </summary>
    public void PlayStartAnimation()
    {
        //Primero "saca" a los pokemon de la escena
        pokemonImage.transform.localPosition = new Vector3(initialPosition.x + (isPlayer ? -1 : 1) * 400,
            initialPosition.y, initialPosition.z);
        
        //Después ejecuta una animación de entrada, haciendo regresar el sprite a la posición inicial
        pokemonImage.transform.DOLocalMoveX(initialPosition.x, startTimeAnimation);
    }

    /// <summary>
    /// Reproduce la animación de ataque del pokemon
    /// </summary>
    public void PlayAttackAnimation()
    {
        //Reproduce una secuencia de movimientos (animaciones)
        var sequence = DOTween.Sequence();
        //Primera animación de la secuencia (el pokemon hace un pequeño movimiento rápido en el eje x)
        sequence.Append(pokemonImage.transform.DOLocalMoveX(initialPosition.x + (isPlayer ? 1 : -1) * 60,
            attackTimeAnimation));
        //Segunda animación de la secuencia (el pokemon vuelve a su posición inicial)
        sequence.Append(pokemonImage.transform.DOLocalMoveX(initialPosition.x, attackTimeAnimation));
    }

    
    /// <summary>
    /// Reproduce la animación de recibir daño, haciendo cambios en el color del pokemon
    /// </summary>
    public void PlayReceiveAttackAnimation()
    {
        //Reproduce una secuencia de animaciones
        var sequence = DOTween.Sequence();
        //Primer cambio de color
        sequence.Append(pokemonImage.DOColor(Color.gray, hitTimeAnimation));
        //Vuelve al color inicial
        sequence.Append(pokemonImage.DOColor(initialColor, hitTimeAnimation));
    }

    /// <summary>
    /// Reproduce la animación de derrota del pokemon
    /// </summary>
    public void PlayFaintAnimation()
    {
        //Reproduce dos animaciones simultáneamente
        var sequence = DOTween.Sequence();
        //Una de las animaciones desplaza el pokemon hacia abajo
        sequence.Append(pokemonImage.transform.DOLocalMoveY(initialPosition.y - 200, loseTimeAnimation));
        //La otra hace un "Fade out". Como queremos que se reproduzca simultáneamente, se utiliza Joint, no Append
        sequence.Join(pokemonImage.DOFade(0f, loseTimeAnimation));
    }


    /// <summary>
    /// Reproduce la animación del pokemon siendo atrapado por una pokeball
    /// </summary>
    /// <returns></returns>
    public IEnumerator PlayCapturedAnimation()
    {
        //Animaciones que se reproducirán simultáneamente
        var sequence = DOTween.Sequence();
        //Animación de "Fade out"
        sequence.Append(pokemonImage.DOFade(0, capturedTimeAnimation ));
        //Animación de escalado decreciente 
        sequence.Join(transform.DOScale(new Vector3(0.25f, 0.25f, 1f), capturedTimeAnimation));
        //Animación de desplazamiento hacia arriba, en dirección a la pokeball
        sequence.Join(transform.DOLocalMoveY(initialPosition.y + 50f, capturedTimeAnimation));
        //Espera a que termine la secuencia de animaciones
        yield return sequence.WaitForCompletion();
    }
    
    /// <summary>
    /// Reproduce la animación del pokemon escapando de una pokeball
    /// </summary>
    /// <returns></returns>
    public IEnumerator PlayBreakOutAnimation()
    {
        //Animaciones que se reproducirán simultáneamente
        var sequence = DOTween.Sequence();
        //Animación de "Fade in"
        sequence.Append(pokemonImage.DOFade(1, capturedTimeAnimation ));
        //Animación de escalado creciente 
        sequence.Join(transform.DOScale(new Vector3(1f, 1f, 1f), capturedTimeAnimation));
        //Animación de desplazamiento hacia la posición inicial
        sequence.Join(transform.DOLocalMoveY(initialPosition.y, capturedTimeAnimation));
        //Espera a que termine la secuencia de animaciones
        yield return sequence.WaitForCompletion();
    }
}   

