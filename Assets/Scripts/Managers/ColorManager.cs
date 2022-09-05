using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LOS DIVERSOS COLORES QUE SE UTILIZARÁN EN LA ESCENA

public class ColorManager : MonoBehaviour
{
    //La instancia de esta clase
    public static ColorManager SharedInstance;
    
    [SerializeField][Tooltip("Color que utilizado para resaltar elementos seleccionados en la UI")]
    Color selectedColor;
    public Color SelectedColor => selectedColor;


    private void Awake()
    {
        SharedInstance = this;
    }

    
}
