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
    /// <summary>
    /// Inicializa la factoría de StatusConditions
    /// </summary>
    public static void InitFactory()
    {
        //Recorre el diccionario y asigna al campo Id el valor de la clave correspondiente
        foreach (var condition in StatusConditions)
        {
            var id = condition.Key;
            var statusCondition = condition.Value;
            statusCondition.Id = id;
        }
    }
    
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
            },
            
            { //Estado paralizado
            StatusConditionID.par,
            new StatusCondition()
            {
                Name = "Paralizado",
                Description = "Hace que el pokemon pueda estar paralizado en el turno (no podría atacar)",
                StartMessage = "ha sido paralizado",
                //Código (Método) que se ejecutará al activarse el evento OnStartTurn del StatusCondition, lo
                //cual se hará al inicio de cada turno, desde RunMovement  en el BattleManager
                OnStartTurn = ParalizedEffect
            }
        },
            
            { //Estado congelado
            StatusConditionID.frz,
            new StatusCondition()
            {
                Name = "Congelado",
                Description = "Hace que el pokemon esté congelado, pero se puede curar aleatoriamente en un turno",
                StartMessage = "ha sido congelado",
                //Código (Método) que se ejecutará al activarse el evento OnStartTurn del StatusCondition, lo
                //cual se hará al inicio de cada turno, desde RunMovement  en el BattleManager
                OnStartTurn = FrozenEffect
            }
        },
            
            { //Estado dormido
                StatusConditionID.slp,
                new StatusCondition()
                {
                    Name = "Dormido",
                    Description = "Hace que el pokemon duerma durante un número determinado de turnos",
                    StartMessage = "se ha dormido",
                    //Código (Método) que se ejecutará al activarse el evento OnApplyStatusCondition del StatusCondition
                    //lo cual se hará cuando el estado sea aplicado desde RunMovement  en el BattleManager
                    OnApplyStatusCondition = (Pokemon pokemon) =>
                    {
                        //Establece aleatoriamente, entre 1 y 3, el número de turnos que el estado dormido durará
                        pokemon.StatusNumTurns = Random.Range(1, 4);
                        Debug.Log($"El pokemon va a dormir {pokemon.StatusNumTurns} turnos");
                    },
                    //Código (Método) que se ejecutará al activarse el evento OnStartTurn del StatusCondition
                    OnStartTurn = (Pokemon pokemon) =>
                    {
                        if (pokemon.StatusNumTurns <= 0) //Si ya se han terminado los turnos en que iba a estar dormido
                        {
                            //Cura al pokemon del estado dormido
                            pokemon.CureStatusCondition();
                            //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI 
                            pokemon.StatusChangeMessages.Enqueue($"¡{pokemon.Base.PokemonName} ha despertado!");
                            //El pokemon ya puede atacar
                            return true;
                        }
                      
                        //Si el pokemon todavía está dormido
                        //Descuenta el número de turnos que el pokemon permanecerá dormido
                        pokemon.StatusNumTurns--;
                        //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI 
                        pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} sigue dormido");
                        //El pokemon todavía duerme, no puede atacar
                        return false;
                    }
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


    /// <summary>
    /// Implementa el estado "paralizado" sobre un pokemon
    /// </summary>
    /// <param name="pokemon">El pokemon que sufre el efecto</param>
    /// <returns>True si el efecto no surte efecto y el pokemon podrá atacar en el siguiente turno, o
    /// false si el efecto de parálisis afecta al pokemon y éste no podrá atacar en el siguiente turno</returns>
    private static bool ParalizedEffect(Pokemon pokemon)
    {
        //Se calcula aleatoriamente el resultado, con una probabilidad del 25% de que la parálisis surta efecto
        if (Random.Range(0, 100) < 25)
        {
            //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI 
            pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} está paralizado y no puede atacar"); 
            return false;
        }
        
        //El pokemon no se ha paralizado, podrá atacar
        return true;
    }
    
    
    /// <summary>
    /// Implementa el estado "congelado" sobre un pokemon
    /// </summary>
    /// <param name="pokemon"></param>
    /// <returns>True si el pokemon se cura del efecto congelado, por lo que podrá atacar en el siguiente turno, o
    /// false si el pokemon no puede atacar porque no se ha curado del estado congelado</returns>
    private static bool FrozenEffect(Pokemon pokemon)
    {
        //Se calcula aleatoriamente el resultado, con una probabilidad del 25% de que se cure del efecto congelado
        if (Random.Range(0, 100) < 25)
        {
            //Añade un nuevo mensaje a la cola de strings del pokemon que se mostrarán en la UI 
            pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} ya no está congelado");
            //Aplica la "curación" al pokemon
            pokemon.CureStatusCondition();
            //El pokemon podrá atacar
            return true;
        }
        
        //El pokemon no se ha curado del estado congelado, no podrá atacar
        pokemon.StatusChangeMessages.Enqueue($"{pokemon.Base.PokemonName} está congelado y no puede atacar");
        return false;
    }
}
