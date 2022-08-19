using System;
using System.Collections;
using System.Collections.Generic;
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
   
   //Para controlar la acción seleccionada por el player en el panel de selección de acciones
   private int currentSelectedAction;
   //Para controlar que no se pueda cambiar de acción seleccionada hasta pasado un lapso aunque se mantenga pulsado
   private float timeSinceLastClick;
   [SerializeField] private float timeBetweenClicks = 1.0f;


   private void Start()
   {
      StartCoroutine(SetupBattle());
   }

   private void Update()
   {
      //Cuando esté activo el estado de selección de una acción por parte del player
      if (state == BattleState.PlayerSelectAction)
      {
         HandlePlayerActionSelection();
      }
   }

   /// <summary>
   /// Corutina que realiza la configuración de inicio de una batalla
   /// </summary>
   public IEnumerator SetupBattle()
   {
      //Establece el estado inicial de la batalla
      state = BattleState.StartBattle;
      
      //Configura el pokemon del player
      playerUnit.SetupPokemon();
      //Configura el HUD del player
      playerHUD.SetPokemonData(playerUnit.Pokemon);
      
      //Configura el pokemon del enemigo
      enemyUnit.SetupPokemon();
      //Configura el HUD del enemigo
      enemyHUD.SetPokemonData(enemyUnit.Pokemon);
      
      //Muestra el primer mensaje en la caja de diálogo de la batalla, esperando hasta que finalice ese proceso
      yield return battleDialogogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
         , enemyUnit.Pokemon.Base.PokemonName));
      
      //Hace una pausa de 1 segundo
      yield return new WaitForSeconds(1.0f);
      
      //Inicia las acciones del player o del enemigo. Se comparan las velocidades de ambos contendientes
      //(enemyUnit, playerUnit) para decidir quién atacará primero
      if (enemyUnit.Pokemon.Speed > playerUnit.Pokemon.Speed)
      {
         EnemyAction();
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
      
      //Muestra el panel de selección de acciones
      battleDialogogBox.ToggleActions(true);
      
      //Se reinicia y resalta la acción por defecto
      currentSelectedAction = 0;
      battleDialogogBox.SelectAction(currentSelectedAction);
   }

   /// <summary>
   /// Responde a la acción seleccionada por el player
   /// </summary>
   private void HandlePlayerActionSelection()
   {
      //Se cambiará de acción pulsando arriba/abajo, dejando un lapso de tiempo mínimo para poder ir cambiando
      timeSinceLastClick += Time.deltaTime;
      
      if (timeSinceLastClick < timeBetweenClicks)
         return;
      if (Input.GetAxisRaw("Vertical") != 0)
      {
         timeSinceLastClick = 0;
         
         if (currentSelectedAction == 0)
         {
            currentSelectedAction++;
         }
         else
         {
            currentSelectedAction = 0;
         }
         
         //Se resalta la acción seleccionada en el panel de la UI
         battleDialogogBox.SelectAction(currentSelectedAction);
      }
      
   }

   /// <summary>
   /// Inicia las acciones de ataque del enemigo
   /// </summary>
   private void EnemyAction()
   {
      StartCoroutine(battleDialogogBox.SetDialog("El enemigo ataca primero..."));
   }
}
