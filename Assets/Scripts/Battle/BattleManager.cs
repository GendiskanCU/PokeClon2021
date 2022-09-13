using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
//Importa la librería del paquete de la Asset Store "DOTween (HOTween v2)" para las animaciones
using DG.Tweening;
using Random = UnityEngine.Random;


//GESTIONA LA BATALLA POKEMON PARA EL PLAYER O EL ENEMIGO

//Estados de la batalla
public enum BattleState
{
   StartBattle,//Inicio de la batalla
   ActionSelection,//El player tiene que hacer la selección de movimiento
   MovementSelection,//Se ejecuta el movimiento del player
   PerformMovement,//El player o el enemigo está realizando un movimiento
   Busy,//No se puede hacer nada
   PartySelectScreen,//En la pantalla de selección de pokemon de la party
   ItemSelectScreen,//En la pantalla de selección de items (inventario o mochila)
   ForgetMovement,//En la pantalla de selección de movimiento a olvidar al superar el límite de movs. aprendidos
   LoseTurn,//El player pierde su turno
   FinishBattle//La batalla ha finalizado
}

//Tipos de batalla (contra un pokemon salvaje o contra el de un entrenador o el líder de un gimnasio)
public enum BattleType
{
   WildPokemon,
   Trainer,
   Leader
}

public class BattleManager : MonoBehaviour
{
   [SerializeField] [Tooltip("Battle Unit / Unidad (pokemon) del player que participa en la batalla")]
   private BattleUnit playerUnit;

   [SerializeField] [Tooltip("Battle Unit / Unidad (pokemon) del enemigo que participa en la batalla")]
   private BattleUnit enemyUnit;

   [SerializeField] [Tooltip("Contenedor de la caja de texto del diálogo de batalla")]
   private BattleDialogBox battleDialogBox;

   [SerializeField] [Tooltip("Panel de selección de pokemon de la party del player")]
   private PartyHUD partyHUD;

   [SerializeField] [Tooltip("Panel de selección de movimiento a olvidar cuando se supere el límite ")]
   private LearnedMovesSelectionUI selectMoveUI;

   [SerializeField] [Tooltip("Prefab de la Pokeball que el player podrá lanzar")]
   private GameObject pokeBall;


   //Para establecer el tipo de batalla
   private BattleType battleType;
   
   //Para controlar el estado actual de la batalla
   private BattleState state;
   
   //Para guardar la party de pokemons del player disponible al entrar en la batalla
   private PokemonParty playerParty;
   
   //Para guardar el pokemom salvaje enemigo que aparece en la batalla
   private Pokemon wildPokemon;
   
   //Para controlar la acción seleccionada por el player en el panel de selección de acciones
   private int currentSelectedAction;
   //Para controlar que no se pueda cambiar de acción seleccionada hasta pasado un lapso aunque se mantenga pulsado
   private float timeSinceLastClick;
   [SerializeField][Tooltip("Tiempo para poder cambiar la elección en los paneles de acción, ataque, etc.")]
   private float timeBetweenClicks = 1.0f;
   
   //Para controlar el ataque seleccionado por el player en el panel de selección de ataques o movimientos
   private int currentSelectedMovement;
   
   //Para controlar, y limitar, el número de intentos de huir de una batalla por parte del player
   private int escapeAttemps;
   
   //Para controlar el pokemon seleccionado por el player en el panel de selección de pokemons de la party
   private int currentSelectedPokemon;

   //Para guardar un nuevo movimiento que el pokemon vaya a aprender
   private MoveBase moveToLearn;
   
   //Evento de la clase Action de Unity para que el GameManager conozca cuándo finaliza la batalla
   //El evento devolverá un booleano para indicar además si el player ha vencido (true) o ha perdido (false)
   public event Action<bool> OnBattleFinish;

   
   //Sonidos:
   [SerializeField] [Tooltip("Sonido que se reproducirá al atacar")]
   private AudioClip attackClip;

   [SerializeField] [Tooltip("Sonido que se reproducirá al recibir daño")]
   private AudioClip damageClip;

   [SerializeField] [Tooltip("Sonido que se reproducirá al subir de nivel")]
   private AudioClip levelUpClip;

   [SerializeField] [Tooltip("Sonido que se reproducirá al finalizar la batalla")]
   private AudioClip battleFinishClip;

   [SerializeField] [Tooltip("Sonido que se reproducirá al lanzar una pokeball")]
   private AudioClip pokeballClip;

   [SerializeField] [Tooltip("Sonido que se reproducirá cuando un pokemon es vencido")]
   private AudioClip faintedClip;

   /// <summary>
   /// Configura una nueva batalla contra un pokemon salvaje
   /// </summary>
   /// <param name="playerPokemonParty">Party de pokemons del player</param>
   /// <param name="enemyPokemon">Pokemon salvaje que aparece en la batalla</param>
   public void HandleStartBattle(PokemonParty playerPokemonParty, Pokemon enemyPokemon)
   {
      //Guarda la party del player y el pokemon enemigo que intervendrán en la batalla
      playerParty = playerPokemonParty;
      wildPokemon = enemyPokemon;
      
      //Establece el tipo de batalla
      battleType = BattleType.WildPokemon;
      
      //Reinicia el número de intentos de huir de la batalla
      escapeAttemps = 0;
      
      StartCoroutine(SetupBattle());
   }

   /// <summary>
   /// Configura una nueva batalla contra el pokemon de un entrenador o un líder de gimnasio
   /// </summary>
   /// <param name="playerPokemonParty">La party de pokemons del player</param>
   /// <param name="trainerPokemonParty">La party de pokemons del entrenador</param>
   /// <param name="isLeader">True si el entrenador es líder de gimnasio, False si no lo es</param>
   public void HandleStartTrainerBatlle(PokemonParty playerPokemonParty, PokemonParty trainerPokemonParty,
      bool isLeader)
   {
      //Establece el tipo de batalla
      battleType = (isLeader)? BattleType.Leader: BattleType.Trainer;
      
      //TODO: implementar el inicio de la batalla contra un NPC entrenador
   }
   
