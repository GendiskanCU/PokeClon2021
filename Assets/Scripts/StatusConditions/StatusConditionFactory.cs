using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LOS ESTADOS ALTERADOS QUE PUEDEN AFECTAR A UN POKEMON DURANTE LA BATALLA


//Identificador de los diversos estados
public enum StatusConditionID
{
    none, //Ninguno
    brn, //Quemado
    frz, //Congelado
    par, //Paralizado
    psn, //Envenenado
    slp //Dormido
}

public class StatusConditionFactory
{
    //Diccionario que asocia cada identificador de los estados con la definición del estado de que se trata
    //Será accesible directamente desde otros scripts
    public static Dictionary<StatusConditionID, StatusCondition> StatusConditions { get; set; } =
        new Dictionary<StatusConditionID, StatusCondition>()
        {
            { //Estado envenenado
                StatusConditionID.psn,
                new StatusCondition()
                {
                    Name = "Veneno",
                    Description = "Hace que el pokemon sufra daño al finalizar cada turno",
                    StartMessage = "ha sido envenenado",
                    //Código (Método) que se ejecutará al activarse el evento OnFinishTurn del StatusCondition, lo
                    //cual se hará al final de cada turno, desde RunMovement  en el BattleManager
                    OnFinishTurn = PoisonEffect  
                }
            },
            
            { //Estado quemado
            StatusConditionID.brn,
            new StatusCondition()
            {
                Name = "Quemado",
                Description = "Hace que el pokemon sufra daño al finalizar cada turno",
                StartMessage = "ha sido quemado",
                //Código (Método) que se ejecutará al activarse el evento OnFinishTurn del StatusCondition, lo
                //cual se hará al final de cada turno, desde RunMovement  en el BattleManager
                OnFinishTurn = BurnEffect  
            }
        }
        };


    
    /// <summary>
    /// Implementa el efecto del estado "envenenado" sobre un pokemon
    /// </summary>
    /// <param name="pokemon">El pokemon que sufre el efecto</param>
    private static void PoisonEffect(Pokemon pokemon)
    {
        //El envenenamiento produce un daño que se calcula en función de la vida total del pokemon, según
        //la fórmula que se puede consultar en la "Bulbapedia", dividiéndola por 8
        pokemon.UpdateHP(Mathf.CeilToInt( (float)pokemon.MaxHP / 8f));
        //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI al final del ataque
        pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} sufre los efectos del veneno"); 
    }
    
    /// <summary>
    /// Implementa el efecto del estado "quemado" sobre un pokemon
    /// </summary>
    /// <param name="pokemon">El pokemon que sufre el efecto</param>
    private static void BurnEffect(Pokemon pokemon)
    {
        //El envenenamiento produce un daño que se calcula en función de la vida total del pokemon, según
        //la fórmula que se puede consultar en la "Bulbapedia", dividiéndola por 15
        pokemon.UpdateHP(Mathf.CeilToInt( (float)pokemon.MaxHP / 15f));
        //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI al final del ataque
        pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} sufre los efectos de la quemadura"); 
    }
    
    
}
