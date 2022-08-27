using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LOS POKEMON QUE PUEDEN APARECER EN CADA ÁREA DEL MAPA

public class PokemonMapArea : MonoBehaviour
{
    [SerializeField] [Tooltip("Lista de pokemon salvajes")]
    private List<Pokemon> wildPokemons;

    /// <summary>
    /// Devuelve un pokemon elegido aleatoriamente de entre la lista de pokemon salvajes que pueden aparecer
    /// dentro de un área de pokemons
    /// </summary>
    /// <returns></returns>
    public Pokemon GetRandomWildPokemon()
    {
        //Obtiene un pokemon al azar
        var pok = wildPokemons[Random.Range(0, wildPokemons.Count)];
        //Inicializa el pokemon obtenido y lo devuelve
        pok.InitPokemon();
        return pok;
    }
}
