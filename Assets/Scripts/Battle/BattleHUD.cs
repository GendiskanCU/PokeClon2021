using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [SerializeField] [Tooltip("Barra indicadora de la experiencia actual del Pokemon")]
    private GameObject expBar;

    //Referencia al pokemon que contenga este script
    private Pokemon _pokemon;

    /// <summary>
    /// Inicializa la información con  nombre, nivel y tamaño de la barra de vida de un pokemon en el HUD de batalla
    /// </summary>
    /// <param name="pokemon">El Pokemon del que se va a mostrar información en el HUD</param>
    public void SetPokemonData(Pokemon pokemon)
    {
        //Guarda el pokemon recibido para poder ser utilizado a partir de ahora en otros métodos de esta misma clase
        _pokemon = pokemon;
        
        pokemonName.text = pokemon.Base.PokemonName;
        pokemonLevel.text = String.Format("Lv {0}", pokemon.Level);
        
        //Inicializa la barra de vida con la vida actual del player
        healthBar.SetHP((float) _pokemon.Hp / _pokemon.MaxHP);
        
        StartCoroutine(UpdatePokemonData(_pokemon.Hp));
    }

    /// <summary>
    /// Actualiza el texto con la vida y la barra de vida del pokemon en el HUD
    /// </summary>
    public IEnumerator UpdatePokemonData(int oldHPValue)
    {
        //La vida hay que pasarla con un valor entre 0 y 1, por lo que se divide la actual entre la máxima
        //Hay que forzar que el resultado dé un float para evitar que al dividir números enteros pueda ser siempre 0
        //healthBar.SetHP((float)_pokemon.Hp / _pokemon.MaxHP);
        StartCoroutine(healthBar.SetSmoothHP((float) _pokemon.Hp / _pokemon.MaxHP));
        StartCoroutine( DecreaseHealthPointsText(oldHPValue));

        yield return null;
    }

    /// <summary>
    /// Actualiza progresivamente el texto de cantidad de vida en la barra
    /// </summary>
    /// <param name="oldHP"></param>
    /// <returns>vida de partida antes de recibir daño</returns>
    private IEnumerator DecreaseHealthPointsText(int oldHP)
    {
        while (oldHP > _pokemon.Hp)
        {
            oldHP--;
            pokemonHealth.text = String.Format("{0}/{1}",oldHP, _pokemon.MaxHP);
            yield return new WaitForSeconds(0.1f);
        }
        
        pokemonHealth.text = String.Format("{0}/{1}", _pokemon.Hp, _pokemon.MaxHP);
    }


    /// <summary>
    /// Actualiza la barra de experiencia del pokemon
    /// </summary>
    public void SetExp()
    {
        if (expBar != null)//Solo tiene barra de experiencia el pokemon del player
        {
            
        }
    }
}
