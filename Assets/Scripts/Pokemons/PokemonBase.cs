using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//PARA DEFINIR LAS ESTADÍSTICAS BASE DE CADA POKEMON QUE SE CREE EN LA POKEDEX


//Tipos de Pokemon que puede haber
public enum PokemonType
{
    NINGUNO,
    NORMAL,
    FUEGO,
    AGUA,
    ELECTRICO,
    HIERBA,
    HIELO,
    LUCHA,
    VENENO,
    TIERRA,
    AEREO,
    PSIQUICO,
    BICHO,
    ROCA,
    FANTASMA,
    DRAGON,
    OSCURO,
    ACERO,
    HADA
}

//Matriz de tipos. Para establecer el daño que cada tipo de ataque hace a cada tipo de defensa
//0 no hace daño, 1 hace daño normal, 0.5 hace la mitad de daño, 2 hace el doble de daño
//Nota: solo se han quedado ajustados los cinco primeros tipos, habría que finalizar los demás
public class TypeMatrix
{
    private float[][] matrix =
    {
        //                    NOR  FUE  AGU  ELE  HIE  HIE  LUC  VEN  TIE  AER  PSI  BIC  ROC  FAN  DRA  OSC  ACE  HAD
        /*NOR*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  0f, 0.5f, 1f },
        /*FUE*/ new float[] { 1f, 0.5f,0.5f, 1f,  2f,  2f,  1f,  1f,  1f,  1f,  1f,  2f, 0.5f, 1f, 0.5f, 1f,  2f,  1f },
        /*AGU*/ new float[] { 1f,  2f, 0.5f, 1f,  1f, 0.5f, 1f,  1f,  2f,  1f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  1f },
        /*ELE*/ new float[] { 1f,  1f,  2f, 0.5f,0.5f, 1f,  1f,  1f,  0f,  2f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  1f },
        /*HIE*/ new float[] { 1f, 0.5f, 2f,  1f, 0.5f, 1f,  1f, 0.5f, 2f, 0.5f, 1f, 0.5f, 2f,  1f, 0.5f, 1f, 0.5f, 1f },
        /*HIE*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*LUC*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*VEN*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*TIE*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*AER*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*PSI*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*BIC*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*ROC*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*FAN*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*DRA*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*OSC*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*ACE*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f },
        /*HAD*/ new float[] { 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f }
    };
}

//Tipos de ataque (o movimiento) que los pokemon pueden aprender en cada nivel
//Es una clase [Serializable] para que luego pueda mostrarse en el editor de Unity
[Serializable]public class LearnableMove
{
    [SerializeField][Tooltip("Tipo de ataque o movimiento")]
    private MoveBase _move;
    public MoveBase Move => _move;

    [SerializeField] [Tooltip("Nivel necesario para aprender el ataque o movimiento")]
    private int level;
    public int Level => level;
}

//Se crea una nueva entrada en los menús del editor de Unity para poder crear los pokemon definidos aquí
[CreateAssetMenu(fileName = "Pokemon", menuName= "Pokemon/New pokemon")]

public class PokemonBase : ScriptableObject
{
    //Propiedades del Pokemon (privadas con una propiedad pública para su lectura)
    [SerializeField] [Tooltip("Identificador del Pokemon")]
    private int idPokemon;
    
    [SerializeField] [Tooltip("Nombre del Pokemon")]
    private string pokemonName;
    public string PokemonName => pokemonName;

    [SerializeField] [Tooltip("Descripción del Pokemon")][TextArea]
    private string description;
    public string Description => description;

    [SerializeField] [Tooltip("Primer tipo al que pertenece el Pokemon")]
    private PokemonType type1;
    public PokemonType Type1 => type1;

    [SerializeField] [Tooltip("Segundo tipo al que pertenece el Pokemon")]
    private PokemonType type2;
    public PokemonType Type2 => type1;

    [SerializeField] [Tooltip("Sprite delantero del Pokemon")]
    private Sprite frontSprite;
    public Sprite FrontSprite => frontSprite;

    [SerializeField] [Tooltip("Sprite trasero del Pokemon")]
    private Sprite  backSprite;
    public Sprite BackSprite => backSprite;

    //Estadísticas (privadas con una propiedad pública para su lectura)
    [SerializeField] [Tooltip("Puntos de vida máximos del Pokemon")]
    private int maxHP;
    public int MaxHp => maxHP;

    [SerializeField] [Tooltip("Puntos de ataque del Pokemon")]
    private int attack;
    public int Attack => attack;

    [SerializeField] [Tooltip("Puntos de defensa del Pokemon")]
    private int defense;
    public int Defense => defense;

    [SerializeField] [Tooltip("Puntos de ataque especial del Pokemon")]
    private int spAttack;
    public int SpAttack => spAttack;

    [SerializeField] [Tooltip("Puntos de defensa especial del Pokemon")]
    private int spDefense;
    public int SpDefense => spDefense;

    [SerializeField] [Tooltip("Velocidad del Pokemon")]
    private int speed;
    public int Speed => speed;

    [SerializeField] [Tooltip("Lista de ataques que puede aprender el Pokemon")]
    private List<LearnableMove> _learnableMoves;
    public List<LearnableMove> LearnableMoves => _learnableMoves;
}
