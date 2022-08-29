using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;


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
   FinishBattle//La batalla ha finalizado
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
   
   //Para controlar el pokemon seleccionado por el player en el panel de selección de pokemons de la party
   private int currentSelectedPokemon;
   
   //Evento de la clase Action de Unity para que el GameManager conozca cuándo finaliza la batalla
   //El evento devolverá un booleano para indicar además si el player ha vencido (true) o ha perdido (false)
   public event Action<bool> OnBattleFinish;


   /// <summary>
   /// Configura una nueva batalla pokemon
   /// </summary>
   /// <param name="playerPokemonParty">Party de pokemons del player</param>
   /// <param name="enemyPokemon">Pokemon salvaje que aparece en la batalla</param>
   public void HandleStartBattle(PokemonParty playerPokemonParty, Pokemon enemyPokemon)
   {
      //Guarda la party del player y el pokemon enemigo que intervendrán en la batalla
      playerParty = playerPokemonParty;
      wildPokemon = enemyPokemon;
      
      StartCoroutine(SetupBattle());
   }

   
   /// <summary>
   /// Método que iniciará una batalla en el update cuando sea invocado desde el GameManager
   /// </summary>
   public void HandleUpdate()
   {
      //No se realizará ninguna acción nueva mientras se esté escribiendo algo en el texto de diálogo de la batalla
      if (battleDialogBox.IsWriting)
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
   }

   /// <summary>
   /// Corutina que realiza la configuración de inicio de una batalla
   /// </summary>
   public IEnumerator SetupBattle()
   {
      //Establece el estado inicial de la batalla
      state = BattleState.StartBattle;
      
      //Configura el primer pokemon con vida de la party de pokemons del player
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

      //Inicia las acciones del player o del enemigo. Se comparan las velocidades de ambos contendientes
      //(enemyUnit, playerUnit) para decidir quién atacará primero
      if (enemyUnit.Pokemon.Speed > playerUnit.Pokemon.Speed)//Comienza el enemigo
      {
         //Activa/desactiva los paneles correspondientes
         battleDialogBox.ToggleDialogText(true);
         battleDialogBox.ToggleActions(false);
         battleDialogBox.ToggleMovements(false);
         //Muestra un mensaje y ejecuta la acción del enemigo
         yield return battleDialogBox.SetDialog("El enemigo ataca primero");
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
      //Cambia el estado de la batalla
      state = BattleState.FinishBattle;

      //Transmite el evento de finalización de la batalla
      OnBattleFinish(playerHasWon);
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
      //Se cambiará de acción pulsando arriba/abajo, izquierda/derecha
      //estableciendo un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      
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
               //El player revisa mochila. Se abre la UI del inventario del player
               OpenInventoryScreen();
               break;
            case 3:
               //El player huye. Se activa el evento de final de batalla con el resultado de derrota
               OnBattleFinish(false);
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
   }
   

   /// <summary>
   /// Implementa la lógica de acción de ataque del player
   /// </summary>
   private void HandlePlayerMovementSelection()
   {
      //Se cambiará el ataque seleccionado presionando arriba/abajo derecha/izquierda,
      //estableciendo un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      
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
      //Reduce los puntos de poder disponibles del atacante para el movimiento que se va a ejecutar
      move.Pp--;

      //Muestra el mensaje del ataque ejecutado y espera a que finalice de ser mostrado
      yield return battleDialogBox.SetDialog(String.Format("{0} ha usado {1}",
         attacker.Pokemon.Base.PokemonName, move.Base.AttackName));
      
      //Guarda la vida del pokemon defensor antes de ser atacado
      int oldHPValue = target.Pokemon.Hp;
      
      //Reproduce la animación de ataque
      attacker.PlayAttackAnimation();

      //Hace una pausa para dejar que la animación termine
      yield return new WaitForSeconds(1f);
      
      //Reproduce la animación de recibir daño por parte del enemigo
      target.PlayReceiveAttackAnimation();
      
      //Daña al pokemon enemigo y se obtiene el resultado y si ha sido vencido
      DamageDescription damageDesc = target.Pokemon.ReceiveDamage(move, attacker.Pokemon);
      
      //Actualiza la información del pokemon atacado en el HUD
      yield return target.HUD.UpdatePokemonData(oldHPValue);

      yield return ShowDamageDescription(damageDesc);//Muestra información adicional en el HUD

      if (damageDesc.Fainted)
      {
         //El atacante vence
         yield return battleDialogBox.SetDialog(String.Format("{0} se ha debilitado",
            target.Pokemon.Base.PokemonName));
         
         //Reproduce la animación de derrota del defensor
         target.PlayLoseAnimation();
         
         //Espera un instante para dejar que se reproduzca la animación
         yield return new WaitForSeconds(1.5f);
         
         //Comprueba el resultado de la batalla
         CheckForBattleFinish(target);
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
      //Se cambiará el pokemon seleccionado presionando arriba/abajo derecha/izquierda,
      //estableciendo un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      
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
      string switchMessage = String.Format("¡Vuelve, {0}!", playerUnit.Pokemon.Base.PokemonName);
      //Este mensaje solo se mostrará si el pokemon actual todavía tiene vida:
      if(playerUnit.Pokemon.Hp > 0)
         yield return battleDialogBox.SetDialog(switchMessage);
      
      //Reproduce la animación de retirada del pokemon actual
      playerUnit.PlayLoseAnimation();

      //Espera un instante para que finalice la animación
      yield return new WaitForSeconds(1.5f);
      
      //Configura el pokemon que se incorpora a la batalla
      playerUnit.SetupPokemon(newPokemon);

      //Se actualizan los movimientos
      battleDialogBox.SetPokemonMovements(newPokemon.Moves);

      //Muestra el mensaje de entrada del nuevo pokemon
      switchMessage = String.Format("¡Adelante, {0}!", newPokemon.Base.PokemonName);
      yield return battleDialogBox.SetDialog(switchMessage);
      
      //Y le tocará atacar al enemigo
      StartCoroutine(PerfomEnemyMovement());
   }
 
}