   /// <summary>
   /// Método que iniciará una batalla en el update cuando sea invocado desde el GameManager
   /// </summary>
   public void HandleUpdate()
   {
      //No se realizará ninguna acción nueva mientras se esté escribiendo algo en el texto de diálogo de la batalla
      if (battleDialogBox.IsWriting)
         return;
      
      //Se cambiará de acción pulsando arriba/abajo, izquierda/derecha
      //estableciendo un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      
      
      if (state == BattleState.ActionSelection)//Estado: selección de una acción por parte del player
      {
         HandlePlayerActionSelection();
      }
      else if (state == BattleState.MovementSelection)//Estado: ejecución de un ataque por parte del player
      {
         HandlePlayerMovementSelection();
      }
      else if (state == BattleState.PartySelectScreen)//Estado: en la pantalla de selección de pokemon
      {
         HandlePlayerPartySelection();
      }
      else if (state == BattleState.LoseTurn)//Estado: el player ha perdido su turno, es turno del enemigo
      {
         StartCoroutine(PerfomEnemyMovement());
      }
      else if (state == BattleState.ForgetMovement)//Estado: hay que elegir un movimiento a olvidar
      {
         //Abre la pantalla de selección, actuando en función del valor devuelto por el evento correspondiente
         selectMoveUI.HandleForgetMoveSelection((moveIndex) =>
         {
            if (moveIndex == -1)//Cuando en la pantalla de selección se haya cambiado el elemento seleccionado
            {
               timeSinceLastClick = 0;//Resetea el timer entre selecciones
            }
            else//Cuando en la pantalla de selección se haya pulsado Submit moveIndex tendrá el movimiento seleccionado
            {
               StartCoroutine(ForgetOldMove(moveIndex));
            }
         });
      }
   }

   /// <summary>
   /// Corutina que realiza la configuración de inicio de una batalla contra un pokemon salvaje
   /// </summary>
   public IEnumerator SetupBattle()
   {
      //Establece el estado inicial de la batalla
      state = BattleState.StartBattle;
      
      //Captura y configura el primer pokemon con vida de la party de pokemons del player
      playerUnit.SetupPokemon(playerParty.GetFirstNonFaintedPokemon());
      
      //Rellena también el panel de ataques con los que puede ejecutar el pokemon del player
      battleDialogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);
      
      //Inicializa el HUD de selección de pokemon de la party del player
      partyHUD.InitPartyHUD();
      
      //Configura el pokemon del enemigo
      enemyUnit.SetupPokemon(wildPokemon);

