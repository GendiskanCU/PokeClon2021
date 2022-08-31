using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

//POKEMON INSTANCIABLES.
//SE CONTROLAN SUS ESTADÍSTICAS PARTIENDO DE LAS ESTADÍSTICAS BASE Y EL NIVEL ACTUAL DEL POKEMON

//La clase debe ser serializable para que luego los pokemon que declaremos como [SerializaField]
//puedan aparecer en el editor de Unity
[Serializable]
public class Pokemon
{
    //Propiedades y estadísticas base del pokemon
    [SerializeField][Tooltip("Base pokemon")]
    private PokemonBase _base;
    public PokemonBase Base => _base;

    //Nivel actual del pokemon (en función del nivel, las estadísticas base variarán)
    [SerializeField][Tooltip("Nivel actual del pokemon")]
    private int _level;
    public int Level
    {
        get => _level;
        set => _level = value;
    }

    //Vida actual del pokemon
    private int _hp;
    public int Hp
    {
        get => _hp;
        set => _hp = value;
    }


    //Ataque del pokemon, en función del ataque base y el nivel actual. La fórmula que se utiliza es multiplicarlo
    //el ataque base por el nivel y el resultado dividirlo por 100. Como el ataque es un entero y el resultado de  la
    //división podrá tener decimales, se trunca el resultado con Floor. Al final, se suma una pequeña cantidad entre
    //1 y 5 para evitar que en niveles bajos el resultado final dé el valor cero o un valor demasiado pequeño
    public int Attack => Mathf.FloorToInt((_base.Attack * _level) / 100) + 1;
    
    //Con el resto de estadísticas se utiliza la misma fórmula
    public int Defense => Mathf.FloorToInt((_base.Defense * _level) / 100) + 2;
    public int SpAttack => Mathf.FloorToInt((_base.SpAttack * _level) / 100) + 2;
    public int SpDefense => Mathf.FloorToInt((_base.SpDefense * _level) / 100) + 2;
    public int Speed => Mathf.FloorToInt((_base.Speed * _level) / 100) + 3;
    
    //Vida máxima del pokemon. Similar, pero asegurando al menos 10 puntos y dividiendo por una cantidad menor
    public int MaxHP => Mathf.FloorToInt((_base.MaxHp * _level) / 20) + 10;


    //Ataques o movimientos que tiene el Pokemon
    private List<Move> _moves;
    public List<Move> Moves
    {
        get => _moves;
        set => _moves = value;
    }
    
    
    /// <summary>
    /// Constructor de un nuevo pokemon que se puede utilizar para copiar un pokemon en otro
    /// </summary>
    /// <param name="pBase">Pokemon base del nuevo pokemon</param>
    /// <param name="pLevel">Nivel del nuevo pokemon</param>
    public Pokemon(PokemonBase pBase, int pLevel)
    {
        _base = pBase;
        _level = pLevel;
        
        InitPokemon();
    }
    
    //Inicializa los datos del pokemon
    public void InitPokemon()
    {
        //Inicializa la vida actual con la máxima calculada en función del nivel inicial
        _hp = MaxHP;
        
        //Inicializa la lista de ataques
        _moves = new List<Move>();
        //Rellena inicialmente la lista con los ataques ya se tienen con el nivel inicial del pokemon
        foreach (LearnableMove mov in _base.LearnableMoves)
        {
            if (mov.Level <= _level)
            {
                _moves.Add(new Move(mov.Move));
            }

            if (_moves.Count >= 4 )//Aunque limita el número de ataques que puede aprender a cuatro
                break;
        }
    }

    /// <summary>
    /// Implementa el daño que sufre un pokemon al recibir el ataque de otro pokemon
    /// </summary>
    /// <param name="move">Movimiento o ataque que recibe el pokemon</param>
    /// <param name="attacker">Pokemon atacante</param>
    /// <returns>Estructura DamageDescription con los valores descriptivos del daño recibido</returns>
    public DamageDescription ReceiveDamage(Move move, Pokemon attacker)
    {
        //Se utilizará una fórmula para calcular el daño, a la que afectarán también
        //Modificadores, como:
        
        //Primer modificador: % de acierto
        float modifiers = Random.Range(0.85f, 1.0f);//% de acierto aleatorio entre 85-100%
        
        //2º modificador: multiplicador de efectividad de la matriz de tipos y combinando los dos tipos del defensor
        float Type1 = TypeMatrix.GetMultiplierEfectiveness(move.Base.Type, this.Base.Type1);
        float Type2 = TypeMatrix.GetMultiplierEfectiveness(move.Base.Type, this.Base.Type2);
        float Type = Type1 * Type2;
        
        modifiers *= Type;
        
        //Tercer modificador: probabilidad de crítico (que multiplicará el daño por 2) de un 6%
        float critical = 1f;
        if (Random.Range(0, 100) < 6)
            critical *= 2;

        modifiers *= critical;

        //Creamos una estructura describiendo el daño con los valores obtenidos (la estructura está definida más abajo)
        DamageDescription damageDesc = new DamageDescription()
        {
            Critical = critical,
            AttackType = Type,
            Fainted = false
        };
        
        //Del atacante se tiene en cuenta si el ataque realizado es de tipo especial
        float attack = (move.Base.IsSpecialMove) ? attacker.SpAttack : attacker.Attack;
        //Del pokemon que recibe el daño se hace lo mismo con su defensa, en función del ataque recibido
        float defense = (move.Base.IsSpecialMove) ? SpDefense : Defense; 
        
        //Cálculo del daño base, basada en la fórmula que se puede ver
        //en la web https://bulbapedia.bulbagarden.net/wiki/Damage
        float baseDamage = ((2f * attacker.Level / 5f + 2) * move.Base.Power * (attack / (float) defense)) / 50f + 2;
        
        //Cálculo de daño efectivo, aplicándole los modificadores 
        int totalDamage = Mathf.FloorToInt(baseDamage * modifiers);

        //Aplica el daño al pokemon
        Hp -= totalDamage;
        
        //Comprueba el resultado y se marca si el pokemon ha sido vencido
        if (Hp <= 0)
        {
            Hp = 0;
            damageDesc.Fainted = true;
        }
    
        //Devuelve el resultado describiendo el daño recibido
        return damageDesc;
    }

    /// <summary>
    /// Devuelve un movimiento (ataque) aleatorio de entre los que el pokemon tiene disponibles en su lista
    /// Se utilizará para implementar el ataque del pokemon enemigo
    /// </summary>
    /// <returns></returns>
    public Move RandonMove()
    {
        //Obtiene una nueva lista solo con los movimientos que no tengan agotados sus PP
        var movesWithPP = Moves.Where(m => m.Pp > 0).ToList();
        
        //Escoge uno de esos movimientos de forma aleatoria
        int randId = Random.Range(0, movesWithPP.Count);

        if (movesWithPP.Count > 0)//Si queda algún ataque con PP
        {
            return movesWithPP[randId];    
        }
        
        //Si no quedaran ataques con PP se llegaría hasta aquí
        return null;
        //TODO: para este caso se puede implementar un ataque que hace daño tanto al enemigo como al atacante
    }
}


//Estructura para describir las causas que provocan el daño (el tipo de ataque, si es crítico, y si provoca la derrota)
public struct DamageDescription
{
    public float Critical { get; set; }
    public float AttackType { get; set; }
    public bool Fainted { get; set; }
}
