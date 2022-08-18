using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//POKEMON INSTANCIABLES.
//SE CONTROLAN SUS ESTADÍSTICAS PARTIENDO DE LAS ESTADÍSTICAS BASE Y EL NIVEL ACTUAL DEL POKEMON

public class Pokemon
{
    //Propiedades y estadísticas base del pokemon
    private PokemonBase _base;
    public PokemonBase Base => _base;

    //Nivel actual del pokemon (en función del nivel, las estadísticas base variarán)
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
    
    //Vida máxima del pokemon. Similar, pero asegurando al menos 10 puntos
    public int MaxHP => Mathf.FloorToInt((_base.MaxHp * _level) / 100) + 10;


    //Ataques o movimientos que tiene el Pokemon
    private List<Move> _moves;
    public List<Move> Moves
    {
        get => _moves;
        set => _moves = value;
    }
    
    //Constructor
    public Pokemon(PokemonBase pokemonBase, int pokemonLevel)
    {
        _base = pokemonBase;
        _level = pokemonLevel;
        //Inicializa la vida actual
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
}
