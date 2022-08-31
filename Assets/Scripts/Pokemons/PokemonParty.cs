using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//GESTIONA EL EQUIPO DE POKEMONS DEL PLAYER

public class PokemonParty : MonoBehaviour
{
   //Número máximo de pokemons que puede tener la party del player
   private const int NUM_MAX_POKEMON_IN_PARTY = 6;
   
   [SerializeField] [Tooltip("Lista de pokemons del party")]
   private List<Pokemon> pokemons;

   public List<Pokemon> Pokemons
   {
      get => pokemons;
   }
   
   //Para posible futura implementación del "PC de Bill" en el que guardar los pokemon que no caben en la party
   //El PC de Bill tendrá una capacidad de 6 cajas de pokemons:
   private List<List<Pokemon>> pcBillBoxes;

   private void Start()
   {
      //Inicializa todos los pokemon de la party
      foreach (var pok in pokemons)
      {
         pok.InitPokemon();
      }
      
      /*Para posible futura implementación del "PC de Bill" en el que guardar los pokemon que no caben en la party
      //Cada caja de pokemons del PC de Bill tendrá una capacidad de 15 pokemons
      var box = new List<Pokemon>(15);
      //El PC de Bill tendrá 6 cajas de pokemons
      for(int i= 0; i < 6; i++)
         pcBillBoxes.Add(box);   */
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


   /// <summary>
   /// Devuelve la posición de un pokemon en la lista de pokemons de la party
   /// </summary>
   /// <param name="pokemon">Pokemon del que obtener su posición en la party</param>
   /// <returns></returns>
   public int GetPositionFromPokemon(Pokemon pokemon)
   {
      for (int i = 0; i < Pokemons.Count; i++)
      {
         if (pokemon == Pokemons[i])
            return i;
      }

      return -1;//Aquí en realidad no se va a llegar
   }
 
   /// <summary>
   /// Añade un pokemon a la party de pokemons del player, siempre que no esté llena
   /// </summary>
   /// <param name="pokemon">Pokemon a añdir a la party</param>
   /// <returns>True si el pokemon ha podido ser añadido a la party</returns>
   public bool AddPokemonToParty(Pokemon pokemon)
   {
      if (pokemons.Count < NUM_MAX_POKEMON_IN_PARTY)
      {
         pokemons.Add(pokemon);
         return true;
      }
      else
      {
         return false;
         //TODO: Faltaría añadir la funcionalidad de enviar el pokemon capturado al "PC de Bill"
      }
   }
}
