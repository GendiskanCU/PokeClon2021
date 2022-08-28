using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//GESTIONA EL EQUIPO DE POKEMONS DEL PLAYER

public class PokemonParty : MonoBehaviour
{
   [SerializeField] [Tooltip("Lista de pokemons del party")]
   private List<Pokemon> pokemons;

   public List<Pokemon> Pokemons
   {
      get => pokemons;
   }

   private void Start()
   {
      //Inicializa todos los pokemon de la party
      foreach (var pok in pokemons)
      {
         pok.InitPokemon();
      }
   }

   /// <summary>
   /// Devuelve el primer pokemon con vida que localice en la party de pokemons del player
   /// </summary>
   /// <returns></returns>
   public Pokemon GetFirstNonFaintedPokemon()
   {
      //Utiliza una función lambda para acortar la sintaxis de búsqueda, en vez de un bucle for/foreach
      //Devuelve el primer pokemon que encuentre con Hp > 0 o nulo si no hay ninguno
      return pokemons.Where(x => x.Hp > 0).FirstOrDefault();
   }
}
