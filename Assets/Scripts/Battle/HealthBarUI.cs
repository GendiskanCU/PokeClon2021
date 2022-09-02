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

   
   /// <summary>
   /// Devuelve el color de la barra de vida según la cantidad de vida indicada
   /// </summary>
   /// <param name="finalScale">Cantidad de vida, normalizada en término de escala de la barra (de 0 a 1)</param>
   /// <returns></returns>
   public Color BarColor (float finalScale)
   {
      if (finalScale < 0.15f)
      {
         return new Color(195f / 255, 53f / 255, 23 / 255f);
      }
      else if (finalScale < 0.5f)
      {
         return new Color(229f / 255, 154f / 255, 44f / 255);
      }
      else
         {
            return new Color(114f/255, 207f/255, 131f/255);
         }
      
   }

   /// <summary>
   /// Actualiza la barra de vida a partir del valor normalizado (entre 0 y 1) de la misma
   /// </summary>
   /// <param name="normalizedValue"Valor normalizado (entre 0f y 1f) de la vida></param>
   public void SetHP(float normalizedValue)
   {
      //Modifica el tamaño de la barra de vida escalándola
      healthBar.transform.localScale = new Vector3(normalizedValue, 1, 1);
      
      //Cambia el color de la barra en función de su tamaño actual
      healthBar.GetComponent<Image>().color = BarColor(normalizedValue);
   }


   /// <summary>
   /// Actualiza la barra de vida de forma progresiva a partir del valor normalizado de la misma
   /// </summary>
   /// <param name="newHP">Cantidad de vida normalizada (entre 0f y 1f)</param>
   /// <returns></returns>
   public IEnumerator SetSmoothHP(float normalizedValue)
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
      
      sequence.Append( healthBar.transform.DOScaleX(normalizedValue, 1f));
      sequence.Join (healthBar.GetComponent<Image>().DOColor(BarColor(normalizedValue), 1f));
      yield return sequence.WaitForCompletion();
   }
   
   
}
