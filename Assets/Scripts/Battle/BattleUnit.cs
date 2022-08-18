using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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
    
    //Para controlar si el pokemon es el de player o del enemigo
    [SerializeField][Tooltip("¿Es el pokemon del player? (¿o del enemigo?")]
    private bool isPlayer;

    //Pokemon que participa en la batalla (público para ser accesible desde el BattleManager)
    public Pokemon Pokemon { get; set; }
    
    

    /// <summary>
    /// Inicializa el pokemon de batalla
    /// </summary>
    public void SetupPokemon()
    {
        Pokemon = new Pokemon(_base, _level);

        //Establece la imagen que se debe mostrar según si es el pokemon del player o del enemigo
        GetComponent<Image>().sprite = isPlayer ? Pokemon.Base.BackSprite : Pokemon.Base.FrontSprite;
    }
}   

