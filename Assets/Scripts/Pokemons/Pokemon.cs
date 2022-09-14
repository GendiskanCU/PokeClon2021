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

    //Diccionario con las estadísticas del pokemon. La clave será el nombre de la estadística, definido en
    //el enumerado Stat de la clase PokemonBase y cada estadística tendrá un número entero en el valor
    public Dictionary<Stat, int> Stats { get; private set; }   //Pública para lectura, pero privada para escritura
    
    //Un diccionario también con las estadísticas del pokemon, pero que se utilizará para calcular y obtener sus
    //valores una vez que hayan sido afectados por modificadores de estadísticas durante una batalla. Guardará en
    //cada clave el nivel de mejora o empeoramiento de la estadística original y será un valor entre -6 y +6
    //siendo -6 el nivel máximo de empeoramiento, 0 que no hay modificador y +6 el nivel máximo de mejora
    public Dictionary<Stat, int> StatsBoosted { get; private set; }
    
    
    //Una cola para guardar y poder mostrar en pantalla los diversos cambios en las estadísticas (boosts)
    //de un pokemon que puedan ir sucediendo durante una batalla como consecuencia de la ejecución de ataques
    public Queue<string> StatusChangeMessages { get; private set; }
    
    
    //Para guardar el estado del pokemon (ninguno o alterado -envenenado, congelado, etc.)
    public StatusCondition StatusCondition { get; set; }
    
    //Para guardar el número de turnos que alguno de los estados alterados, como el de dormido, van a durar
    public int StatusNumTurns { get; set; }
    
    
    //Para guardar el estado de tipo volátil del pokemon 
    public StatusCondition VolatileStatusCondition { get; set; }
    
    //Para guardar el número de turnos que alguno de los estados volátiles va a durar
    public int VolatileStatusNumTurns { get; set; }
    
    
    //Evento para que el pokemon indique que su condición de estado ha cambiado
    public event Action OnStatusConditionChanged;

    //Vida actual del pokemon
    private int _hp;
    public int Hp
    {
        get => _hp;
        set
        {
            //La vida no podrá ser inferior a cero ni superior a la MaxHP
            _hp = Mathf.FloorToInt(Mathf.Clamp(value, 0, MaxHP));
        }
    }
    
    //Experiencia actual del pokemon
    private int _experience;
    public int Experience
    {
        get => _experience;
        set => _experience = value;
    }

    //Estadísticas públicas del pokemon, que pueden se objeto de modificadores por la acción de ataques enemigos
    public int Attack => GetStat(Stat.Attack);
    public int Defense => GetStat(Stat.Defense);
    public int SpAttack =>GetStat(Stat.SpAttack);
    public int SpDefense => GetStat(Stat.spDefense);
    public int Speed => GetStat(Stat.Speed);
    
    //Vida máxima del pokemon
    public int MaxHP { get; private set; }

    //Para poder saber si durante un turno de la batalla la vida del pokemon se ha visto reducida o aumentada
    public bool HasHPChanged { get; set; } = false;
    
    //Para poder guardar la vida que tenía el pokemon antes de que la misma haya sido modificada
    public int PreviousHPValue { get; set; }

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
    
    
    /// <summary>
    /// Inicializa los datos del pokemon
    /// </summary>
    public void InitPokemon()
    {
        //Inicializa la lista de ataques
        _moves = new List<Move>();
        //Rellena inicialmente la lista con los ataques ya se tienen con el nivel inicial del pokemon
        foreach (LearnableMove mov in _base.LearnableMoves)
        {
            if (mov.Level <= _level)
            {
                _moves.Add(new Move(mov.Move));
            }

            if (_moves.Count >= PokemonBase.NUMBER_OF_LEARNABLE_MOVES )
                //Se limita el número de ataques que puede aprender a cuatro
                break;
        }
        
        //Inicializa la experiencia que tiene el pokemon a partir de su nivel inicial
        _experience = Base.GetNeccessaryExperienceForLevel(_level);
        
        //Calcula las estadísticas iniciales del pokemon
        CalculateStats();
        
        //Inicializa los boost que afectan a las stats
        ResetBoostings();
        
        //Inicializa la vida actual con la máxima calculada en función del nivel inicial
        _hp = MaxHP;
        //Inicializa de la misma forma la vida que tenía el pokemon antes de alguna modificación
        PreviousHPValue = MaxHP;
        //Para forzar que al inicio de una batalla la barra de vida de la UI muestre bien los valores iniciales
        HasHPChanged = true;
        
        //Inicializa el estado alterado y volátil
        StatusCondition = null;
        VolatileStatusCondition = null;
    }


    /// <summary>
    /// Pone a cero los boost o modificadores de las stats del pokemon e inicializa la lista de mensajes sobre
    /// los boosts producidos durante las batallas
    /// </summary>
    private void ResetBoostings()
    {
        StatsBoosted = new Dictionary<Stat, int>()
        {
            {Stat.Attack, 0},
            {Stat.Defense, 0},
            {Stat.SpAttack, 0},
            {Stat.spDefense, 0},
            {Stat.Speed, 0},
            {Stat.Accuracy, 0},
            {Stat.Evasion, 0}
        };

        StatusChangeMessages = new Queue<string>();
    }
    
    
    
    /// <summary>
    /// Calcula las estadísticas del pokemon: vida máxima por un lado, y el resto de estadísticas que serán guardadas
    /// en el diccionario de estadísticas, Stats
    /// </summary>
    private void CalculateStats()
    {
        Stats = new Dictionary<Stat, int>();
        //Ataque del pokemon, en función del ataque base y el nivel actual. La fórmula que se utiliza es multiplicar
        //el ataque base por el nivel y el resultado dividirlo por 100. Como el ataque es un entero y el resultado de  la
        //división podrá tener decimales, se trunca el resultado con Floor. Al final, se suma una pequeña cantidad entre
        //1 y 5 para evitar que en niveles bajos el resultado final dé el valor cero o un valor demasiado pequeño
        Stats.Add(Stat.Attack, Mathf.FloorToInt((_base.Attack * _level) / 100) + 1);
        //Con el resto de estadísticas se utiliza una fórmula similar
        Stats.Add(Stat.Defense, Mathf.FloorToInt((_base.SpDefense * _level) / 100) + 2);
        Stats.Add(Stat.SpAttack, Mathf.FloorToInt((_base.SpAttack * _level) / 100) + 2);
        Stats.Add(Stat.spDefense, Mathf.FloorToInt((_base.SpDefense * _level) / 100) + 2);
        Stats.Add(Stat.Speed, Mathf.FloorToInt((_base.Speed * _level) / 100) + 3);

        //Se inicializa también la vida, si bien es una estadística que se debe tratar aparte, por lo que no
        //se encontrará en el diccionario de estadísticas del pokemon
        //Se calcula con una fórmula similar, sumándole al final el nivel actual del pokemon
        MaxHP = Mathf.FloorToInt((_base.MaxHp * _level) / 20) + _level;
    }


    /// <summary>
    /// Devuelve el valor de una estadística del diccionario de estadísticas del pokemon, teniendo en cuenta
    /// los posibles modificadores que puedan aplicarse a la misma
    /// </summary>
    /// <param name="stat">Estadística de la que deseamos obtener el valor</param>
    /// <returns>Valor en la estadística del pokemon</returns>
    private int GetStat(Stat stat)
    {
        //Guarda temporalmente el valor actual de la estadística
        int statValue = Stats[stat];
        
        //Aplica los modificadores de estado según el nivel que se guarda en el diccionario boosted
        int boost = StatsBoosted[stat];//Valor entre -6 y +6
        //Si el boost es negativo, significa que la estadística empeora y si es positivo que mejora
        //Hay 6 niveles de mejora o empeoramiento, cuyos valores a aplicar son 1, 1.5, 2, 2.5, 3, 3.5, 4
        //Nivel de boost:  -6    -5     -4     -3     -2     -1     0     1     2     3     4     5     6
        //Modificador  :   -4    -3.5   -3     -2.5   -2     -1.5   1     1.5   2     2.5   3     3.5   4
        float statsModifier =  1 + Mathf.Abs(boost) / 2.0f;
        if (boost >= 0)
        {
            statValue = Mathf.FloorToInt(statValue * statsModifier);
        }
        else
        {
            statValue = Mathf.FloorToInt(statValue / statsModifier);
        }
        
        //Devuelve el valor de la estadística después de haber aplicado los posibles modificadores
        return statValue;
    }


    /// <summary>
    /// Aplica un boost a una de las stats de un Pokemon y guarda el mensaje informativo en la cola
    /// de mensajes que se mostrarán en la UI de batalla
    /// </summary>
    /// <param name="statBoostings">La stat con el valor de boost que se debe aplicar</param>
    public void ApplyBoost(StatBoosting statBoosting)
    {
       
        //La stat 
        var stat = statBoosting.stat;
        //Valor de boost a aplicar sobre la stat
        var value = statBoosting.boost;
            
        //Actualiza el valor correspondiente del diccionario de estados del pokemon
        //Limitamos los valores mínimos y máximos para que siempre estén entre -6 y +6
        StatsBoosted[stat] = Mathf.Clamp(StatsBoosted[stat] + value, -6, 6);

        if (value > 0) //Si el boost ha sido positivo (mejora la stat del pokemon)
        {
            //Añade un nuevo mensaje a la cola de strings informando del incremento
            StatusChangeMessages.Enqueue($"{Base.PokemonName} ha incrementado su " +
                                         $"{stat}");
        }
        else if (value < 0) //Si el boost ha sido negativo
        {
            //Añade el mensaje informando de la reducción
            StatusChangeMessages.Enqueue($"{Base.PokemonName} ha reducido su " +
                                         $"{stat}");
        }
        else //Si el boost ha sido cero, no se produce ningún efecto
        {
            StatusChangeMessages.Enqueue($"{Base.PokemonName} no nota ningún efecto en sus stats");
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
        UpdateHP(totalDamage);
        
        //Comprueba el resultado y se marca si el pokemon ha sido vencido
        if (Hp <= 0)
        {
            damageDesc.Fainted = true;
        }
    
        //Devuelve el resultado describiendo el daño recibido
        return damageDesc;
    }


    /// <summary>
    /// Actualiza la vida de un pokemon tras sufrir daño durante la batalla
    /// </summary>
    /// <param name="damage">Cantidad de daño que sufre el pokemon</param>
    public void UpdateHP(int damage)
    {
        //Indica que la vida ha sido modificada
        HasHPChanged = true;
        
        //Guarda la vida que el pokemon tenía antes de ser modificada
        PreviousHPValue = Hp;
        
        //Aplica el daño al pokemon asegurando que la vida no baja de 0
        Hp -= damage;

        if (Hp <= 0)
        {
            Hp = 0;
        }
    }
    
    
    /// <summary>
    /// Aplica un estado alterado al pokemon
    /// </summary>
    /// <param name="id">La id del estado alterado a aplicar</param>
    public void SetConditionStatus(StatusConditionID id)
    {
        //A un pokemon solo puede afectarle un estado alterado. Si ya tiene uno aplicado, no puede aplicársele otro
        if (StatusCondition != null)
        {
            return;
        }
        
        StatusCondition = StatusConditionFactory.StatusConditions[id];
        //Añade un nuevo mensaje a la cola de strings informando del cambio de estado del pokemon
        StatusChangeMessages.Enqueue($"{Base.PokemonName} {StatusCondition.StartMessage}");
        
        //Invoca el evento que efectúa alguna acción adicional al aplicarse un estado alterado, siempre que en el
        //estado alterado actual esté definido este evento (ver que se ponen los "?"). Esto ocurre, por ejemplo,
        //en el estado "dormido", en el que al ser aplicado se debe establecer el número de turnos que va a durar
        StatusCondition?.OnApplyStatusCondition?.Invoke(this);
        
        //Invoca el evento indicando que el estado ha sido modificado (este evento será recogido en BattleHUD)
        OnStatusConditionChanged?.Invoke();
    }
    
    
    /// <summary>
    /// Aplica un estado volátil al pokemon
    /// </summary>
    /// <param name="id">La id del estado volátil a aplicar</param>
    public void SetVolatileConditionStatus(StatusConditionID id)
    {
        //A un pokemon solo puede afectarle un estado volátil. Si ya tiene uno aplicado, no puede aplicársele otro
        if (VolatileStatusCondition != null)
        {
            return;
        }
        
        VolatileStatusCondition = StatusConditionFactory.StatusConditions[id];
        //Añade un nuevo mensaje a la cola de strings informando del cambio de estado del pokemon
        StatusChangeMessages.Enqueue($"{Base.PokemonName} {VolatileStatusCondition.StartMessage}");
        
        //Invoca el evento que efectúa alguna acción adicional al aplicarse un estado volátil, siempre que en el
        //estado volátil actual esté definido este evento (ver que se ponen los "?"). Esto ocurre, por ejemplo,
        //en estados que al ser aplicados se establece el número de turnos que va a durar
        VolatileStatusCondition?.OnApplyStatusCondition?.Invoke(this);
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

    /// <summary>
    /// Comprueba si el pokemon tiene suficiente experiencia para subir de nivel. En caso contrario, su nivel subirá
    /// </summary>
    /// <returns>True si el pokemon sube de nivel / false si todavía no tiene la suficiente experiencia</returns>
    public bool NeedsToLevelUp()
    {
        //Si el pokemon tiene suficiente experiencia para subir al siguiente nivel
        if (_experience > Base.GetNeccessaryExperienceForLevel(_level + 1))
        {
            int currentMaxHp = MaxHP;//Guarda la vida máxima antes de la subida de nivel
            _level++;//Sube de nivel
            
            //La vida actual se incrementa en la misma cantidad que haya aumentado la vida máxima al subir de nivel
            Hp += (MaxHP - currentMaxHp);
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve el movimiento que el pokemon puede ser capaz de aprender a su nivel actual
    /// </summary>
    /// <returns>El movimiento aprendible al nivel actual del pokemon</returns>
    public LearnableMove GetLearnableMoveAtCurrentLevel()
    {
        return Base.LearnableMoves.Where(move => move.Level == _level).FirstOrDefault();
        
        /*  Nota: con la sintaxis tradicional, equivale a:
        
        foreach (LearnableMove move in _base.LearnableMoves)
        {
            if (move.Level == _level)
            {
                return move;
            }
        }

        return null;  */
    }


    /// <summary>
    /// Implementa la lógica de aprender un nuevo movimiento o ataque
    /// </summary>
    /// <param name="learnableMove">El movimiento a aprender</param>
    public void LearnMove(LearnableMove learnableMove)
    {
        //Comprueba que puede aprenderse el nuevo movimiento (no se ha superado el límite de movimientos aprendidos)
        if (Moves.Count > PokemonBase.NUMBER_OF_LEARNABLE_MOVES)
        {
            return;
        }
        
        //Se crea el nuevo movimiento, en el que se copia el que ha de aprender
        Move moveToLearn = new Move(learnableMove.Move);
        //Se añade el nuevo movimiento a la lista de movimientos aprendidos por el pokemon
        Moves.Add(moveToLearn);
    }


    /// <summary>
    /// Realiza las acciones necesarias sobre un pokemon cuando una batalla haya finalizado, para que el
    /// pokemon quede bien inicializado de cara a la siguiente batalla
    /// </summary>
    public void OnBattleFinish()
    {
        //Vuelve a dejar los boost de las stats a cero
        ResetBoostings();
        
        //Si hay algún estado volátil activo, éste desaparece al terminar la batalla
        VolatileStatusCondition = null;
    }


    /// <summary>
    /// Implementa la "curación" del pokemon del efecto de algún estado alterado durante la batalla
    /// </summary>
    public void CureStatusCondition()
    {
        StatusCondition = null;
        //Invoca el evento que indica que el estado alterado del pokemon ha cambiado
        OnStatusConditionChanged?.Invoke();
    }
    
    
    /// <summary>
    /// Implementa la "curación" del pokemon del efecto de algún estado volátil
    /// </summary>
    public void CureVolatileStatusCondition()
    {
        VolatileStatusCondition = null;
    }


    /// <summary>
    /// Realiza las acciones adicionales necesarias sobre un pokemon al inicio de cada turno, como
    /// la activación de los efectos de estados alterados y/o volátiles
    /// </summary>
    /// <returns>True si el pokemon puede atacar en este turno, false en caso contrario</returns>
    public bool OnStartTurn()
    {
        bool canPerformMovement = true;//Por defecto el pokemon podrá atacar
        
        //Activa el evento OnStartTurn para comprobar si el pokemon no puede atacar por estar afectado por un
        //estado alterado de parálisis, dormido, etc.
        if (StatusCondition?.OnStartTurn != null) //Si hay asignado un estado alterado que deba aplicarse al StartTurn
        {
            if (!StatusCondition.OnStartTurn(this))//Si el estado alterado no permite atacar al pokemon
            {
                canPerformMovement = false;
            }
        }
        
        //Activa el evento OnStartTurn para comprobar si el pokemon no puede atacar por estar afectado por un
        //estado volátil.
        if (VolatileStatusCondition?.OnStartTurn != null) //Si hay asignado un estado volátil
        {
            if (!VolatileStatusCondition.OnStartTurn(this))//Si el estado volátil no permite atacar al pokemon
            {
                canPerformMovement = false;
            }
        }

        //Devuelve el resultado final tras las comprobaciones realizadas
        return canPerformMovement;
    }
    
    
    /// <summary>
    /// Realiza las acciones adicionales necesarias sobre un pokemon tras la finalización de cada turno, como
    /// la activación de los efectos de estados alterados de envenenamiento, quemado, etc.
    /// </summary>
    public void OnFinishTurn()
    {
        //Invoca el evento OnFinishTurn de StatusCondition para que puedan aplicarse los efectos de estado alterado
        //que el pokemon pueda haber sufrido en el turno tras el última ataque recibido
        StatusCondition?.OnFinishTurn?.Invoke(this);
        
        //Invoca el evento OnFinishTurn de VolatileStatusCondition para que puedan aplicarse los efectos de estado
        //volátil que pueda estar afectando al pokemon
        VolatileStatusCondition?.OnFinishTurn?.Invoke(this);
    }
}



//Estructura para describir las causas que provocan el daño (el tipo de ataque, si es crítico, y si provoca la derrota)
public struct DamageDescription
{
    public float Critical { get; set; }
    public float AttackType { get; set; }
    public bool Fainted { get; set; }
}
