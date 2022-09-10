using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LAS BARRAS DE VIDA DE UN POKEMON EN LA UI DE BATALLA

public class HealthBarUI : MonoBehaviour
{
   [SerializeField] [Tooltip("Imagen de la barra de vida")]
   private GameObject healthBar;

   [SerializeField] [Tooltip("Texto que contendrá la cantidad de vida actual del Pokemon")]
   private Text currentHPText;

   [SerializeField] [Tooltip("Texto que contendrá la cantidad de vida máxima del Pokemon")]
   private Text maxHPText;
   
  

   /// <summary>
   /// Actualiza el tamaño la barra de vida a partir de un valor normalizado (entre 0 y 1) de la misma
   /// y también actualiza el texto con el valor de vida máxima del pokemon que hay en la barra de vida
   /// </summary>
   /// <param name="normalizedValue"Valor normalizado (entre 0f y 1f) de la vida></param>
   public void SetHP(Pokemon pokemon)
   {
      //Calcula el valor normalizado de la vida actual para aplicarlo a la escala de la barra de vida
      float normalizedValue = (float) pokemon.Hp / pokemon.MaxHP;
      
      //Modifica el tamaño de la barra de vida escalándola
      healthBar.transform.localScale = new Vector3(normalizedValue, 1, 1);
      
      //Cambia el color de la barra en función de su tamaño actual
      healthBar.GetComponent<Image>().color = ColorManager.SharedInstance.BarColor(normalizedValue);
      
      //Actualiza el texto que muestra la cantidad de vida máxima del pokemon
      maxHPText.text = $"/{pokemon.MaxHP}";
   }


   /// <summary>
   /// Actualiza la barra de vida de forma progresiva a partir del valor normalizado de la misma
   /// </summary>
   /// <param name="pokemon">El pokemon del que hay que actualizar la barra de vida</param>
   /// <returns></returns>
   public IEnumerator SetSmoothHP(Pokemon pokemon)
   {
      /*//Alternativa sin utilizar la librería DG.Tweening:
      float currentScale = healthBar.transform.localScale.x;//Guarda el valor inicial

      float updateQuantity = currentScale - normalizedValue;//Cantidad de vida que hay que restar

      while (currentScale - normalizedValue > Mathf.Epsilon)//Utiliza Epsilon en vez de "0"
      {
         currentScale -= updateQuantity * Time.deltaTime;
         //Cambia el tamaño de la barra y el color en función de este tamaño
         healthBar.transform.localScale = new Vector3(currentScale, 1, 1);
         healthBar.GetComponent<Image>().color = BarColor;
         yield return null;//Espera a que finalice el frame actual        
         
      }
      //Al final del bucle asegura que el tamaño de la barra queda en la escala final
      healthBar.transform.localScale = new Vector3(normalizedValue, 1, 1);
       */

      //Utiliza la librería DG.Tweening para escalar la barra en el eje X e ir variando el color de forma animada

      var sequence = DOTween.Sequence();
      
      //La vida hay que pasarla con un valor entre 0 y 1, por lo que se divide la actual entre la máxima
      //Hay que forzar que el resultado dé un float para evitar que al dividir números enteros pueda ser siempre 0
      //healthBar.SetHP((float)_pokemon.Hp / _pokemon.MaxHP);
      float normalizedValue = (float) pokemon.Hp / pokemon.MaxHP;
      
      sequence.Append( healthBar.transform.DOScaleX(normalizedValue, 1f));
      sequence.Join (healthBar.GetComponent<Image>().DOColor(
         ColorManager.SharedInstance.BarColor(normalizedValue), 1f));
      //También habrá una animación que actualiza el texto con la cantidad de vida en la barra de vida
      sequence.Join(currentHPText.DOCounter(pokemon.PreviousHPValue, pokemon.Hp, 1f));
      maxHPText.text = $"/{pokemon.MaxHP}";
      yield return sequence.WaitForCompletion();

   }

   
   
   
}
