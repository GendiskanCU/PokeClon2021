using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LA INFORMACIÓN DE NOMBRE DEL POKEMON, NIVEL DE POKEMON Y BARRA DE VIDA EN LA UI DE BATALLA

public class BattleHUD : MonoBehaviour
{
    [SerializeField] [Tooltip("Texto que contendrá el nombre del Pokemon")]
    private Text pokemonName;
    
    [SerializeField] [Tooltip("Texto que contendrá el nivel del Pokemon")]
    private Text pokemonLevel;
    
    [SerializeField] [Tooltip("Texto que contendrá la cantidad de vida del Pokemon")]
    private Text pokemonHealth;
    
    [SerializeField] [Tooltip("Script que gestiona la barra de vida del Pokemon")]
    private HealthBarUI healthBar;

    /// <summary>
    /// Actualiza la información del nombre, nivel y tamaño de la barra de vida de un pokemon en el HUD de batalla
    /// </summary>
    /// <param name="pokemon">Pokemon</param>
    public void SetPokemonData(Pokemon pokemon)
    {
        pokemonName.text = pokemon.Base.PokemonName;
        pokemonLevel.text = String.Format("Lv: {0}", pokemon.Level);

        pokemonHealth.text = String.Format("{0}/{1}", pokemon.Hp, pokemon.MaxHP);
        
        //La vida hay que pasarla con un valor entre 0 y 1, por lo que se divide la actual entre la máxima
        healthBar.SetHP(pokemon.Hp / pokemon.MaxHP);
    }
}
