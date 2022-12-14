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
    
    
    
    [SerializeField] [Tooltip("Script que gestiona la barra de vida del Pokemon")]
    private HealthBarUI healthBar;

    [SerializeField] [Tooltip("Recuadro de color que indica el estado alterado del pokemon, si lo hay")]
    private GameObject statusBox;

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
        SetLevelText();
        
        //Inicializa la barra de vida con la vida actual del pokemon del player
        //healthBar.SetSmoothHP(_pokemon);
        healthBar.SetHP(_pokemon);
        
        
        //Inicializa la barra de experiencia con la experiencia actual del pokemon del player
        SetExp();
        
        //Inicializa el recuadro indicador del estado alterado del pokemon
        SetStatusConditionData();
        
        //Suscripción al evento que invocará el pokemon cuando su estado haya cambiado, para actualizar el recuadro
        _pokemon.OnStatusConditionChanged += SetStatusConditionData;
        
        StartCoroutine(UpdatePokemonData());
    }

    /// <summary>
    /// Actualiza el texto con la vida y la barra de vida del pokemon en el HUD, si la misma ha sido modificada
    /// </summary>
    /// <returns></returns>
    public IEnumerator UpdatePokemonData()
    {
        if (_pokemon.HasHPChanged)
        {
            
            yield return healthBar.SetSmoothHP(_pokemon);
            
            //Vuelve a resetear la booleana para que no se vuelva a ejecutar este código si la vida no se modifica
            //otra vez
            _pokemon.HasHPChanged = false;
        }
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
    /// <param name="needsToResetBar">True si la barra debe ponerse otra vez a "0" porque se ha subido de nivel</param>
    /// <returns></returns>
    public IEnumerator SetExpSmooth(bool needsToResetBar = false)
    {
        if (needsToResetBar)
        {
            expBar.transform.localScale = new Vector3(0, 1, 1);
        }
        
        if (expBar != null)//Solo tiene barra de experiencia el pokemon del player, por lo que puede ser nulo
        {
            //Utiliza un método de la librería DG.Tweening para escalar la barra en el eje X con una animación
            yield return expBar.transform.DOScaleX(NormalizedExp, 1.5f).WaitForCompletion();
        }

        yield break;
    }


    /// <summary>
    /// Escribe el nivel actual del pokemon en la caja de texto correspondiente de la UI
    /// </summary>
    public void SetLevelText()
    {
        pokemonLevel.text = $"Lv {_pokemon.Level}";
    }


    private void SetStatusConditionData()
    {
        if (_pokemon.StatusCondition == null) //Si el pokemon no tiene activo ningún estado alterado
        {
            statusBox.SetActive(false);
        }
        else
        {
            //Cambia el color del recuadro indicador conforme al estado alterado activo
            statusBox.GetComponent<Image>().color =
                ColorManager.StatusConditionColor.GetColorFromStatusCondition(_pokemon.StatusCondition.Id);
            //Cambia el texto dentro del recuadro indicador
            statusBox.GetComponentInChildren<Text>().text = _pokemon.StatusCondition.Id.ToString().ToUpper();
            statusBox.SetActive(true);
        }
    }
    
}
