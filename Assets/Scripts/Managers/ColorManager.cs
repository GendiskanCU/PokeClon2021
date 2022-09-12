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

    
    /// <summary>
    /// Devuelve el color de la barra de vida según la cantidad de vida indicada
    /// </summary>
    /// <param name="finalScale">Cantidad de vida, normalizada en término de escala de la barra (de 0 a 1)</param>
    /// <returns></returns>
    public Color BarColor (float finalScale)
    {
        if (finalScale < 0.2f)
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
    /// Devuelve el color para el texto de los Puntos de Poder del pokemon
    /// </summary>
    /// <param name="finalScale">Cantidad de PP restantes en términos porcentuales (de 0 a 1)</param>
    /// <returns></returns>
    public Color PPColor (float finalScale)
    {
        if (finalScale < 0.2f)
        {
            return new Color(195f / 255, 53f / 255, 23 / 255f);
        }
        else if (finalScale < 0.5f)
        {
            return new Color(229f / 255, 154f / 255, 44f / 255);
        }
        else
        {
            return Color.black;
        }
      
    }
    
    
    //Clase interna con la matriz de colores de cada tipo de pokemon
    public class TypeColor
    {
        private static Color[] colors =
        {
            Color.white, //NINGUNO
            new Color(0.8734059f, 0.8773585f, 0.8235582f), //NORMAL
            new Color(0.990566f, 0.5957404f, 0.5279903f), //FUEGO
            new Color(0.5613208f, 0.7828107f, 1), //AGUA
            new Color(0.9942768f, 1f, 0.5707547f), //ELECTRICO
            new Color(0.4103774f, 1, 0.6846618f), //HIERBA
            new Color(0.7216981f, 0.9072328f, 1), //HIELO
            new Color(0.735849f, 0.5600574f, 0.5310609f), //LUCHA
            new Color(0.6981132f, 0.4774831f, 0.6539872f), //VENENO
            new Color(0.9433962f, 0.7780005f, 0.5562478f), //TIERRA
            new Color(0.7358491f, 0.7708895f, 0.9811321f), //AEREO
            new Color(1, 0.6650944f, 0.7974522f), //PSIQUICO
            new Color(0.8193042f, 0.9333333f, 0.5254902f), //BICHO
            new Color(0.8584906f, 0.8171859f, 0.6519669f), //ROCA
            new Color(0.6094251f, 0.6094251f, 0.7830189f), //FANTASMA
            new Color(0.6556701f, 0.5568628f, 0.7647059f), //DRAGON
            new Color(0.735849f, 0.6178355f, 0.5588287f), //OSCURO
            new Color(0.7889819f, 0.7889819f, 0.8490566f), //ACERO
            new Color(0.9339623f, 0.7621484f, 0.9339623f) //HADA
        };
        

        /// <summary>
        /// Devuelve el color característico de un tipo de pokemon
        /// </summary>
        /// <param name="type">Tipo de pokemon del que queremos obtener el color característico</param>
        /// <returns>Color característico del tipo de pokemon</returns>
        public static Color GetColorFromType(PokemonType type)   //Estática para que no haga falta crear instancia
        {
            return colors[(int)type];
        }
    }

    /// <summary>
    /// Clase interna con un diccionario que contendrá, para cada estado alterado que pueda afectar a un pokemon,
    /// el color correspondiente que se mostrará en la UI de batalla para mostrar este hecho
    /// </summary>
    public class StatusConditionColor
    {
        private static Dictionary<StatusConditionID, Color> colors = new Dictionary<StatusConditionID, Color>()
        {
            {StatusConditionID.none, Color.white}, //Ninguno
            {StatusConditionID.brn, new Color(223f / 255, 134f / 255, 67f / 255)}, //Quemado
            {StatusConditionID.frz, new Color(168f / 255, 214f / 255, 215f / 255)}, //Congelado
            {StatusConditionID.par, new Color(241f / 255, 208f / 255, 83f / 255)}, //Paralizado
            {StatusConditionID.psn, new Color(147f / 255, 73f / 255, 156f / 255)}, //Envenenado
            {StatusConditionID.slp, new Color(163f / 255, 147f / 255, 234f / 255)}  //Dormido
        };

        /// <summary>
        /// Obtiene el color correspondiente a una Status Condition (estado alterado) de un pokemon
        /// </summary>
        /// <param name="id">La clave de la Status Condition</param>
        /// <returns></returns>
        public static Color GetColorFromStatusCondition(StatusConditionID id)
        {
            return colors[id];
        }
    }
    
}


