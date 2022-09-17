using System;
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

    [SerializeField] [Tooltip("Lista de efectos que el movimiento o ataque puede provocar (contiene la stat a la" +
                              " que afectará y si el efecto es de mejora o de empeoramiento de la misma")]
    private MoveStatEffect effects;
    public MoveStatEffect Effects => effects;

    [SerializeField] [Tooltip("El efecto del ataque, ¿afectará al propio " +
                              "pokemon que ejecuta el ataque, o al que lo recibe?")]
    private MoveTarget target;
    public MoveTarget Target => target;

    [SerializeField] [Tooltip("Lista de efectos secundarios adicionales que el ataque puede provocar")]
    private List<SecondaryMoveStatEffect> secondaryEffects;
    public List<SecondaryMoveStatEffect> SecondaryEffects => secondaryEffects;

    
    [SerializeField] [Tooltip("Poder del ataque")]
    private int power;
    public int Power => power;

    [SerializeField] [Tooltip("Precisión del ataque (tasa de acierto)")][Range(0, 100)]
    private int accuracy;
    public int Accuracy => accuracy;
    
    [SerializeField] [Tooltip("¿El ataque siempre acierta? (no se verá afectado por tasa de acierto o evasión")]
    private bool alwaysHit;
    public bool AlwaysHit => alwaysHit;

    [SerializeField] [Tooltip("Número de puntos de poder (veces que puede ser utilizado antes de recargar) del ataque")]
    private int pp;
    public int PP => pp;

    [SerializeField] [Tooltip("Prioridad del movimiento o ataque")] [Range(-1, 1)]
    private int priority = 0;
    public int Priority => priority;

    //Para definir si el movimiento es especial
    private bool isSpecialMove;
    public bool IsSpecialMove => typeOfMove == MoveType.Special;
    
    
            /*El movimiento es especial si es de uno de los siguientes tipos
            (información extraída de https://bulbapedia.bulbagarden.net/wiki/Special_move)
            if (type == PokemonType.AGUA || type == PokemonType.FUEGO ||
                type == PokemonType.HIELO || type == PokemonType.DRAGON ||
                type == PokemonType.HIERBA || type == PokemonType.ELECTRICO ||
                type == PokemonType.OSCURO || type == PokemonType.PSIQUICO)*/
           
}



//Clase para definir las estadísticas que podrían sufrir un boost de mejora o empeoramiento al ejecutarse un ataque
//Y también si puede provocar un estado alterado (envenenado, congelado, etc.) o un estado volátil
//sobre el pokemon que recibe el ataque
[Serializable]
public class MoveStatEffect
{
    [SerializeField] [Tooltip("Lista de stats con tipo de boost, de mejora o empeoramiento, a las que afecta")]
    private List<StatBoosting> boostings;
    public List<StatBoosting> Boostings => boostings;

    
    [SerializeField] [Tooltip("Estado alterado que provoca el ataque sobre el pokemon objetivo, si lo hay")]
    private StatusConditionID status;
    public StatusConditionID Status => status;
    
    
    [SerializeField] [Tooltip("Estado volátil que provoca el ataque sobre el pokemon objetivo, si lo hay")]
    private StatusConditionID volatileStatus;
    public StatusConditionID VolatileStatus => volatileStatus;
}


/// <summary>
/// Clase para definir los posibles efectos secundarios que un movimiento o ataque puede ocasionar. Serán también
/// estados alterados, pero los definimos aparte de estos porque estos efectos secundarios no se producirán siempre,
/// sino que tendrán una probabilidad de que sucedan. Por ello, heredará de la clase MoveStatEffect
/// </summary>
[Serializable] public class SecondaryMoveStatEffect : MoveStatEffect
{
    [SerializeField] [Tooltip("Probabilidad de que el ataque provoque efecto secundario")]
    [Range(0, 100)] private int chance;
    public int Chance => chance;

    [SerializeField] [Tooltip("Pokemon que sufrirá el efecto secundario")]
    private MoveTarget target;
    public MoveTarget Target => target;
}



//Clase para vincular cada estadísticas que puede mejorar o empeorar con el tipo de boost
[Serializable]public class StatBoosting
{
    public Stat stat;
    //Tipo de boost (p.ej. -1 empeora, +1 mejora)
    public int boost;
    //Pokemon al que afecta el boost (puede ser el propio pokemon que ejecuta el ataque, o el pokemon atacado
    public MoveTarget target;
}


/// <summary>
/// Enumerado para poder definir a qué pokemon afectará el boost de mejora o empeoramiento de una stat cuando
/// se ejecute un ataque o movimiento (Self si es al propio atacante, Other si es al defensor)
/// </summary>
public enum MoveTarget
{
    Self, //El boost afectará al propio pokemon que ejecute el movimiento o ataque
    Other //El boost afectará al pokemon que recibe el ataque
}



