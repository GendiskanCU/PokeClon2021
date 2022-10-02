using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
//Importa la librería del paquete de la Asset Store "DOTween (HOTween v2)" para las animaciones
using DG.Tweening;
using UnityEngine.UI;
using Random = UnityEngine.Random;


//GESTIONA LA BATALLA POKEMON PARA EL PLAYER O EL ENEMIGO

//Estados de la batalla
public enum BattleState
{
   StartBattle,//Inicio de la batalla
   ActionSelection,//El player tiene que hacer la selección de movimiento
   MovementSelection,//Se ejecuta el movimiento del player
   Busy,//No se puede hacer nada
   YesNoChoice, //El player está en el modo de elegir en el panel "Sí/No"
   PartySelectScreen,//En la pantalla de selección de pokemon de la party
   ItemSelectScreen,//En la pantalla de selección de items (inventario o mochila). Por implementar
   ForgetMovement,//En la pantalla de selección de movimiento a olvidar al superar el límite de movs. aprendidos
   RunTurn,//El player ejecuta su turno
   FinishBattle//La batalla ha finalizado
}

//Tipos de batalla (contra un pokemon salvaje o contra el de un entrenador o el líder de un gimnasio)
public enum BattleType
{
   WildPokemon,
   Trainer,
   Leader
}

//Acciones que se pueden llevar a cabo durante una batalla
public enum BattleAction
{
   Move, //Elegir movimiento
   SwitchPokemon,  //Cambiar de pokemon
   UseItem, //Usar un item
   Run  //Ejecutar un movimiento
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

   [SerializeField] [Tooltip("Imagen que representará al player en una batalla contra un entrenador")]
   private Image playerImage;

   [SerializeField] [Tooltip("Imagen que representará al entrenador en una batalla contra un entrenador")]
   private Image trainerImage;

   //Para establecer el tipo de batalla
   private BattleType battleType;
   
   //Para guardar el estado actual de la batalla
   private BattleState state;
   
   //Para guardar el estado previo al actual
   //El interrogante se pone para que pueda tomar un valor nulo aunque ese valor no esté entre los enumerados
   private BattleState? previousState;
   
   //Para guardar la party de pokemons del player disponible al entrar en la batalla
   private PokemonParty playerParty;
   
   //Para guardar la party de pokemons de un entrenador pokemon disponible al entrar en la batalla
   private PokemonParty trainerParty;

   //Controladores del player y del entrenador pokemon, que se utilizarán en la batalla contra un entrenador
   private PlayerController player;
   private TrainerController trainer;
   
   //Para guardar el pokemom enemigo que aparece en la batalla contra un pokemon salvaje
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
   
