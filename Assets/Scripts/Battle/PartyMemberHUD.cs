using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LA INFORMACIÓN DE CADA POKEMON EN LA PANTALLA DE LA UI DE ELECCIÓN DE POKEMON 

public class PartyMemberHUD : MonoBehaviour
{
    [SerializeField] [Tooltip("Texto con el nombre del pokemon")]
    private Text nameText;

    [SerializeField] [Tooltip("Texto con el nivel del pokemon")]
    private Text levelText;

    [SerializeField] [Tooltip("Texto con el tipo del pokemon")]
    private Text typeText;

    [SerializeField] [Tooltip("Texto con la vida del pokemon")]
    private Text hpText;

    [SerializeField] [Tooltip("Barra de vida del pokemon")]
    private HealthBarUI healthBar;

    [SerializeField] [Tooltip("Imagen del pokemon")]
    private Image pokemonImage;

    //El pokemon
    private Pokemon _pokemon;

    /// <summary>
    /// Completa la información de un pokemon en la pantalla de elección de la party de pokemons
    /// </summary>
    /// <param name="pok">Pokemon del que mostrar la información</param>
    public void SetPokemonData(Pokemon pok)
    {
        _pokemon = pok;

        nameText.text = _pokemon.Base.PokemonName;
        levelText.text = String.Format("Lv {0}", _pokemon.Level);
        
        typeText.text = (_pokemon.Base.Type2.ToString() != "NINGUNO") ?
            $"{_pokemon.Base.Type1.ToString()}-{_pokemon.Base.Type2.ToString()}" :
            $"{_pokemon.Base.Type1.ToString()}";
        
        hpText.text = String.Format("{0}/{1}",_pokemon.Hp, _pokemon.MaxHP);
        healthBar.SetHP((float)_pokemon.Hp / _pokemon.MaxHP);
        pokemonImage.sprite = _pokemon.Base.FrontSprite;
        
        //Se cambia el color de fondo de la caja de información según el tipo1 del pokemon del que se trate
        GetComponent<Image>().color = ColorManager.TypeColor.GetColorFromType(_pokemon.Base.Type1);
    }
    
    /// <summary>
    /// Modifica el color en el nombre del pokemon
    /// </summary>
    /// <param name="selected">True si es el pokemon seleccionado, false en caso contrario</param>
    public void SetSelectedPokemon(bool selected)
    {
        if (selected)
        {
            nameText.color = ColorManager.SharedInstance.SelectedColor;
        }
        else
        {
            nameText.color = Color.black;
        }
    }
}
