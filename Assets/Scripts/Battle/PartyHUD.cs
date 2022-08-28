using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LA PANTALLA DE SELECCIÓN DE POKEMON DE LA PARTY EN LA UI

public class PartyHUD : MonoBehaviour
{
   //Lista de la información en UI de los pokemons de la party
   private PartyMemberHUD[] memberHuds;

   [SerializeField] [Tooltip("Texto de los mensajes informativos")]
   private Text messageText;

   //Para guardar la lista de pokemons de la party
   private List<Pokemon> pokemons;

   /// <summary>
   /// Inicializa el HUD de la party de pokemons
   /// </summary>
   public void InitPartyHUD()
   {
      //Rellena la lista de las UI de los pokemon en la party
      memberHuds = GetComponentsInChildren<PartyMemberHUD>();
      
      
   }

   /// <summary>
   /// Rellena el HUD de la party de pokemons con la información de los pokemon del player
   /// </summary>
   /// <param name="pokemons">Lista de pokemon en la party del player</param>
   public void SetPartyData(List<Pokemon> pokemonsInParty)
   {
      //Guarda la lista de pokemons
      pokemons = pokemonsInParty;
      
      //Recorre las cajas y si hay pokemon para ellas muestra su información, u oculta la caja en caso contrario
      for (int i = 0; i < memberHuds.Length; i++)
      {
         if (i < pokemons.Count)
         {
            memberHuds[i].SetPokemonData(pokemons[i]);
            memberHuds[i].gameObject.SetActive(true);
         }
         else
         {
            memberHuds[i].gameObject.SetActive(false);
         }
      }
      
      //Muestra el mensaje inicial
      messageText.text = "Elige un Pokemon";
   }

   
   /// <summary>
   /// Actualiza la UI de selección de pokemon mostrando el pokemon seleccionado en ese momento
   /// </summary>
   /// <param name="selectedPokemon">Posición del pokemon seleccionado</param>
   public void UpdateSelectedPokemon(int selectedPokemon)
   {
      for (int i = 0; i < pokemons.Count; i++)
      { 
         memberHuds[i].SetSelectedPokemon( i == selectedPokemon  );
      }
   }


   /// <summary>
   /// Muestra un mensaje en el área de información del HUD de selección de pokemon de la party de pokemons
   /// </summary>
   /// <param name="message"></param>
   public void SetMessage(string message)
   {
      messageText.text = message;
   }
}
