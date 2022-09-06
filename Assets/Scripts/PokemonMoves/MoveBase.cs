using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//PARA DEFINIR LOS ATAQUES BASE ("MOVIMIENTOS") DE LOS POKEMON


//Tipos de movimiento a ataque que pueden existir
public enum MoveType
{
    Physical, //El ataque es de tipo físico
    Special, //El ataque es de tipo especial
    Stats  //El ataque modifica las estadísticas de un pokemon
}


//Crea una entrada en el menú contextual del editor de Unity
[CreateAssetMenu(fileName = "PokemonMove", menuName = "Pokemon/New movement")]


public class MoveBase : ScriptableObject
{
    //Privadas con propiedades de solo lectura
    
    [SerializeField] [Tooltip("Nombre del ataque")]
    private string attackName;
    public string AttackName => attackName;

    [SerializeField] [Tooltip("Descripción del ataque")] [TextArea]
    private string description;
    public string Description => description;

    [SerializeField] [Tooltip("Tipo de pokemon del que deriva el ataque o movimiento")]
    private PokemonType type;
    public PokemonType Type => type;

    [SerializeField] [Tooltip("Tipo ataque según la clase de efecto que produce")]
    private MoveType typeOfMove;
    public MoveType TypeOfMove => typeOfMove;

    [SerializeField] [Tooltip("Poder del ataque")]
    private int power;
    public int Power => power;

    [SerializeField] [Tooltip("Precisión del ataque")]
    private int accuracy;
    public int Accuracy => accuracy;

    [SerializeField] [Tooltip("Número de puntos de poder (veces que puede ser utilizado antes de recargar) del ataque")]
    private int pp;
    public int PP => pp;

    //Para definir si el movimiento es especial
    private bool isSpecialMove;
    public bool IsSpecialMove
    {
        get
        {
            return typeOfMove == MoveType.Special;
            
            /* Código sustituido:
            //El movimiento es especial si es de uno de los siguientes tipos
            //(información extraída de https://bulbapedia.bulbagarden.net/wiki/Special_move)
            if (type == PokemonType.AGUA || type == PokemonType.FUEGO ||
                type == PokemonType.HIELO || type == PokemonType.DRAGON ||
                type == PokemonType.HIERBA || type == PokemonType.ELECTRICO ||
                type == PokemonType.OSCURO || type == PokemonType.PSIQUICO)
            {
                return true;
            }
            else
            {
                return false;
            }  */
        }
    }
}



