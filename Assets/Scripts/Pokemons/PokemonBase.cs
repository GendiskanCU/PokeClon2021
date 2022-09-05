using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;

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

//Tipos de ratio de crecimiento o subida de nivel (algunos pokemon subirán de nivel más rápido que otros según esto)
public enum GrowthRate
{
    Erratic, Fast, MediumFast, MediumSlow, Slow, Fluctuating
}


//Matriz de tipos. Para establecer el daño que cada tipo de ataque hace a cada tipo de defensa
//0 no hace daño, 1 hace daño normal, 0.5 hace la mitad de daño, 2 hace el doble de daño
//Nota: solo se han quedado ajustados los cinco primeros tipos, habría que finalizar los demás
public class TypeMatrix
{
    private static float[][] matrix =    //Estática para que no sea necesario crear una instancia
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

    /// <summary>
    /// Devuelve el multiplicador efectivo de un ataque pokemon en base a los datos de la matriz de tipos pokemon
    /// </summary>
    /// <param name="attackType">Tipo de ataque del pokemon atacante</param>
    /// <param name="pokemonDefenderType">Tipo del pokemon defensor</param>
    /// <returns></returns>
    public static float GetMultiplierEfectiveness(PokemonType attackType, PokemonType pokemonDefenderType)
    {
        //Si alguno de los pokemon es de tipo "ninguno" se devuelve el multiplicador estándar
        if (attackType == PokemonType.NINGUNO || pokemonDefenderType == PokemonType.NINGUNO)
        {
            return 1.0f;
        }
        
        //Cálculo de la fila del array bidimensional de la que extraer el multiplicador
        int row = (int) attackType - 1;//Se resta 1 porque el tipo "ninguno" no está en la matriz
        //Cálculo de la columna del array bidimensional de la que extraer el multiplicador
        int col = (int) pokemonDefenderType - 1;

        return matrix[row][col];
    }
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
    
    //Número máximo de ataques que el pokemon puede aprender
    public static int NUMBER_OF_LEARNABLE_MOVES { get; } = 4;//De solo lectura

    [SerializeField] [Tooltip("Ratio de captura del pokemon")][Range(0, 255)]
    private int catchRate = 255;
    public int CatchRate => catchRate;

    [SerializeField] [Tooltip("Experiencia base del pokemon (cantidad que otorga cuando sea vencido")]
    private int experienceBase;
    public int ExperienceBase => experienceBase;

    [SerializeField] [Tooltip("Ratio de crecimiento de nivel del pokemon")]
    private GrowthRate pokemonGrowthRate;
    public GrowthRate PokemonGrowthRate => pokemonGrowthRate;


    /// <summary>
    /// Calcula y devuelve la cantidad de experiencia necesaria para que un pokemon suba a un determinado nivel
    /// teniendo en cuenta la GrowthRate de ese pokemon y según las fórmulas de referencia de la Bulbapedia
    /// </summary>
    /// <param name="level">Nivel al que se quiere subir</param>
    /// <returns></returns>
    public int GetNeccessaryExperienceForLevel(int level)
    {
        switch (pokemonGrowthRate) //Según el ratio de crecimiento del pokemon, la experiencia necesaria será:
        {
            case GrowthRate.Fast:
                return Mathf.FloorToInt(4 * Mathf.Pow(level, 3) / 5);
                //break;
            case GrowthRate.MediumFast:
                return Mathf.FloorToInt(Mathf.Pow(level, 3));
                //break;
            case GrowthRate.MediumSlow:
                return Mathf.FloorToInt(6 * Mathf.Pow(level, 3) / 5 - 15 * Mathf.Pow(level, 2) +
                    100 * level - 140);
                //break;
            case GrowthRate.Slow:
                return Mathf.FloorToInt(5 * Mathf.Pow(level, 3) / 4);
                //break;
            case GrowthRate.Erratic:
                if (level < 50)
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) * (100 - level) / 50);
                }
                else if (level < 68)
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) * (150 - level) / 100);
                }
                else if (level < 98)
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) *
                        Mathf.FloorToInt((1911 - 10 * level) / 3) / 500);
                }
                else
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) * (160 - level) / 50);
                }

                //break;
            case GrowthRate.Fluctuating:
                if (level < 15)
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) *
                        (Mathf.FloorToInt((level + 1) / 3) + 24) / 50);
                }
                else if (level < 36)
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) * (level + 14) / 50);
                }
                else
                {
                    return Mathf.FloorToInt(Mathf.Pow(level, 3) * Mathf.FloorToInt((level / 2) + 32) / 50);
                }

                //break;
        }

        return -1;
    }
}