      //Muestra el primer mensaje en la caja de diálogo de la batalla, esperando hasta que finalice ese proceso
      yield return battleDialogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
         , enemyUnit.Pokemon.Base.PokemonName));

      //Inicia el ataque del player o del enemigo, dependiendo de a cuál le toque el primer turno 
      yield return ChooseFirstTurn(true);
   }


   /// <summary>
   /// Decide cuál de los Pokemon de la batalla comenzará atacando primero, comparando la velocidad de ambos
   /// </summary>
   /// <param name="showFirstMessage">Indica si se debe mostrar el mensaje inicial de la batalla</param>
   /// <returns></returns>
   private IEnumerator ChooseFirstTurn(bool showFirstMessage = false)
   {
      //Se comparan las velocidades de ambos contendientes
      //(enemyUnit, playerUnit) para decidir quién atacará primero
      if (enemyUnit.Pokemon.Speed > playerUnit.Pokemon.Speed)
      {
         //Activa/desactiva los paneles correspondientes
         battleDialogBox.ToggleDialogText(true);
         battleDialogBox.ToggleActions(false);
         battleDialogBox.ToggleMovements(false);
         //Muestra un mensaje y ejecuta la acción del enemigo
         if (showFirstMessage)
         {
            yield return battleDialogBox.SetDialog("El enemigo ataca primero");
         }

         yield return PerfomEnemyMovement();
      }
      else
      {
         PlayerActionSelection();
      }
   }

   
   /// <summary>
   /// Finaliza una batalla
   /// </summary>
   /// <param name="playerHasWon">true si el resultado de la batalla es de victoria para el player</param>
   private void BattleFinish(bool playerHasWon)
   {
      //Reproduce el sonido de fin de batalla
      SoundManager.SharedInstance.PlaySound(battleFinishClip);
      
      //Cambia el estado de la batalla
      state = BattleState.FinishBattle;

      //Transmite el evento de finalización de la batalla
      OnBattleFinish(playerHasWon);
      
      //Restablece los valores que correspondan en los pokemon del player que hayan intervenido en la batalla
      playerParty.Pokemons.ForEach(pok => pok.OnBattleFinish());
      /*Nota: La línea anterior equivale a la nomenclatura tradicional:
      foreach (var pok in playerParty.Pokemons)
      {
         pok.OnBattleFinish();
      } */
   }
   
   /// <summary>
   /// Inicia las acciones de ataque del player
   /// </summary>
   private void PlayerActionSelection()
   {
      //Establece el estado actual de la batalla
      state = BattleState.ActionSelection;

      StartCoroutine(battleDialogBox.SetDialog("Selecciona una acción..."));
      
      //Muestra/oculta los paneles correspondientes de la UI
      //Muestra el diálogo de batalla
      battleDialogBox.ToggleDialogText(true);
      //Muestra el panel de selección de acciones
      battleDialogBox.ToggleActions(true);
      //Oculta el panel de selección de ataque
      battleDialogBox.ToggleMovements(false);
      
      //Se reinicia y resalta la acción por defecto
      currentSelectedAction = 0;
      battleDialogBox.SelectAction(currentSelectedAction);
   }

   /// <summary>
   /// Responde a la acción seleccionada por el player
   /// </summary>
   private void HandlePlayerActionSelection()
   {
      if (Input.GetAxisRaw("Vertical") != 0)//Al pulsar hacia arriba/abajo
      {
         //Reinicia el tiempo desde la última selección
         timeSinceLastClick = 0;
         
         //La opción cambiará de dos en dos
         currentSelectedAction = (currentSelectedAction + 2) % 4;

         //Se resalta la acción seleccionada en el panel de la UI
         battleDialogBox.SelectAction(currentSelectedAction);
      }
      else if (Input.GetAxisRaw("Horizontal") != 0)//Al pulsar izquierda/derecha
      {
         timeSinceLastClick = 0;
         
         //Irá cambiando de columna, manteniéndose en la misma fila
         currentSelectedAction = (currentSelectedAction + 1) % 2 +
                                 2 * Mathf.FloorToInt(currentSelectedAction / 2);
         
         //Se resalta la acción seleccionada en el panel de la UI
         battleDialogBox.SelectAction(currentSelectedAction);
      }

      if (Input.GetAxisRaw("Submit") != 0)//Al pulsar el botón de acción
      {
         timeSinceLastClick = 0;

         //Ejecuta la acción seleccionada
         switch (currentSelectedAction)
         {
            case 0:
               //El player ataca
               PlayerMovementSelection();
               break;
            case 1:
               //El player escoge pokemon. Se abre la UI de selección de pokemon de la party del player
               OpenPartySelectionScreen();
               break;
            case 2:
               //TODO: El player revisa mochila. Se abre la UI del inventario del player
               OpenInventoryScreen();
               break;
            case 3:
               //El player intenta huir de la batalla.
               StartCoroutine(TryToEscapeFromBattle());
               break;
            default:
               break;
         }
      }
   }

   /// <summary>
   /// Inicializa la acción de ataque por parte del player
   /// </summary>
   private void PlayerMovementSelection()
   {
      //Cambia al estado correspondiente
      state = BattleState.MovementSelection;
      
      //Muestra/oculta los paneles correspondientes de la UI
      battleDialogBox.ToggleDialogText(false);
      battleDialogBox.ToggleActions(false);
      battleDialogBox.ToggleMovements(true);
      
      //Establece el ataque seleccionado por defecto
      currentSelectedMovement = 0;
      //Se resalta el ataque seleccionado en el panel de la UI y se actualiza su información en el HUD
      battleDialogBox.SelectMovement(currentSelectedMovement,
         playerUnit.Pokemon.Moves[currentSelectedMovement]);
   }
   
   /// <summary>
   /// Rellena y abre la interfaz de selección de pokemon para la batalla
   /// </summary>
   private void OpenPartySelectionScreen()
   {
      //Modifica el estado actual
      state = BattleState.PartySelectScreen;
      
      //Rellena el HUD de selección de pokemon con la lista de pokemon de la party del player y lo muestra
      partyHUD.SetPartyData(playerParty.Pokemons);
      partyHUD.gameObject.SetActive(true);

      //Deja seleccionado por defecto el pokemon actual en batalla
      currentSelectedPokemon = playerParty.GetPositionFromPokemon(playerUnit.Pokemon);
      partyHUD.UpdateSelectedPokemon(currentSelectedPokemon);
   }
   
   
   /// <summary>
   /// Abre la interfaz de inventario del player
   /// </summary>
   private void OpenInventoryScreen()
   {
      //TODO: pendiente de implementar el inventario del player
      Debug.Log("Inventario");
      
      //Desactiva el panel de elección de acciones
      battleDialogBox.ToggleActions(false);
      
      //El player lanza una pokeball
      StartCoroutine(ThrowPokeball());
   }
   

   /// <summary>
   /// Implementa la lógica de acción de ataque del player
   /// </summary>
   private void HandlePlayerMovementSelection()
   {
      /*Representación de las posiciones del panel en las que hay que desplazarse:
        0    1
        2    3 */
      
      if (Input.GetAxisRaw("Vertical") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;

         int oldSelectedMovement = currentSelectedMovement;//Guarda el actual movimiento
         
         //El desplazamiento en vertical cambiará la selección moviendo dos posiciones
         currentSelectedMovement = (currentSelectedMovement + 2) % 4;
         
         //Si el nuevo movimiento se "sale" de la lista de movimientos disponibles, se deja el que había
         if (currentSelectedMovement >= playerUnit.Pokemon.Moves.Count)
            currentSelectedMovement = oldSelectedMovement;
         
         //Se resalta el ataque seleccionado en el panel de la UI y se actualiza su información en el HUD
         battleDialogBox.SelectMovement(currentSelectedMovement,
            playerUnit.Pokemon.Moves[currentSelectedMovement]);
      }
      else if (Input.GetAxisRaw("Horizontal") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;
         
         int oldSelectedMovement = currentSelectedMovement;//Guarda el actual movimiento
         
         //El desplazamiento en horizontal cambia la selección moviendo una posición
         if (currentSelectedMovement <= 1)//Fila superior
         {
            currentSelectedMovement = (currentSelectedMovement + 1) % 2;
         }
         else//Fila inferior
         {
            currentSelectedMovement = (currentSelectedMovement + 1) % 2 + 2;
         }
         
         //Si el nuevo movimiento se "sale" de la lista de movimientos disponibles, se deja el que había
         if (currentSelectedMovement >= playerUnit.Pokemon.Moves.Count)
            currentSelectedMovement = oldSelectedMovement;
         
         //Se resalta el ataque seleccionado en el panel de la UI y se actualiza su información en el HUD
         battleDialogBox.SelectMovement(currentSelectedMovement,
            playerUnit.Pokemon.Moves[currentSelectedMovement]);
      }
      //Si se pulsa el botón de acción, se ejecutará el ataque seleccionado por el player
      if (Input.GetAxisRaw("Submit") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;
         //Oculta el panel de movimientos
         battleDialogBox.ToggleMovements(false);
         //Muestra el diálogo
         battleDialogBox.ToggleDialogText(true);

         StartCoroutine(PerformPlayerMovement());
      }
      
      //Si se pulsa el botón de cancelar, se regresa a la pantalla anterior (la de selección de acción)
      if (Input.GetAxisRaw("Cancel") != 0)
      {
         PlayerActionSelection();
      }
   }

   /// <summary>
   /// Ejecuta el ataque seleccionado por el player mostrando los mensajes poco a poco
   /// </summary>
   /// <returns></returns>
   private IEnumerator PerformPlayerMovement()
   {
      //Cambia de estado para que no se pueda realizar otra acción hasta finalizar el movimiento del player
      state = BattleState.PerformMovement;
      
      //Movimiento que se debe ejecutar
      Move move = playerUnit.Pokemon.Moves[currentSelectedMovement];

      if (move.Pp <= 0) //Si se han agotado los PP del ataque, no se podrá ejecutar
      {
         //Vuelve al estado de selección de movimiento del player
         PlayerMovementSelection();
         yield break;//Y sale de la corutina sin hacer nada más
      }
      
      //Ejecuta el ataque
      yield return RunMovement(playerUnit, enemyUnit, move);
      
      //Solo si el estado de la batalla se mantiene sin modificar, se da el control al enemigo
      if (state == BattleState.PerformMovement)
      {
         StartCoroutine(PerfomEnemyMovement());
      }
   }

   
   
   
   /// <summary>
   /// Implementa las acciones de ataque del enemigo
   /// </summary>
   private IEnumerator PerfomEnemyMovement()
   {
      //Cambia el estado de la batalla
      state = BattleState.PerformMovement;
      
      //El enemigo decide el ataque a ejecutar aleatoriamente
      Move move = enemyUnit.Pokemon.RandonMove();

      if (move != null)//Si al enemigo no le quedan ataques con Pp, no podrá atacar
      {
         //Ejecuta el ataque
         yield return RunMovement(enemyUnit, playerUnit, move);
      }

      //Solo si el estado de la batalla se mantiene sin modificar, se da el control al player
      if (state == BattleState.PerformMovement)
      {
         PlayerActionSelection();
      }
   }


   /// <summary>
   /// Ejecuta un movimiento de ataque
   /// </summary>
   /// <param name="attacker">Unidad de batalla atacante</param>
   /// <param name="target">Unidad de batalla defensora</param>
   /// <param name="move">Movimiento que el atacante ejecutará al defensor</param>
   /// <returns></returns>
   private IEnumerator RunMovement(BattleUnit attacker, BattleUnit target, Move move)
   {
      //Al inicio del turno se comprueba si hay algún estado alterado que impida atacar al pokemon, como
      //el de parálisis, dormido, etc.
      bool canRunMovement = attacker.Pokemon.OnStartTurn();
      if (!canRunMovement) //Si el pokemon atacante no puede atacar
      {
         //Hace una actualización de su barra de vida, porque es posible que no pueda atacar por estar afectado
         //por un efecto de estado volátil o alterado que provoca que se haga daño a sí mismo
         yield return attacker.HUD.UpdatePokemonData();
         //Muestra los mensajes correspondientes
         yield return ShowStatsMessages(attacker.Pokemon);
         //Sale sin hacer nada más
         yield break;
      }
      
      
      //Asegura de que si queda algún mensaje en cola que se deba de mostrar, lo haga ahora
      yield return ShowStatsMessages(attacker.Pokemon);
      
      
      //Reduce los puntos de poder disponibles del atacante para el movimiento que se va a ejecutar
      move.Pp--;

      //Muestra el mensaje del ataque ejecutado y espera a que finalice de ser mostrado
      yield return battleDialogBox.SetDialog(String.Format("{0} ha usado {1}",
         attacker.Pokemon.Base.PokemonName, move.Base.AttackName));

      //Reproduce las animaciones correspondientes
      yield return RunMoveAnims(attacker, target);

      //Si el ataque ejecutado es del tipo de los que modifican los estados (stats) con un boost
      if (move.Base.TypeOfMove == MoveType.Stats)
      {
         //Aplica los cambios correspondientes sobre las stats
         yield return RunMoveStats(attacker.Pokemon, target.Pokemon, move);
      }
      else //Si el ataque es de otro tipo (físico o especial)
      {
         
         //Daña al pokemon enemigo y se obtiene el resultado y si ha sido vencido
         DamageDescription damageDesc = target.Pokemon.ReceiveDamage(move, attacker.Pokemon);

         //Actualiza la información del pokemon atacado en el HUD
         yield return target.HUD.UpdatePokemonData();

         yield return ShowDamageDescription(damageDesc); //Muestra información adicional en el HUD
      }
      
      if (target.Pokemon.Hp <= 0)//Si tras el ataque el pokemon atacado es debilitado
      {
         //Implementa las acciones que suceden cuando un pokemon es vencido
         yield return HandlePokemonFainted(target);
      }

      //Tras finalizar el turno del atacante, se realizan acciones adicionales sobre él,
      //tales como aplicar los efectos de estado alterado que pudiera haber sufrido
      attacker.Pokemon.OnFinishTurn();
      //Además, se muestran los mensajes informativos de los cambios en sus stats y estado alterado sufridos en el turno
      yield return ShowStatsMessages(attacker.Pokemon);
      //Y se actualiza la información de su vida en el HUD, si se ha visto modificada
      yield return attacker.HUD.UpdatePokemonData();
      //Si tras el turno el pokemon que ataca es debilitado como consecuencia de los estados alterados anteriores
      if (attacker.Pokemon.Hp <= 0)
      {
         //Implementa las acciones que suceden cuando el pokemon atacante es vencido
         yield return HandlePokemonFainted(attacker);
      }
   }


   /// <summary>
   /// Aplica los boosts correspondientes sobre las stats del pokemon atacanta o defensor, cuando el movimiento
   /// ejecutado es de los que modifican estadísticas
   /// También aplica un estado alterado sobre el pokemon si el movimiento ejecutado lo puede producir
   /// </summary>
   /// <param name="attacker">El pokemon que ejecuta el ataque o movimiento que afecta a estadisticas</param>
   /// <param name="target">El pokemon que recibe el ataque o movimiento que afecta a estadísticas</param>
   /// <param name="move">El ataque o movimiento que afecta a estadísticas</param>
   /// <returns></returns>
   private IEnumerator RunMoveStats(Pokemon attacker, Pokemon target, Move move)
   {
      //Primero aplica los boosts en las stats
      if (move.Base.Effects.Boostings != null)//Siempre y cuando en el ataque estén definidos los boosts
      {
         //Se recorre la lista de boost que puede provocar el ataque
         foreach (var boost in move.Base.Effects.Boostings)
         {
            if (boost.target == MoveTarget.Self)//Si el boost afecta al propio pokemon que realiza el ataque
            {
               attacker.ApplyBoost(boost);
            }
            else //Si el boost afecta al pokemon que recibe el ataque
            {
               target.ApplyBoost(boost);
            }
         }
      }
      
      //Después aplica el estado alterado, si lo hubiera, sobre el pokemon que recibe el ataque
      if (move.Base.Effects.Status != StatusConditionID.none)
      {
         if (move.Base.Target == MoveTarget.Other)//Si el objetivo del ataque es el otro pokemon
         {
            target.SetConditionStatus(move.Base.Effects.Status);
         }
         else //Si el objetivo del ataque es el propio pokemon que ejecuta el ataque
         {
            attacker.SetConditionStatus(move.Base.Effects.Status);
         }
      }
      
      //También aplica el estado volátil, si lo hubiera, sobre el pokemon que recibe el ataque
      if (move.Base.Effects.VolatileStatus != StatusConditionID.none)
      {
         if (move.Base.Target == MoveTarget.Other)//Si el objetivo del ataque es el otro pokemon
         {
            target.SetVolatileConditionStatus(move.Base.Effects.VolatileStatus);
         }
         else //Si el objetivo del ataque es el propio pokemon que ejecuta el ataque
         {
            attacker.SetVolatileConditionStatus(move.Base.Effects.VolatileStatus);
         }
      }
      
      
      //Muestra los mensajes informando de los cambios en las stats y los estados que se hayan producido
      yield return ShowStatsMessages(attacker);
      yield return ShowStatsMessages(target);
   }


   /// <summary>
   /// Reproduce las animaciones de ejecutar/recibir un ataque durante la batalla
   /// </summary>
   /// <param name="attacker">Pokemon que ejecuta un ataque</param>
   /// <param name="target">Pokemon que recibe un ataque</param>
   /// <returns></returns>
   private IEnumerator RunMoveAnims(BattleUnit attacker, BattleUnit target)
   {
      //Reproduce la animación de ataque
      attacker.PlayAttackAnimation();
      //Reproduce el sonido de ataque
      SoundManager.SharedInstance.PlaySound(attackClip);

      //Hace una pausa para dejar que la animación termine
      yield return new WaitForSeconds(1f);
      
      //Reproduce la animación de recibir daño por parte del enemigo
      target.PlayReceiveAttackAnimation();
      //Reproduce el sonido de recibir daño
      SoundManager.SharedInstance.PlaySound(damageClip);
      yield return new WaitForSeconds(1f);
   }


   /// <summary>
   /// Muestra en la UI de la batalla un mensaje mostrando los cambios que se hayan producido en las stats
   /// de un pokemon a lo largo de la misma, como consecuencia de la ejecición de los diversos ataques
   /// </summary>
   /// <param name="pokemon">El pokemon del que mostrar las stats</param>
   /// <returns></returns>
   IEnumerator ShowStatsMessages(Pokemon pokemon)
   {
      //Se lee la cola de los mensajes a mostrar que tiene el pokemon, y mientras haya mensajes en la misma....
      while (pokemon.StatusChangeMessages.Count > 0)
      {
         //Guarda el primer mensaje de la cola, sacándolo de la misma
         string message = pokemon.StatusChangeMessages.Dequeue();
         //Se muestra el mensaje
         yield return battleDialogBox.SetDialog(message);
      }
   }
   
   
   /// <summary>
   /// Comprueba el resultado final de una batalla, dando la victoria al pokemon del player o al del enemigo
   /// </summary>
   /// <param name="faintedUnit">La unidad de batalla que ha sido vencida</param>
   private void CheckForBattleFinish(BattleUnit faintedUnit)
   {
      if (faintedUnit.IsPlayer)//Si el pokemon vencido es del player
      {
         //Comprueba si todavía hay algún pokemon disponible en la party de pokemons del player
         Pokemon nextPokemon = playerParty.GetFirstNonFaintedPokemon();
         if (nextPokemon != null)//Si queda algún pokemon
         {
            OpenPartySelectionScreen();//Abre la ventana de selección de pokemon
         }
         else//Si ya no quedan más pokemon
         {
            BattleFinish(false);//Finaliza la batalla con derrota del player
         }
      }
      else//Si el pokemon vencido es del enemigo
      {
         BattleFinish(true);//Finaliza la batalla con victoria del player
      }
   }
   
   
   /// <summary>
   /// Muestra en la UI un mensaje si el daño recibido es más o menos efectivo de lo normal y si ha sido crítico
   /// </summary>
   /// <param name="damageDesc">Estructura con los valores del daño recibido</param>
   /// <returns></returns>
   private IEnumerator ShowDamageDescription(DamageDescription damageDesc)
   {
      if (damageDesc.Critical > 1)
      {
         yield return battleDialogBox.SetDialog("¡Golpe crítico!");
      }

      if (damageDesc.AttackType > 1)
      {
         yield return battleDialogBox.SetDialog("¡Ataque superefectivo!");
      }
      else if (damageDesc.AttackType < 1)
      {
         yield return battleDialogBox.SetDialog("No es muy efectivo");
      }
   }

   
   /// <summary>
   /// Gestiona la selección de un pokemon de la party del player desde el HUD
   /// </summary>
   private void HandlePlayerPartySelection()
   {
      /*Representación de las posiciones del panel en las que hay que desplazarse:
        0    1
        2    3
        4    5 */
      
      if (Input.GetAxisRaw("Vertical") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;
         
         //Calcula la nueva posición teniendo en cuenta que deberá avanzar de dos en dos, sin dar la vuelta
         //(de 0 a 2, de 2 a 4, de 4 a 2 o de 2 a 0)
         currentSelectedPokemon -= (int)Input.GetAxisRaw("Vertical") * 2;

      }
      else if (Input.GetAxisRaw("Horizontal") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;

         //En horizontal solo avanzará una posición sin dar la vuelta
         currentSelectedPokemon += (int) Input.GetAxisRaw("Horizontal") * 1;
      }

      //Asegura que el valor resultante siempre queda dentro de los límites del array
      currentSelectedPokemon = Mathf.Clamp(currentSelectedPokemon, 0, playerParty.Pokemons.Count - 1);
           
      //Muestra el pokemon seleccionado con un color diferente
      partyHUD.UpdateSelectedPokemon(currentSelectedPokemon);
      
      //Si se pulsa el botón de acción, saldrá al combate el pokemon seleccionado, salvo que éste ya no tenga vida 
      if (Input.GetAxisRaw("Submit") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;

         //Guarda el pokemon seleccionado
         Pokemon selectedPokemon = playerParty.Pokemons[currentSelectedPokemon];
         //Comprueba si tiene vida, y en caso contrario muestra un mensaje informativo y sale sin hacer nada
         if (selectedPokemon.Hp <= 0)
         {
            partyHUD.SetMessage("No puedes enviar un pokemon debilitado");
            return;
         }
         else if (selectedPokemon == playerUnit.Pokemon) //Si el pokemon seleccionado es el que ya está en la batalla
         {
            partyHUD.SetMessage("El pokemon seleccionado ya está en batalla");
            return; //También sale sin hacer nada
         }

         state = BattleState.Busy;//Mientras se hace el cambio de pokemon, cambia el estado para no permitir movimientos
         
         //Desactiva el panel de elección de pokemon
         partyHUD.gameObject.SetActive(false);
         
         //Intercambia el pokemon actual por el seleccionado
         StartCoroutine(SwitchPokemon(selectedPokemon));
      }
      
      //Si se pulsa el botón de cancelar, se regresa a la pantalla anterior (la de selección de acción)
      if (Input.GetAxisRaw("Cancel") != 0)
      {
         partyHUD.gameObject.SetActive(false);
         PlayerActionSelection();
      }
   }
   

   /// <summary>
   /// Cambia el pokemon actual en batalla por el indicado como parámetro
   /// </summary>
   /// <param name="newPokemon">El pokemon que debe entrar en la batalla</param>
   /// <returns></returns>
   private IEnumerator SwitchPokemon(Pokemon newPokemon)
   {
      //¿El pokemon que sale de la batalla ha sido vencido? (por defecto, sí)
      bool currentPokemonFainted = true;
      
      //Se comprueba si el pokemon de la batalla todavía no había sido vencido (aún tiene vida)
      if (playerUnit.Pokemon.Hp > 0)
      {
         currentPokemonFainted = false;
      }

      string switchMessage;
      //Nota: este mensaje solo se mostrará si el pokemon actual que va a salir de la batalla todavía tenía vida:
      if (!currentPokemonFainted)
      {
         switchMessage = String.Format("¡Vuelve, {0}!", playerUnit.Pokemon.Base.PokemonName);
         yield return battleDialogBox.SetDialog(switchMessage);
      }

      //Reproduce la animación de retirada del pokemon actual
      playerUnit.PlayFaintAnimation();

      //Espera un instante para que finalice la animación
      yield return new WaitForSeconds(1.5f);
      
      //Configura el pokemon que se incorpora a la batalla
      playerUnit.SetupPokemon(newPokemon);

      //Se actualizan los movimientos
      battleDialogBox.SetPokemonMovements(newPokemon.Moves);

      //Muestra el mensaje de entrada del nuevo pokemon
      switchMessage = String.Format("¡Adelante, {0}!", newPokemon.Base.PokemonName);
      yield return battleDialogBox.SetDialog(switchMessage);
      
      //Si el pokemon que sale de la batalla había sido vencido, se decide si al salir el nuevo
      //le corresponde a él atacar primero, o si le corresponde al pokemon enemigo
      if (currentPokemonFainted)
      {
         yield return ChooseFirstTurn();
      }
      else //Si el pokemon retirado aún tenía vida, siempre le tocará atacar al pokemon enemigo
      {
         yield return PerfomEnemyMovement();
      }
   }

   /// <summary>
   /// Ejecuta la acción de lanzar una pokeball por parte del player para atrapar al pokemon enemigo
   /// </summary>
   /// <returns></returns>
   private IEnumerator ThrowPokeball()
   {
      //Cambia el estado actual
      state = BattleState.Busy;

      //Si se ha lanzado la pokeball contra un pokemon de otro entrenador, no será posible y se perderá el turno
      if (battleType != BattleType.WildPokemon)
      {
         battleDialogBox.SetDialog("¡No puedes robar los pokemon de otros entrenadores!");
         state = BattleState.LoseTurn;
         yield break;
      }

      //Si el pokemon era salvaje, se continuará con el intento de captura
      
      yield return battleDialogBox.SetDialog($"¡Has lanzado una {pokeBall.gameObject.name}!");

      //Reproduce el sonido de lanzar una pokeball
      SoundManager.SharedInstance.PlaySound(pokeballClip);
      //Instancia la pokeball un poco a la izquierda/abajo del pokemon del player
      GameObject pokeballInst = Instantiate(pokeBall,
         playerUnit.transform.position - new Vector3(2, 2, 0), Quaternion.identity);
      
      //Comienza la animación del movimiento parabólico de la pokeball hacia el pokemon enemigo:
      //Captura el sprite de la pokeball
      SpriteRenderer pokeballSpt = pokeballInst.GetComponent<SpriteRenderer>();
      //Utiliza la librería del paquete de la Asset Store "DOTween (HOTween v2)" para la animación de lanzamiento
      yield return pokeballSpt.transform.DOLocalJump(enemyUnit.transform.position + new Vector3(0, 1.5f, 0),
         2f, 1, 1f).WaitForCompletion();
      
      //Se reproduce la animación del pokemon siendo absorbido (capturado) por la pokeball
      yield return enemyUnit.PlayCapturedAnimation();
      
      //La pokeball cae hacia el suelo
      yield return pokeballSpt.transform.DOLocalMoveY(enemyUnit.transform.position.y - 1.5f,
         0.3f).WaitForCompletion();
      
      //Se calcula el número de sacudidas que va a realizar la pokeball en el intento de capturar el pokemon
      int numberOfShakes = TryToCatchPokemon(enemyUnit.Pokemon);

      //Reproduce la animación de las sacudidas, limitándolas a máximo de 3 aunque se hayan obtenido 4 sacudidas
      for (int i = 0; i < Mathf.Min(numberOfShakes, 3); i++)
      {
         yield return new WaitForSeconds(0.5f);
         yield return pokeballSpt.transform.DOPunchRotation(new Vector3(0, 0, 15f),
            0.6f).WaitForCompletion();
      }
      
      //Cuando el número de sacudidas resultante sea 4, significará que el pokemon ha sido capturado
      if (numberOfShakes == 4)
      {
         yield return battleDialogBox.SetDialog($"¡{enemyUnit.Pokemon.Base.name} capturado!");
         
         //La pokeball desaparece
         yield return pokeballSpt.DOFade(0, 1.5f).WaitForCompletion();
         Destroy(pokeballInst);
         
         //Intenta añadir el pokemon capturado a la party del player
         if (playerParty.AddPokemonToParty(enemyUnit.Pokemon))
         {
            yield return battleDialogBox.SetDialog($"{enemyUnit.Pokemon.Base.name} se ha añadido a tu party");
         }
         else
         {
            yield return battleDialogBox.SetDialog($"{enemyUnit.Pokemon.Base.name} no cabe en tu party");
         }

         yield return new WaitForSeconds(0.5f);//Pequeña pausa

         //Finaliza la batalla con resultado de victoria del player
         BattleFinish(true);
      }
      else //Si el pokemon no ha sido capturado
      {
         //El pokemon escapa. Hace una pequeña pausa
         yield return new WaitForSeconds(0.5f);
         
         //La pokeball desaparece y el pokemon enemigo vuelve a reaparecer
         yield return pokeballSpt.DOFade(0, 0.2f).WaitForCompletion();
         Destroy(pokeballInst);
         yield return enemyUnit.PlayBreakOutAnimation();
         
         //Muestra un mensaje, diferente según lo "cerca" que ha estado de ser atrapado
         if (numberOfShakes < 2)
         {
            yield return battleDialogBox.SetDialog($"¡{enemyUnit.Pokemon.Base.name} ha escapado!");
         }
         else
         {
            yield return battleDialogBox.SetDialog("¡Casi lo consigues!");
         }
         
         //La batalla entra en el estado de pérdida de turno del player, la batalla pasa a ser control del enemigo
         state = BattleState.LoseTurn;
      }
   }

   /// <summary>
   /// Implementa el "intento" de capturar un pokemon. Hará uso de la fórmula que se puede consultar en
   /// https://bulbapedia.bulbagarden.net/wiki/Catch_rate 
   /// </summary>
   /// <param name="pokemon">El pokemon a intentar capturar</param>
   /// /// <returns>El número de shakes o sacudidas que realizará la pokeball</returns>
   private int TryToCatchPokemon(Pokemon pokemon)
   {
      //Estas dos variables se dejan en 1, pero listo para futuras implementaciones de un bonus según el tipo
      //de pokeball y otro según el estado actual del pokemon que se intenta capturar
      float bonusPokeball = 1;//TODO: clase pokeball con su multiplicador
      float bonusStat = 1;//TODO: stats para chequear condición de bonificación
      
      //Se obtiene el primer valor de la fórmula
      float a = (3 * pokemon.MaxHP - 2 * pokemon.Hp) * pokemon.Base.CatchRate * bonusPokeball * bonusStat /
                (3 * pokemon.MaxHP);
      
      //Si este valor a alcanza o supera los 255, se devolverá el número máximo de shakes (4): captura inmediata
      if (a >= 255)
      {
         return 4;
      }
      else//En caso contrario se hace un cálculo (aleatorio) de shakes:
      {
         //Se calcula el segundo número de la fórmula, una probabilidad del shake
         float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

         //Se hacen 4 comparaciones entre un número aleatorio de 0 a 65535 y b. Por cada comparación que resulte
         //exitosa (cuando el número aleatorio sea menor que b) se aumentará en 1 el número de shakes
         int shakeCount = 0;
         while (shakeCount < 4)
         {
            if (Random.Range(0, 65535) >= b)
            {
               break;
            }
            else
            {
               shakeCount++;
            }
         }
         //Se devuelve el número de shakes obtenido
         return shakeCount;
      }
   }

   
   /// <summary>
   /// Implementa el intento de huir de una batalla
   /// </summary>
   private IEnumerator TryToEscapeFromBattle()
   {
      //Cambia el estado de la batalla para evitar otras acciones mientras se ejecuta ésta
      state = BattleState.Busy;
      
      //No será posible escapar de batallas contra pokemon de un entrenador o líder
      if (battleType != BattleType.WildPokemon)
      {
         yield return battleDialogBox.SetDialog("¡No puedes huir de este combate!");
         state = BattleState.LoseTurn;//Pierde el turno
         yield break;//Sale sin hacer más
      }
      
      //Si la batalla es contra un pokemon salvaje, se calcula la probabilidad de huir de la misma
      //Utilizaremos la fórmula que se puede consultar en bulbapedia.bulbagarden.net
      //Variables necesarias:
      int playerSpeed = playerUnit.Pokemon.Speed;//Velocidad del player
      int enemySpeed = enemyUnit.Pokemon.Speed;//Velocidad del enemigo
      
      //Se aumenta el número de intentos de huida que ha realizado el player
      escapeAttemps++;

      //Si la velocidad del player es >= que la del enemigo, siempre podrá escapar de la batalla
      if (playerSpeed >= enemySpeed)
      {
         yield return battleDialogBox.SetDialog("Has escapado con éxito");
         yield return new WaitForSeconds(1f);
         BattleFinish(true);
      }
      else//En caso contrario, se calcula la probabilidad de huida
      {
         int oddsEscape = (Mathf.FloorToInt(playerSpeed * 128 / enemySpeed) + 30 * escapeAttemps) % 256;
         //Y se calcula si se ha logrado huir
         if (Random.Range(0, 256) < oddsEscape)
         {
            yield return battleDialogBox.SetDialog("Has escapado con éxito");
            yield return new WaitForSeconds(1f);
            BattleFinish(true);
         }
         else
         {
            yield return battleDialogBox.SetDialog("No has logrado huir de la batalla");
            yield return new WaitForSeconds(0.5f);
            //El player pierde el turno
            state = BattleState.LoseTurn;
         }
      }
   }
   
   /// <summary>
   /// Implementa las acciones que sucederán cuando un pokemon haya sido debilitado
   /// </summary>
   /// <param name="faintedUnit">El pokemon que ha sido vencido</param>
   /// <returns></returns>
   private IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
   {
      //El atacante vence
      yield return battleDialogBox.SetDialog(String.Format("{0} se ha debilitado",
         faintedUnit.Pokemon.Base.PokemonName));
         
      //Reproduce el sonido de derrota
      SoundManager.SharedInstance.PlaySound(faintedClip);
      //Reproduce la animación de derrota del pokemon
      faintedUnit.PlayFaintAnimation();
         
      //Espera un instante para dejar que se reproduzca la animación y el sonido
      yield return new WaitForSeconds(1.5f);
      
      //Cuando el pokemon debilitado es de un enemigo, el del player deberá ganar experiencia
      if (!faintedUnit.IsPlayer)
      {
         //Experiencia base que da el pokemon vencido
         int expBase = faintedUnit.Pokemon.Base.ExperienceBase;
         //Nivel del pokemon vencido
         int level = faintedUnit.Pokemon.Level;
         //Si el pokemon vencido fuera el de un entrenador, dará 1.5 veces más experiencia
         float multiplier = (battleType == BattleType.WildPokemon) ? 1 : 1.5f;
         
         //Experiencia total que se gana (fórmula de referencia de la Bulbapedia)
         int wonExp = Mathf.FloorToInt((expBase * level * multiplier) / 7);
         
         //Suma la experiencia obtenida al pokemon del player mostrando el texto informativo
         playerUnit.Pokemon.Experience += wonExp;
         yield return battleDialogBox.SetDialog(
            $"¡{playerUnit.Pokemon.Base.PokemonName} ha obtenido {wonExp} puntos de experiencia!");
         yield return new WaitForSeconds(0.5f);
         
         //Actualiza la experiencia en la barra de experiencia del HUD
         yield return playerUnit.HUD.SetExpSmooth();
         yield return new WaitForSeconds(1f);
         
         //Comprueba también si con la nueva cantidad de experiencia el pokemon sube de nivel
         //Como podría necesitar subir más de un nivel de golpe si ha conseguido suficiente experiencia,
         //por haber vencido a un enemigo muy superior, se usa el while en vez de una simple instrucción if
         while(playerUnit.Pokemon.NeedsToLevelUp())//Si el pokemon sube de nivel
         {
            //Reproduce el sonido de subir nivel
            SoundManager.SharedInstance.PlaySound(levelUpClip);
            //Espera para que se puede reproducir el sonido totalmente
            yield return new WaitForSeconds(2.5f);
            //Actualiza la información en el HUD
            playerUnit.HUD.SetLevelText();
            playerUnit.Pokemon.HasHPChanged = true;//Marca que la vida del pokemon ha sido cambiada al subir de nivel
            yield return playerUnit.HUD.UpdatePokemonData();
            yield return battleDialogBox.SetDialog($"{playerUnit.Pokemon.Base.PokemonName} sube de nivel");
            
            //Se comprueba si el pokemon puede aprender un nuevo movimiento al nuevo nivel
            LearnableMove newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrentLevel();
            if (newMove != null)
            {
               //Si no se ha superado el número máximo de movimientos aprendidos
               if (playerUnit.Pokemon.Moves.Count < 
                   PokemonBase.NUMBER_OF_LEARNABLE_MOVES)
               {
                  //Aprende el nuevo movimiento mostrando un mensaje informativo
                  playerUnit.Pokemon.LearnMove(newMove);
                  yield return battleDialogBox.SetDialog($"{playerUnit.Pokemon.Base.PokemonName}" +
                                                         $" ha aprendido {newMove.Move.AttackName}");
                  //Actualiza la lista de movimientos en la UI de la batalla
                  battleDialogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);
               }
               else //Si ya se ha superado el número máximo de movimientos aprendidos
               {
                  //Debe olvidar uno de los movimientos aprendidos para hacer espacio al nuevo
                  yield return battleDialogBox.SetDialog($"{playerUnit.Pokemon.Base.PokemonName}" +
                                                         $" intenta aprender " +
                                                         $"{newMove.Move.AttackName}");
                  yield return battleDialogBox.SetDialog($"Pero no puede aprender más de " +
                                                         $"{PokemonBase.NUMBER_OF_LEARNABLE_MOVES} movimientos");
                  yield return ChooseMovementToForget(playerUnit.Pokemon, newMove.Move);
                  
                  //(ChooseMovementToForget establece el estado a ForgetMovement)
                  //Indica que se debe esperar hasta que el estado del juego salga de ForgetMovement
                  yield return new WaitUntil(() => state != BattleState.ForgetMovement);
               }
            }
            
            //Actualiza la barra de experiencia, reseteándola a 0 pues al subir de nivel ya está al máximo
            yield return playerUnit.HUD.SetExpSmooth(true);
         }
      }
         
      //Finalizado, se comprueba el resultado final de la batalla
      CheckForBattleFinish(faintedUnit);
   }

   /// <summary>
   /// Lógica de elección del movimiento a olvidar cuando se ha superado el límite de movimientos máximos
   /// </summary>
   /// <param name="learner">El pokemon que debe olvidar uno de los movimientos</param>
   /// <param name="newMove">El nuevo movimiento que puede aprender tras haber subido de nivel</param>
   /// <returns></returns>
   private IEnumerator ChooseMovementToForget(Pokemon learner, MoveBase newMove)
   {
      //Cambia el estado de la batalla para que no se puedan realizar otras acciones durante este proceso
      state = BattleState.Busy;

      //Muestra la UI de selección con los movimientos ya aprendidos y el nuevo movimiento aprendible
      yield return battleDialogBox.SetDialog("Selecciona el movimiento que quieres olvidar");
      selectMoveUI.gameObject.SetActive(true);

      /*Alternativa a la siguiente instrucción sin funciones Lambda:
      List<MoveBase> moveBases = new List<MoveBase>();
      foreach (Move mv in learner.Moves)
      {
         moveBases.Add(mv.Base);
      }
      selectMoveUI.SetMovements(moveBases, newMove);
         */
      //Rellena la lista de movimientos
      selectMoveUI.SetMovements(learner.Moves.Select(mv => mv.Base).ToList(), newMove);
      
      //Guarda el nuevo movimiento que puede aprenderse en la variable global que se maneja en HandleUpdate
      moveToLearn = newMove;
      
      //Ahora ya cambia al estado de selección del movimiento a olvidar para que HandleUpdate actúe en consonancia
      state = BattleState.ForgetMovement;

   }


   /// <summary>
   /// Implementa en una corutina la lógica de olvidar un movimiento (y sustituirlo por el nuevo aprendido si procede)
   /// </summary>
   /// <param name="moveIndex">Índice del movimiento a olvidar</param>
   /// <returns></returns>
   private IEnumerator ForgetOldMove(int moveIndex)
   {
      selectMoveUI.gameObject.SetActive(false);//Desactiva la UI de selección
      if (moveIndex == PokemonBase.NUMBER_OF_LEARNABLE_MOVES)
      {
         //Se olvida el nuevo movimiento adquirido, quedando los que ya se conocían
         yield return StartCoroutine(battleDialogBox.SetDialog($"{playerUnit.Pokemon.Base.PokemonName}" +
                                                  $" no ha aprendido {moveToLearn.AttackName}"));
      }
      else
      {
         //Se olvida el movimiento seleccionado y se aprende el nuevo movimiento:
         //Guarda temporalmente el movimiento que se va a olvidar
         var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
         yield return StartCoroutine(battleDialogBox.SetDialog($"{playerUnit.Pokemon.Base.PokemonName}" +
                                                               $" olvida {selectedMove.AttackName}" +
                                                               $" y aprende {moveToLearn.AttackName}"));
         //Se sustituye el movimiento que se olvida por el nuevo
         playerUnit.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
      }
      
      //Al final se resetea el nuevo movimiento a aprender para liberar referencias en memoria
      moveToLearn = null;
      //Cambia el estado de la batalla
      state = BattleState.FinishBattle;
      //TODO: revisar cuando haya entrenadores
   }
   
   
   
   
}
