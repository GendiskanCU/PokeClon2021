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
   PlayerSelectAction,//El player tiene que hacer la selección de movimiento
   PlayerSelectMove,//Se ejecuta el movimiento del player
   EnemyMove,//Se ejecuta el movimiento del enemigo
   Busy,//No se puede hacer nada
   PartySelectScreen//En la pantalla de selección de pokemon de la party
}

public class BattleManager : MonoBehaviour
{
   [SerializeField] [Tooltip("Battle Unit / Unidad (pokemon) del player que participa en la batalla")]
   private BattleUnit playerUnit;
   
   [SerializeField] [Tooltip("Battle HUD del pokemon del player")]
   private BattleHUD playerHUD;
   
   [SerializeField] [Tooltip("Battle Unit / Unidad (pokemon) del enemigo que participa en la batalla")]
   private BattleUnit enemyUnit;
   
   [SerializeField] [Tooltip("Battle HUD del pokemon del enemigo")]
   private BattleHUD enemyHUD;

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
      
      if (state == BattleState.PlayerSelectAction)//Estado: selección de una acción por parte del player
      {
         HandlePlayerActionSelection();
      }
      else if (state == BattleState.PlayerSelectMove)//Estado: ejecución de un ataque por parte del player
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
      
      //Configura el HUD del player
      playerHUD.SetPokemonData(playerUnit.Pokemon);
      //Rellena también el panel de ataques con los que puede ejecutar el pokemon del player
      battleDialogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);
      
      //Inicializa el HUD de selección de pokemon de la party del player
      partyHUD.InitPartyHUD();
      
      //Configura el pokemon del enemigo
      enemyUnit.SetupPokemon(wildPokemon);
      
      //Configura el HUD del enemigo
      enemyHUD.SetPokemonData(enemyUnit.Pokemon);
      
      //Muestra el primer mensaje en la caja de diálogo de la batalla, esperando hasta que finalice ese proceso
      yield return battleDialogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
         , enemyUnit.Pokemon.Base.PokemonName));

      //Inicia las acciones del player o del enemigo. Se comparan las velocidades de ambos contendientes
      //(enemyUnit, playerUnit) para decidir quién atacará primero
      if (enemyUnit.Pokemon.Speed > playerUnit.Pokemon.Speed)
      {
         yield return StartCoroutine(battleDialogBox.SetDialog("El enemigo ataca primero"));
         StartCoroutine(EnemyAction());
      }
      else
      {
         PlayerAction();
      }
      
   }

   
   /// <summary>
   /// Inicia las acciones de ataque del player
   /// </summary>
   private void PlayerAction()
   {
      //Establece el estado actual de la batalla
      state = BattleState.PlayerSelectAction;

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
               PlayerMovement();
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
   private void PlayerMovement()
   {
      //Cambia al estado correspondiente
      state = BattleState.PlayerSelectMove;
      
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
         PlayerAction();
      }
   }

   /// <summary>
   /// Ejecuta el ataque seleccionado por el player mostrando los mensajes poco a poco
   /// </summary>
   /// <returns></returns>
   private IEnumerator PerformPlayerMovement()
   {
      //Movimiento que se debe ejecutar
      Move move = playerUnit.Pokemon.Moves[currentSelectedMovement];
      
      //Reduce los puntos de poder disponibles para el movimiento que se va a ejecutar
      move.Pp--;

      //Muestra el mensaje del ataque ejecutado y espera a que finalice de ser mostrado
      yield return battleDialogBox.SetDialog(String.Format("{0} ha usado {1}",
         playerUnit.Pokemon.Base.PokemonName, move.Base.AttackName));
      
      //Guarda la vida del pokemon enemigo antes de ser atacado
      int oldHPValue = enemyUnit.Pokemon.Hp;
      
      //Reproduce la animación de ataque
      playerUnit.PlayAttackAnimation();

      //Hace una pausa para dejar que la animación termine
      yield return new WaitForSeconds(1f);
      
      //Reproduce la animación de recibir daño por parte del enemigo
      enemyUnit.PlayReceiveAttackAnimation();
      
      //Daña al pokemon enemigo y se obtiene el resultado y si ha sido vencido
      DamageDescription damageDesc = enemyUnit.Pokemon.ReceiveDamage(move, playerUnit.Pokemon);
      
      enemyHUD.UpdatePokemonData(oldHPValue);//Actualiza la información de la vida en el HUD

      yield return ShowDamageDescription(damageDesc);//Muestra información adiciional en el HUD

      if (damageDesc.Fainted)
      {
         //El player vence
         yield return battleDialogBox.SetDialog(String.Format("{0} se ha debilitado",
            enemyUnit.Pokemon.Base.PokemonName));
         
         //Reproduce la animación de derrota del enemigo
         enemyUnit.PlayLoseAnimation();
         
         //Espera un instante para dejar que se reproduzca la animación
         yield return new WaitForSeconds(1.5f);
         
         //Lanza el evento de finalización de la batalla con el resultado de victoria del player(true)
         OnBattleFinish(true);
      }
      else
      {
         //El enemigo sobrevive y lanza su ataque  
         StartCoroutine(EnemyAction());
      }
   }

   
   
   
   /// <summary>
   /// Implementa las acciones de ataque del enemigo
   /// </summary>
   private IEnumerator EnemyAction()
   {
      state = BattleState.EnemyMove;
      
      //El enemigo decide el ataque a ejecutar aleatoriamente
      Move move = enemyUnit.Pokemon.RandoMove();
      
      //Reduce los puntos de poder disponibles para el movimiento que va a ejecutar
      move.Pp--;
      
      //Muestra el movimiento en pantalla
      yield return battleDialogBox.SetDialog(String.Format("{0} ha usado {1}",
         enemyUnit.Pokemon.Base.PokemonName, move.Base.AttackName));
      
      //Guarda la vida del pokemon de player antes de ser atacado
      int oldHPValue = playerUnit.Pokemon.Hp;
      
      //Reproduce la animación de ataque
      enemyUnit.PlayAttackAnimation();

      //Hace una pausa para dejar que la animación termine
      yield return new WaitForSeconds(1f);
      
      //Reproduce la animación de recibir daño por parte del player
      playerUnit.PlayReceiveAttackAnimation();
      
      //El ataque produce daño al pokemon del player y se obtiene el resultado
      DamageDescription damageDesc = playerUnit.Pokemon.ReceiveDamage(move, enemyUnit.Pokemon);
      
      playerHUD.UpdatePokemonData(oldHPValue);//Actualiza la información de la vida en el HUD
      
      yield return ShowDamageDescription(damageDesc);//Muestra información adiciional en el HUD
      
      if (damageDesc.Fainted)//Si el pokemon del player ha sido vencido
      {
         yield return battleDialogBox.SetDialog(String.Format("{0} ha sido debilitado",
            playerUnit.Pokemon.Base.PokemonName));
         
         //Reproduce la animación de derrota del player
         playerUnit.PlayLoseAnimation();
         
         //Espera un instante para dejar que se reproduzca la animación
         yield return new WaitForSeconds(1.5f);
         
         //Comprueba si en la party de pokemons del player quedan más pokemon con vida
         Pokemon nextPokemon = playerParty.GetFirstNonFaintedPokemon();
         
         //Si no quedan, lanza el evento de finalización de la batalla con el resultado de derrota del player(false)
         if (nextPokemon == null)
         {
            OnBattleFinish(false);
         }
         else//Si queda algún pokemon con vida en la party, se abre la pantalla de selección de pokemons de la party
         {
            OpenPartySelectionScreen();
         }
      }
      else//En caso contrario, el player, vuelve a escoger acción a realizar
      {
         PlayerAction();
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
      for (int i = 0; i < playerParty.Pokemons.Count; i++)
      {
         if (playerUnit.Pokemon == playerParty.Pokemons[i])
            currentSelectedPokemon = i;
      }
      partyHUD.UpdateSelectedPokemon(currentSelectedPokemon);
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
         PlayerAction();
      }
   }
   

   /// <summary>
   /// Abre la interfaz de inventario del player
   /// </summary>
   private void OpenInventoryScreen()
   {
      //TODO: pendiente de implementar el inventario del player
      Debug.Log("Inventario");
      
      //Si se pulsa Cancelar se regresa a la pantalla de selección de acción del player
      if (Input.GetAxisRaw("Cancel") != 0)
      {
         PlayerAction();
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
      
      //Se actualiza el HUD
      playerHUD.SetPokemonData(newPokemon);
      
      //Se actualizan los movimientos
      battleDialogBox.SetPokemonMovements(newPokemon.Moves);

      //Muestra el mensaje de entrada del nuevo pokemon
      switchMessage = String.Format("¡Adelante, {0}!", newPokemon.Base.PokemonName);
      yield return battleDialogBox.SetDialog(switchMessage);
      
      //Y le tocará atacar al enemigo
      StartCoroutine(EnemyAction());
   }
 
}