   //Para controlar la opción seleccionada en el panel de elección "Sí/No"
   private bool currentSelectedChoice = true; //true = sí    false = no

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
      bool isLeader = false)
   {
      //Establece el tipo de batalla (Nota: de momento, no se implementará la batalla frente a un leader)
      battleType = (isLeader)? BattleType.Leader: BattleType.Trainer;

      playerParty = playerPokemonParty;//Guarda la party del player
      trainerParty = trainerPokemonParty;//Guarda la party del entrenador

      //Captura los controladores del player y trainer a partir de sus party
      player = playerParty.GetComponent<PlayerController>();
      trainer = trainerParty.GetComponent<TrainerController>();
      
      StartCoroutine(SetupBattle());
   }


   /// <summary>
   /// Implementa la elección del player entre Sí/No cuando un entrenador rival
   /// va a sacar un nuevo pokemon, dando al jugador la opción de cambiar también el suyo
   /// </summary>
   /// <param name="newTrainerPokemon">Pokemon que va a sacar el entrenador rival</param>
   private IEnumerator YesNoChoice(Pokemon newTrainerPokemon)
   {
      state = BattleState.Busy;//Cambia el estado de batalla para que el player no pueda hacer nada
      yield return battleDialogBox.SetDialog($"{trainer.TrainerName} va a sacar a " +
                                $"{newTrainerPokemon.Base.PokemonName}. ¿Quieres cambiar tu pokemon?");
      state = BattleState.YesNoChoice;//Cambia al estado de selección
      
      //Muestra el panel de elección entre Sí/No
      battleDialogBox.ToggleYesNoBox(true);
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
      else if (state == BattleState.YesNoChoice) //Estado: en el panel de decisión Sí/No
      {
         HandleYesNoChoice();
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
   /// Corutina que realiza la configuración de inicio de una batalla contra un pokemon salvaje o un entrenador
   /// </summary>
   public IEnumerator SetupBattle()
   {
      //Establece el estado inicial de la batalla
      state = BattleState.StartBattle;   
      
      //Desactiva temporalmente los HUD de ambos contendientes
      playerUnit.ClearHUD();
      enemyUnit.ClearHUD();

      if (battleType == BattleType.WildPokemon)//Si la batalla que se inicia es contra un pokemon salvaje
      {
         //Captura y configura el primer pokemon con vida de la party de pokemons del player
         playerUnit.SetupPokemon(playerParty.GetFirstNonFaintedPokemon());
      
         //Rellena también el panel de ataques con los que puede ejecutar el pokemon del player
         battleDialogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);

         //Configura el pokemon del enemigo
         enemyUnit.SetupPokemon(wildPokemon);

         //Muestra el primer mensaje en la caja de diálogo de la batalla, esperando hasta que finalice ese proceso
         yield return battleDialogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
            , enemyUnit.Pokemon.Base.PokemonName));
      }
      else //Si la batalla que se inicia es contra un entrenador de pokemon
      {
         //Muestra la imagen de los dos entrenadores, ocultando la imagen de sus pokemon
         playerUnit.gameObject.SetActive(false);
         enemyUnit.gameObject.SetActive(false);
         
         playerImage.sprite = player.TrainerSprite;
         trainerImage.sprite = trainer.TrainerSprite;
         playerImage.gameObject.SetActive(true);
         trainerImage.gameObject.SetActive(true);
         //Utiliza una animación para mostrar la entrada de los entrenadores a escena
         var playerInitialPosition = playerImage.transform.localPosition;
         playerImage.transform.localPosition =  playerInitialPosition - new Vector3(400f, 0, 0);
         playerImage.transform.DOLocalMoveX(playerInitialPosition.x, 0.5f);
         var trainerInitialPosition = trainerImage.transform.localPosition;
         trainerImage.transform.localPosition =  trainerInitialPosition + new Vector3(400f, 0, 0);
         trainerImage.transform.DOLocalMoveX(trainerInitialPosition.x, 0.5f);

         //Muestra el mensaje de comienzo
         yield return battleDialogBox.SetDialog($"¡{trainer.TrainerName} quiere luchar!");
         yield return new WaitForSeconds(1f);
         
         //Selecciona el primer pokemon de las party del player y del entrenador enemigo
         var playerPokemon = playerParty.GetFirstNonFaintedPokemon();
         var enemyPokemon = trainerParty.GetFirstNonFaintedPokemon();

         //Oculta las imágenes de los dos entrenadores y muestra la de sus pokemon
         yield return trainerImage.transform.DOLocalMoveX(trainerImage.transform.localPosition.x + 400,
            0.5f).WaitForCompletion();
         trainerImage.gameObject.SetActive(false);
         trainerImage.transform.localPosition = trainerInitialPosition;//Devuelve la imagen a la posición original
         enemyUnit.gameObject.SetActive(true);
         enemyUnit.SetupPokemon(enemyPokemon);
         yield return battleDialogBox.SetDialog(
            $"{trainer.TrainerName} ha enviado a {enemyPokemon.Base.PokemonName}");

         yield return playerImage.transform.DOLocalMoveX(playerImage.transform.localPosition.x - 400,
            0.5f).WaitForCompletion();
         playerImage.gameObject.SetActive(false);
         playerImage.transform.localPosition = playerInitialPosition;//Devuelve la imagen a la posición original
         playerUnit.gameObject.SetActive(true);
         playerUnit.SetupPokemon(playerPokemon);
         //Rellena el panel de ataques con los que puede ejecutar el pokemon del player
         battleDialogBox.SetPokemonMovements(playerUnit.Pokemon.Moves);
         yield return battleDialogBox.SetDialog($"¡Ven, {playerPokemon.Base.PokemonName}!");
      }

      //Inicializa el HUD de selección de pokemon de la party del player
      partyHUD.InitPartyHUD();
      
      //El player selecciona la acción a realizar
      PlayerActionSelection();
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
               previousState = state;//Guarda el estado anterior
               OpenPartySelectionScreen();
               break;
            case 2:
               //TODO: El player revisa mochila. Se abre la UI del inventario del player
               //Inicia un turno especificando que la acción elegida por el player es la de usar un ítem
               StartCoroutine(RunTurns(BattleAction.UseItem));
               break;
            case 3:
               //Se inicia el turno especificando que el player ha elegido la acción de intentar huir
               StartCoroutine(RunTurns(BattleAction.Run));
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
      //Si se pulsa el botón de acción, comenzará el turno de ataques con el movimiento seleccionado por el player
      if (Input.GetAxisRaw("Submit") != 0)
      {
         //Reinicia el contador de tiempo para permitir una nueva pulsación
         timeSinceLastClick = 0;
         //Oculta el panel de movimientos
         battleDialogBox.ToggleMovements(false);
         //Muestra el diálogo
         battleDialogBox.ToggleDialogText(true);

         //Comienza el siguiente turno
         StartCoroutine(RunTurns(BattleAction.Move));
      }
      
      //Si se pulsa el botón de cancelar, se regresa a la pantalla anterior (la de selección de acción)
      if (Input.GetAxisRaw("Cancel") != 0)
      {
         PlayerActionSelection();
      }
   }


   /// <summary>
   /// Implementa la lógica de elección en el panel Sí/No
   /// </summary>
   private void HandleYesNoChoice()
   {
      if (Input.GetAxis("Vertical") != 0)
      {
         timeSinceLastClick = 0;
         //Cambia entre el Sí y el No
         currentSelectedChoice = !currentSelectedChoice;
      }
      
      //Resalta la opción escogida
      battleDialogBox.SelectYesNoAction(currentSelectedChoice);

      if (Input.GetAxisRaw("Submit") != 0)//Cuando el player confirme su selección
      {
         timeSinceLastClick = 0;
         battleDialogBox.ToggleYesNoBox(false);//Desactiva el panel de elección Sí/no

         if (currentSelectedChoice)
         {
            previousState = BattleState.YesNoChoice;//Indica que el cambio de pokemon viene desde esta situación
            //Si el player ha elegido "Sí", accederá a la pantalla para cambiar su pokemon
            OpenPartySelectionScreen();
         }
         else
         {
            //Si el player ha elegido no cambiar de pokemon, simplementa entra en batalla el nuevo del entrenador rival
            StartCoroutine(SendNextTrainerPokemonToBattle());
         }
      }

      if (Input.GetAxisRaw("Cancel") != 0)//Si el player cancela la selección
      {
         timeSinceLastClick = 0;
         battleDialogBox.ToggleYesNoBox(false);//Desactiva el panel de elección Sí/no
         StartCoroutine(SendNextTrainerPokemonToBattle());//Entra el batalla el nuevo pokemon del entrenador
      }
   }
   
   
   /// <summary>
   /// Ejecuta las acciones del turno actual, tanto del player o del enemigo
   /// </summary>
   /// <param name="playerAction">Acción que ha elegido el player</param>
   /// <returns></returns>
   private IEnumerator RunTurns(BattleAction playerAction)
   {
      //Se cambia el estado de la batalla. Por defecto, player pierde turno hasta que se averigüe quién
      //ataca primero o qué acciones se van a realizar antes
      state = BattleState.RunTurn;
      
      //Caso 1: Si el player ha escogido ejecutar un movimiento
      if (playerAction == BattleAction.Move)
      {
         //Guarda el movimiento que va ejecutar el player
         playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentSelectedMovement];
         
         //Guarda el movimiento que va a ejecutar el enemigo (se elige aleatoriamente)
         enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.RandonMove();
         
         //Se decide quién ataca primero, teniendo en cuenta la prioridad de los ataques y la velocidad de los pokemon
         //Guarda las prioridades en variables temporales
         int enemyPriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;
         int playerPriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
         bool playerGoesFirst = true;//Por defecto el player ataca antes
         //Se comprueba si el enemigo ha seleccionado un ataque de mayor prioridad que el player
         if (enemyPriority > playerPriority )
         {
            playerGoesFirst = false;//En cuyo caso, el enemigo será el que ataca antes
         }
         else if(enemyPriority == playerPriority)
         {
            //Si los dos ataques tienen la misma prioridad, el enemigo atacará antes solo si es más rápido que player
            playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;   
         }
         //Guarda temporalmente el pokemon que ataca primero y el que ataca después
         var firstUnit = playerGoesFirst ? playerUnit : enemyUnit;
         var secondUnit = playerGoesFirst ? enemyUnit : playerUnit;
         
         //Guarda temporalmente en pokemon de la unidad que atacará en segundo lugar
         var secondPokemon = secondUnit.Pokemon;
         
         //El primer pokemon ataca
         yield return RunMovement(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
         //Se comprueba si el primer atacante sufre daños tras su turno
         yield return RunAfterTurn(firstUnit);
         //Se comprueba si el ataque provoca el fin de la batalla
         if (state == BattleState.FinishBattle)
         {
            yield break;
         }
         
         //Después del ataque del primer pokemon, se comprueba también si al segundo le queda vida, porque
         //en caso contrario la batalla ya no continuará
         if (secondPokemon.Hp > 0)
         {
            //Si la batalla sigue, el segundo pokemon lanza su ataque
            yield return RunMovement(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
            //Se comprueba si el segundo atacante sufre daños tras su turno
            yield return RunAfterTurn(secondUnit);
            //Se comprueba nuevamente si el ataque provoca el fin de la batalla
            if (state == BattleState.FinishBattle)
            {
               yield break;
            }
         }
      }
      else //Si el player ha escogido una acción diferente a la de lanzar un ataque o movimiento
      {
         //Caso 2: el player ha escogido cambiar de pokemon
         if (playerAction == BattleAction.SwitchPokemon)
         {
            //Guarda el pokemon seleccionado actualmente
            Pokemon selectedPokemon = playerParty.Pokemons[currentSelectedPokemon];
            //Entra en el estado ocupado, para que no se pueda realizar otra acción mientras dura ésta
            state = BattleState.Busy;
            //Inicia la corutina de intercambio de pokemon
            yield return SwitchPokemon(selectedPokemon);
         }
         //Caso 3: el player ha escogido usar un ítem de la mochila
         else if (playerAction == BattleAction.UseItem)
         {
            //Se oculta el panel de selección de acciones
            battleDialogBox.ToggleActions(false);
            //El player lanza una pokeball
            yield return ThrowPokeball();
         }
         //Caso 4: el player ha escogido intentar huir de la batalla
         else if (playerAction == BattleAction.Run)
         {
            yield return TryToEscapeFromBattle();
            //Si el player ha conseguido huir, habrá cambiado el estado y saldrá de la batalla sin hacer más
            if (state == BattleState.FinishBattle)
            {
               yield break;
            }
         }

         //Una vez que el player ha finalizado su acción, llegará el turno del enemigo:
         //Guarda el movimiento que va a ejecutar el enemigo (se elige aleatoriamente)
         var enemyMove = enemyUnit.Pokemon.RandonMove();
         //El enemigo lanza su ataque
         yield return RunMovement(enemyUnit, playerUnit, enemyMove);
         //Se comprueba si el enemigo sufre daños tras ejecutar su ataque
         yield return RunAfterTurn(enemyUnit);
         //Se comprueba si el ataque provoca el fin de la batalla (el pokemon atacado ha sido vencido)
         if (state == BattleState.FinishBattle)
         {
            yield break;
         }
      }
      
      //Al finalizar el turno, si la batalla no ha terminado se iniciará un nuevo turno
      //con una nueva selección de acción por parte del player
      if (state != BattleState.FinishBattle)
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

      //Se comprueba si el ataque tiene éxito, y en caso afirmativo se aplica la lógica de los efectos del mismo
      if (MoveHits(move, attacker.Pokemon, target.Pokemon))
      {

         //Reproduce las animaciones correspondientes
         yield return RunMoveAnims(attacker, target);

         //Si el ataque ejecutado es del tipo de los que modifican los estados (stats) con un boost
         if (move.Base.TypeOfMove == MoveType.Stats)
         {
            //Aplica los cambios correspondientes sobre las stats
            yield return RunMoveStats(attacker.Pokemon, target.Pokemon, move.Base.Effects, move.Base.Target);
         }
         else //Si el ataque es de otro tipo (físico o especial)
         {

            //Daña al pokemon enemigo y se obtiene el resultado
            DamageDescription damageDesc = target.Pokemon.ReceiveDamage(move, attacker.Pokemon);

            //Actualiza la información del pokemon atacado en el HUD
            yield return target.HUD.UpdatePokemonData();

            yield return ShowDamageDescription(damageDesc); //Muestra información adicional en el HUD
         }
         
         //Tras el ataque también se comprueba si el mismo puede ocasionar algún efecto secundario adicional
         if (move.Base.SecondaryEffects != null && move.Base.SecondaryEffects.Count > 0)
         {
            //Se recorre la lista de efectos secundarios
            foreach (var sec in move.Base.SecondaryEffects)
            {
               //Si el efecto afecta al pokemon enemigo, y a éste todavía le queda vida, o bien el efecto afecta
               //al pokemon atacante y éste tiene vida todavía, se aplicaría, o no, el efecto, una vez calculada
               //aleatoriamente la concurrencia en función de la probabilidad definida para el mismo
               if ((sec.Target == MoveTarget.Other && target.Pokemon.Hp > 0) || 
                   (sec.Target == MoveTarget.Self && attacker.Pokemon.Hp > 0))
               {
                  int rnd = Random.Range(0, 100);
                  if (rnd < sec.Chance)
                  {
                     yield return RunMoveStats(attacker.Pokemon, target.Pokemon, sec, sec.Target);
                  }
               }
            }
         }
         

         if (target.Pokemon.Hp <= 0) //Si tras el ataque el pokemon atacado es debilitado
         {
            //Implementa las acciones que suceden cuando el pokemon atacado es vencido
            yield return HandlePokemonFainted(target);
         }
      }
      else//Si el ataque no ha tenido éxito se muestra el mensaje informativo
      {
         yield return battleDialogBox.SetDialog($"El ataque de {attacker.Pokemon.Base.PokemonName} ha fallado");
      }
   }
  

   /// <summary>
   /// Calcula si un movimiento tendrá éxito, en función de la probabilidad dada por la tasa de acierto del mismo
   /// y teniendo en cuenta además las propias tasas de acierto y evasión del pokemon atacante y el objetivo
   /// </summary>
   /// <param name="move">Movimiento o ataque que ejecuta el pokemon atacante</param>
   /// <param name="attacker">Pokemon atacante</param>
   /// <param name="target">Pokemom objetivo del ataque</param>
   /// <returns>True si el movimiento tiene éxito, False si el movimiento falla</returns>
   private bool MoveHits(Move move, Pokemon attacker, Pokemon target)
   {
      //Si el ataque es de los que siempre acertarán, sin verse afectado por tasas de acierto ni evasión
      if (move.Base.AlwaysHit)
      {
         return true;
      }
      
      //Si el ataque puede verse afectado por las tasas de acierto/evasión:
      
      //Cálculo de la tasa de acierto en función de la tasa de acierto del ataque ejecutado:
      float moveAcc = move.Base.Accuracy;//Tasa de acierto del movimiento

      //La tasa de acierto calculada podrá ser mejorada o empeorada por el boost de las estadísticas propias de acierto
      //y evasión tanto del propio pokemon que ejecuta el ataque como del que lo recibe
      float accuracy = attacker.StatsBoosted[Stat.Accuracy];
      float evasion = target.StatsBoosted[Stat.Evasion];
      
      
      //Se calculan los multiplicadores de mejora de la tasa de acierto, según la fórmula extraída de la bulbapedia:
      float multiplierAccuracy =  1 + Mathf.Abs(accuracy) / 3.0f;
      float multiplierEvasion =  1 + Mathf.Abs(evasion) / 3.0f;
      
      //Se aplican los multiplicadores, teniendo en cuenta si son positivos o negativos y con efecto contrario si
      //es el de acierto del atacante o el de la evasión del objetivo
      if (accuracy > 0)
      {
         moveAcc *= multiplierAccuracy;
      }
      else
      {
         moveAcc /= multiplierAccuracy;
      }

      if (evasion > 0)
      {
         moveAcc /= multiplierEvasion;
      }
      else
      {
         moveAcc *= multiplierEvasion;
      }
      
      //Se calcula aleatoriamente si el ataque tendrá éxito en función de la tasa de acierto calculada
      float rnd = Random.Range(0, 100);
      
      return rnd < moveAcc;//Devuelve el resultado final
   }
   
   
   /// <summary>
   /// Aplica los boosts correspondientes sobre las stats del pokemon atacanta o defensor, cuando el movimiento
   /// ejecutado es de los que modifican estadísticas
   /// También aplica un estado alterado sobre el pokemon si el movimiento ejecutado lo puede producir
   /// </summary>
   /// <param name="attacker">El pokemon que ejecuta el ataque o movimiento que afecta a estadisticas</param>
   /// <param name="target">El pokemon que recibe el ataque o movimiento que afecta a estadísticas</param>
   /// <param name="move">Efecto alterado, volátil o secundario queu se va a aplicar</param>
   /// /// <param name="targetEffect">Pokemon sobre el que se aplica el efecto</param>
   /// <returns></returns>
   private IEnumerator RunMoveStats(Pokemon attacker, Pokemon target, MoveStatEffect effect,
      MoveTarget targetEffect)
   {
      //Primero aplica los boosts en las stats
      if (effect.Boostings != null)//Siempre y cuando en el ataque estén definidos los boosts
      {
         //Se recorre la lista de boost que puede provocar el ataque
         foreach (var boost in effect.Boostings)
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
      if (effect.Status != StatusConditionID.none)
      {
         if (targetEffect == MoveTarget.Other)//Si el objetivo del ataque es el otro pokemon
         {
            target.SetConditionStatus(effect.Status);
         }
         else //Si el objetivo del ataque es el propio pokemon que ejecuta el ataque
         {
            attacker.SetConditionStatus(effect.Status);
         }
      }
      
      //También aplica el estado volátil, si lo hubiera, sobre el pokemon que recibe el ataque
      if (effect.VolatileStatus != StatusConditionID.none)
      {
         if (targetEffect == MoveTarget.Other)//Si el objetivo del ataque es el otro pokemon
         {
            target.SetVolatileConditionStatus(effect.VolatileStatus);
         }
         else //Si el objetivo del ataque es el propio pokemon que ejecuta el ataque
         {
            attacker.SetVolatileConditionStatus(effect.VolatileStatus);
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
   /// Efectúa comprobaciones del estado del pokemon atacante tras finalizar su turno en la batalla
   /// </summary>
   /// <param name="attacker">El pokemon atacante</param>
   /// <returns></returns>
   private IEnumerator RunAfterTurn(BattleUnit attacker)
   {
      //Si la batalla ya ha finalizado (p.ej. el atacante ha debilitado al otro pokemon) no hace falta comprobar nada
      if (state == BattleState.FinishBattle)
      {
         yield break;
      }

      //Espera hasta que haya finalizado totalmente el turno y el estado de batalla se haya cambiado
      yield return new WaitUntil(() => state == BattleState.RunTurn);
      
      //Tras finalizar el turno, se realizan acciones y comprobaciones adicionales sobre el pokemon atacante,
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
      
      //Espera nuevamente hasta que haya finalizado totalmente el turno y el estado de batalla cambie a nuevo turno
      yield return new WaitUntil(() => state == BattleState.RunTurn);
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
      else//Si el pokemon vencido es del enemigo, hay que diferenciar si la batalla es de entrenador o pokemon salvaje
      {
         if (battleType == BattleType.WildPokemon)
         {
            BattleFinish(true); //Finaliza la batalla con victoria del player
         }
         else
         {
            //Comprueba si el entrenador enemigo tiene todavía algún pokemon con vida en su party
            var nextPokemon = trainerParty.GetFirstNonFaintedPokemon();
            if (nextPokemon != null)
            {
               //Como el entrenador va a cambiar de pokemon, se da al player la opción de
               //cambiar también el suyo
               StartCoroutine(YesNoChoice(nextPokemon));
            }
            else
            {
               //Finaliza la batalla con victoria del player
               BattleFinish(true);
            }
         }
      }
   }


   /// <summary>
   /// "Envía" un nuevo pokemon del entrenador enemigo a la batalla contra el player
   /// </summary>
   /// <returns></returns>
   private IEnumerator SendNextTrainerPokemonToBattle()
   {
      state = BattleState.Busy;//Cambia el estado para que no se puedan realizar acciones hasta terminar
      
      //Captura el nuevo pokemon que se debe enviar a la batalla
      var nextPokemon = trainerParty.GetFirstNonFaintedPokemon();
      
      //Configura el nuevo pokemon del entrenador enemigo
      enemyUnit.SetupPokemon(nextPokemon);
      
      //Muestra un mensaje informativo
      yield return battleDialogBox.SetDialog(
         $"{trainer.TrainerName} ha enviado a {nextPokemon.Base.PokemonName}");
      
      //Vuelve a cambiar el estado de la batalla para que se inicie un nuevo turno
      state = BattleState.RunTurn;
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
      
      //Si se pulsa el botón de acción 
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
         
         //Desactiva el panel de elección de pokemon y también el de acciones
         partyHUD.gameObject.SetActive(false);
         battleDialogBox.ToggleActions(false);

         //Si se ha llegado a la pantalla de selección de pokemon porque el player lo ha elegido así, no porque
         //el pokemon que estaba en batalla haya sido debilitado (el estado previo es ActionSelection)
         if (previousState == BattleState.ActionSelection)
         {
            previousState = null;//Resetea el estado anterior
            //Se inicia un nuevo turno indicando que el player ha elegido cambiar de pokemon
            StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
         }
         //Si se ha llegado a la pantalla de selección de pokemon porque el actual ha sido debilitado
         else
         {
            state = BattleState.Busy;//Cambia el estado para no permitir movimientos
            //Continúa el turno actual intercambiando el pokemon actual por el seleccionado
            StartCoroutine(SwitchPokemon(selectedPokemon));
         }
      }
      
      //Si se pulsa el botón Cancelar
      if (Input.GetAxisRaw("Cancel") != 0)
      {
         //Si el pokemon del player que está en batalla no tiene vida, no podrá cancelar el cambio de pokemon
         if (playerUnit.Pokemon.Hp <= 0)
         {
            partyHUD.SetMessage("Tienes que seleccionar un pokemon para continuar...");
            return;
         }
         
         partyHUD.gameObject.SetActive(false);
         //Si se venía de haber seleccionado Sí en el panel de elección Sí/No que aparecerá cuando el entrenador
         //rival haya cambiado su pokemon, se continúa sin más la batalla, con ese pokemon enviado a la misma
         if (previousState == BattleState.YesNoChoice)
         {
            previousState = null;
            StartCoroutine(SendNextTrainerPokemonToBattle());
         }
         else //En caso contrario, se volverá a escoger la acción del player
         {
            PlayerActionSelection();
         }
      }
   }
   

   /// <summary>
   /// Cambia el pokemon actual en batalla por el indicado como parámetro
   /// </summary>
   /// <param name="newPokemon">El pokemon que debe entrar en la batalla</param>
   /// <returns></returns>
   private IEnumerator SwitchPokemon(Pokemon newPokemon)
   {
      //Solo si el pokemon que se va a retirar todavía tenía vida se realizan estas acciones
      if (playerUnit.Pokemon.Hp > 0)
      {
         //Muestra un mensaje de retirada
         yield return battleDialogBox.SetDialog($"¡Vuelve, {playerUnit.Pokemon.Base.PokemonName}!");
         //Reproduce la animación de retirada del pokemon actual
         playerUnit.PlayFaintAnimation();
         //Espera un instante para que finalice la animación
         yield return new WaitForSeconds(1.5f);
      }

      //Configura el pokemon que se va a incorporar a la batalla
      playerUnit.SetupPokemon(newPokemon);
      //Se actualizan sus movimientos en la UI
      battleDialogBox.SetPokemonMovements(newPokemon.Moves);
      //Muestra el mensaje de entrada del nuevo pokemon
      yield return battleDialogBox.SetDialog($"¡Adelante, {newPokemon.Base.PokemonName}!");
      //Espera un instante para que dé tiempo a que el mensaje finalice correctamente
      yield return new WaitForSeconds(1f);
      
      //Finaliza el turno tras el cambio de pokemon, salvo en el caso de que este cambio venga de la
      //elección de cambio en el panel de elección "Sí/No", que aparecerá cuando un entrenador rival haya cambiado
      //su pokemon. En este caso, tras cambiar también el del player, se procede a mandar a batalla al del entrenador
      if (previousState == null)
      {
         state = BattleState.RunTurn;
      }
      else if (previousState == BattleState.YesNoChoice)
      {
         //Es posible que aquí debamos poner nuevamente previousState = null
         yield return SendNextTrainerPokemonToBattle();
      }
   }

   /// <summary>
   /// Ejecuta la acción de lanzar una pokeball por parte del player para atrapar al pokemon enemigo
   /// </summary>
   /// <returns></returns>
   private IEnumerator ThrowPokeball()
   {
      //Cambia el estado actual de la batalla
      state = BattleState.Busy;

      //Si se ha lanzado la pokeball contra un pokemon de otro entrenador, no será posible y se perderá el turno
      if (battleType != BattleType.WildPokemon)
      {
         yield return battleDialogBox.SetDialog("¡No puedes robar los pokemon de otros entrenadores!");
         state = BattleState.RunTurn;
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
         state = BattleState.RunTurn;
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
         state = BattleState.RunTurn;//Pierde el turno
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
            state = BattleState.RunTurn;
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
