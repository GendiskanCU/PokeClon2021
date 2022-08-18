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

   private void Start()
   {
      SetupBattle();
   }

   /// <summary>
   /// Configura la batalla
   /// </summary>
   public void SetupBattle()
   {
      //Configura el pokemon
      playerUnit.SetupPokemon();
      //Configura el HUD
      playerHUD.SetPokemonData(playerUnit.Pokemon);
   }
}
