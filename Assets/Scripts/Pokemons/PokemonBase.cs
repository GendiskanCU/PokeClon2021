using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

//Se crea una nueva entrada en los menús del editor de Unity para poder crear los pokemon definidos aquí
[CreateAssetMenu(fileName = "Pokemon", menuName= "Pokemon/New pokemon")]

public class PokemonBase : ScriptableObject
{
    [SerializeField] [Tooltip("Identificador del Pokemon")]
    private int idPokemon;
    
    [SerializeField] [Tooltip("Nombre del Pokemon")]
    private string pokemonName;
    
    [SerializeField] [Tooltip("Descripción del Pokemon")][TextArea]
    private string description;

    [SerializeField] [Tooltip("Primer tipo al que pertenece el Pokemon")]
    private PokemonType type1;
    
    [SerializeField] [Tooltip("Segundo tipo al que pertenece el Pokemon")]
    private PokemonType type2;

    [SerializeField] [Tooltip("Sprite delantero del Pokemon")]
    private Sprite frontSprite;

    [SerializeField] [Tooltip("Sprite trasero del Pokemon")]
    private Sprite  backSprite;
    
    //Estadísticas
    [SerializeField] [Tooltip("Puntos de vida máximos del Pokemon")]
    private int maxHP;

    [SerializeField] [Tooltip("Puntos de ataque del Pokemon")]
    private int attack;

    [SerializeField] [Tooltip("Puntos de defensa del Pokemon")]
    private int defense;

    [SerializeField] [Tooltip("Puntos de ataque especial del Pokemon")]
    private int spAttack;

    [SerializeField] [Tooltip("Puntos de defensa especial del Pokemon")]
    private int spDefense;

    [SerializeField] [Tooltip("Velocidad del Pokemon")]
    private int speed;
}
