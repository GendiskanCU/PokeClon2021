using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//PARA DEFINIR LAS ESTADÍSTICAS BASE DE CADA POKEMON QUE SE CREE EN LA POKEDEX


//Tipos de Pokemon que puede haber
public enum PokemonType
{
    None,
    Normal,
    Fire,
    Water,
    Electric,
    Grass,
    Fight,
    Ice,
    Poison,
    Ground,
    Fly,
    Psychic,
    Rock,
    Bug,
    Ghost,
    Dragon,
    Dark,
    Fairy,
    Steel
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

    [SerializeField] [Tooltip("Sprite trasero del Pokemon")]
    private Sprite  backSprite;
    
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
