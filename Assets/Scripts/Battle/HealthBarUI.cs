using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LAS BARRAS DE VIDA DE UN POKEMON EN LA UI DE BATALLA

public class HealthBarUI : MonoBehaviour
{
   [SerializeField] [Tooltip("Imagen de la barra de vida")]
   private GameObject healthBar;

   

   /// <summary>
   /// Actualiza la barra de vida a partir del valor normalizado (entre 0 y 1) de la misma
   /// </summary>
   /// <param name="normalizedValue"Valor normalizado (entre 0 y 1) de la vida></param>
   public void SetHP(float normalizedValue)
   {
      //Modifica el tamaño de la barra de vida escalándola
      healthBar.transform.localScale = new Vector3(normalizedValue, 1, 1);
   }
}
