using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//GESTIONA LA BATALLA POKEMON PARA EL PLAYER O EL ENEMIGO
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

   private void Start()
   {
      SetupBattle();
   }

   /// <summary>
   /// Configura la batalla
   /// </summary>
   public void SetupBattle()
   {
      //Configura el pokemon del player
      playerUnit.SetupPokemon();
      //Configura el HUD del player
      playerHUD.SetPokemonData(playerUnit.Pokemon);
      
      //Configura el pokemon del enemigo
      enemyUnit.SetupPokemon();
      //Configura el HUD del enemigo
      enemyHUD.SetPokemonData(enemyUnit.Pokemon);
      
      //Muestra el primer mensaje en la caja de diálogo de la batalla
      StartCoroutine(battleDialogogBox.SetDialog(String.Format("Un {0} salvaje ha aparecido."
         , enemyUnit.Pokemon.Base.PokemonName)));
   }
}
