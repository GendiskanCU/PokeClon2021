using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


//GESTIONA LA BATALLA POKEMON PARA EL PLAYER O EL ENEMIGO

//Estados de la batalla
public enum BattleState
{
   StartBattle,//Inicio de la batalla
   PlayerSelectAction,//El player tiene que hacer la selección de movimiento
   PlayerMove,//Se ejecuta el movimiento del player
   EnemyMove,//Se ejecuta el movimiento del enemigo
   Busy,//No se puede hacer nada
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
   private BattleDialogBox battleDialogogBox;


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
   [SerializeField][Tooltip("Tiempo para poder cambiar la elección en los paneles de acción y ataque")]
   private float timeBetweenClicks = 1.0f;
   
   //Para controlar el ataque seleccionado por el player en el panel de selección de ataques o movimientos
   private int currentSelectedMovement;
   
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
      if (battleDialogogBox.IsWriting)
         return;
      
      if (state == BattleState.PlayerSelectAction)//Estado: selección de una acción por parte del player
      {
         HandlePlayerActionSelection();
      }
      else if (state == BattleState.PlayerMove)//Estado: ejecución de un ataque por parte del player
      {
         HandlePlayerMovementSelection();
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
      battleDialogogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);
      
      //Configura el pokemon del enemigo
      enemyUnit.SetupPokemon(wildPokemon);
      
      //Configura el HUD del enemigo
      enemyHUD.SetPokemonData(enemyUnit.Pokemon);
      
      //Muestra el primer mensaje en la caja de diálogo de la batalla, esperando hasta que finalice ese proceso
      yield return battleDialogogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
         , enemyUnit.Pokemon.Base.PokemonName));

      //Inicia las acciones del player o del enemigo. Se comparan las velocidades de ambos contendientes
      //(enemyUnit, playerUnit) para decidir quién atacará primero
      if (enemyUnit.Pokemon.Speed > playerUnit.Pokemon.Speed)
      {
         yield return StartCoroutine(battleDialogogBox.SetDialog("El enemigo ataca primero"));
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

      StartCoroutine(battleDialogogBox.SetDialog("Selecciona una acción..."));
      
      //Muestra/oculta los paneles correspondientes de la UI
      //Muestra el diálogo de batalla
      battleDialogogBox.ToggleDialogText(true);
      //Muestra el panel de selección de acciones
      battleDialogogBox.ToggleActions(true);
      //Oculta el panel de selección de ataque
      battleDialogogBox.ToggleMovements(false);
      
      //Se reinicia y resalta la acción por defecto
      currentSelectedAction = 0;
      battleDialogogBox.SelectAction(currentSelectedAction);
   }

   /// <summary>
   /// Responde a la acción seleccionada por el player
   /// </summary>
   private void HandlePlayerActionSelection()
   {
      //Se cambiará de acción pulsando arriba/abajo, estableciendo un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      
      if (Input.GetAxisRaw("Vertical") != 0)//Al pulsar hacia arriba/abajo
      {
         //Reinicia el tiempo desde la última selección
         timeSinceLastClick = 0;
         
         //Solo se puede escoger entre dos acciones (0 y 1), por lo que irá alternando entre ellas al pulsar arr/abj
         currentSelectedAction = (currentSelectedAction + 1) % 2;

         //Se resalta la acción seleccionada en el panel de la UI
         battleDialogogBox.SelectAction(currentSelectedAction);
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
               //El player huye
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
      state = BattleState.PlayerMove;
      
      //Muestra/oculta los paneles correspondientes de la UI
      battleDialogogBox.ToggleDialogText(false);
      battleDialogogBox.ToggleActions(false);
      battleDialogogBox.ToggleMovements(true);
      
      //Establece el ataque seleccionado por defecto
      currentSelectedMovement = 0;
      //Se resalta el ataque seleccionado en el panel de la UI y se actualiza su información en el HUD
      battleDialogogBox.SelectMovement(currentSelectedMovement,
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
         battleDialogogBox.SelectMovement(currentSelectedMovement,
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
         battleDialogogBox.SelectMovement(currentSelectedMovement,
            playerUnit.Pokemon.Moves[currentSelectedMovement]);
      }
      //Si se pulsa el botón de acción, se ejecutará el ataque seleccionado por el player
      if (Input.GetAxisRaw("Submit") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;
         //Oculta el panel de movimientos
         battleDialogogBox.ToggleMovements(false);
         //Muestra el diálogo
         battleDialogogBox.ToggleDialogText(true);

         StartCoroutine(PerformPlayerMovement());
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
      yield return battleDialogogBox.SetDialog(String.Format("{0} ha usado {1}",
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
         yield return battleDialogogBox.SetDialog(String.Format("{0} se ha debilitado",
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
      yield return battleDialogogBox.SetDialog(String.Format("{0} ha usado {1}",
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
         yield return battleDialogogBox.SetDialog(String.Format("{0} ha sido debilitado",
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
         else//Si queda algún pokemon con vida en la party, saldrá a la batalla
         {
            playerUnit.SetupPokemon(nextPokemon);
            playerHUD.SetPokemonData(nextPokemon);
            battleDialogogBox.SetPokemonMovements(nextPokemon.Moves);
            yield return battleDialogogBox.SetDialog(String.Format("¡Adelante {0}!",
               nextPokemon.Base.PokemonName));
            
            //Se reanuda la batalla con el nuevo pokemon
            PlayerAction();
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
         yield return battleDialogogBox.SetDialog("¡Golpe crítico!");
      }

      if (damageDesc.AttackType > 1)
      {
         yield return battleDialogogBox.SetDialog("¡Ataque superefectivo!");
      }
      else if (damageDesc.AttackType < 1)
      {
         yield return battleDialogogBox.SetDialog("No es muy efectivo");
      }
   }
}
