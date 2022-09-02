using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LA INFORMACIÓN DE NOMBRE DEL POKEMON, NIVEL DE POKEMON, EXPERIENCIA Y BARRA DE VIDA EN LA UI DE BATALLA

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

    //Para calcular el valor de la experiencia normalizada entre 0 y 1 (y aplicar el mismo al tamaño de barra de exp)
    private float normalizedExp;
    public float NormalizedExp
    {
        get
        {
            //Experiencia base del nivel actual
            float currentLevelExp = _pokemon.Base.GetNeccessaryExperienceForLevel(_pokemon.Level);
            //Experiencia necesaria para el nivel siguiente
            float nextLevelExp = _pokemon.Base.GetNeccessaryExperienceForLevel(_pokemon.Level + 1);
            
            /* Utiliza la siguiente fórmula, basada en que la experiencia del nivel actual (min) equivale a un
             tamaño 0 de la barra de experiencia y la experiencia del nivel siguiente (max) equivale a un tamaño
             1 de la barra de experiencia. ¿Cuál será el tamaño x de la barra si se tiene un nivel de experiencia
             intermedio (xp)?
                                  (max - min) = (1 - 0)
                                  (xp - min) = (x - 0)            

                  x - 0 = (xp - min) * (1 - 0) / (max - min) ->> x = (xp - min)/(max - min)*/
            
            //Se calcula el valor para el tamaño de la barra de experiencia según la experiencia actual
            float normExp = (_pokemon.Experience - currentLevelExp) / (nextLevelExp - currentLevelExp);
            
            //Se devuelve el valor "normalizado" asegurando que se recorta siempre a un valor entre 0 y 1
            return Mathf.Clamp01(normExp);
        }
        set => normalizedExp = value;
    }
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
        
        //Inicializa la barra de vida con la vida actual del pokemon del player
        healthBar.SetHP((float) _pokemon.Hp / _pokemon.MaxHP);
        
        //Inicializa la barra de experiencia con la experiencia actual del pokemon del player
        SetExp();
        
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
    /// Actualiza el tamaño de la barra de experiencia del pokemon conforme a la experiencia actual
    /// </summary>
    public void SetExp()
    {
        if (expBar != null)//Solo tiene barra de experiencia el pokemon del player, por lo que puede ser nulo
        {
            expBar.transform.localScale = new Vector3(NormalizedExp, 1, 1);
        }
    }

    /// <summary>
    /// Actualiza progresivamente el tamaño de la barra de experiencia del pokemon conforme a la experiencia actual
    /// </summary>
    /// <returns></returns>
    public IEnumerator SetExpSmooth()
    {
        if (expBar != null)//Solo tiene barra de experiencia el pokemon del player, por lo que puede ser nulo
        {
            //Utiliza un método de la librería DG.Tweening para escalar la barra en el eje X con una animación
            expBar.transform.DOScaleX(NormalizedExp, 1.5f).WaitForCompletion();
        }

        yield break;
    }
    
    
}
