using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA EL EQUIPO DE POKEMONS DEL PLAYER

public class PokemonParty : MonoBehaviour
{
   [SerializeField] [Tooltip("Lista de pokemons del party")]
   private List<Pokemon> pokemons;

   public List<Pokemon> Pokemons
   {
      get => pokemons;
      set => pokemons = value;
   }
}
