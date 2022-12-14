using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//GESTIONA LA INFORMACIÓN QUE APARECERÁ EN LA CAJA DE DIÁLOGO DE LA PARTE INFERIOR EN LA BATALLA POKEMON
public class BattleDialogBox : MonoBehaviour
{
    [SerializeField] [Tooltip("Texto que contendrá el diálogo")]
    private Text dialogText;

    [SerializeField] [Tooltip("Área de selección de la acción (luchar, huir)")]
    private GameObject actionSelect;

    [SerializeField] [Tooltip("Área de selección de movimiento o ataque")]
    private GameObject moveSelect;

    [SerializeField] [Tooltip("Área de descripción del movimiento o ataque")]
    private GameObject moveDescription;

    [SerializeField] [Tooltip("Área de elección Sí/No")]
    private GameObject yesNoBox;
    
    
    
    [SerializeField] [Tooltip("Textos de las acciones disponibles")]
    private List<Text> actionTexts;

    [SerializeField] [Tooltip("Textos de los movimientos disponibles")]
    private List<Text> movementTexts;

    [SerializeField] [Tooltip("Texto que mostrará los PP disponibles/totales del movimiento seleccionado")]
    private Text ppText;
    
    [SerializeField] [Tooltip("Texto descriptivo del movimiento seleccionado")]
    private Text typeText;

    [SerializeField] [Tooltip("Texto del Sí de la caja Sí/No")]
    private Text yesText;

    [SerializeField] [Tooltip("Texto del No de la caja Sí/No")]
    private Text noText;
    
    [SerializeField] [Tooltip("Velocidad a la que se irán mostrando los mensajes, en caracteres/segundo")]
    private float charactersPerSecond;

    private bool isWriting = false;
    public bool IsWriting => isWriting;


    /// <summary>
    /// Muestra un mensaje letra a letra en la caja de texto del diálogo de la batalla pokemon
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    /// <returns></returns>
    public IEnumerator SetDialog(string message)
    {
        //Indica que se está "escribiendo" en el texto de diálogo, para evitar que se intente otra escritura
        //hasta que la actual haya finalizado
        isWriting = true;
        
        dialogText.text = "";
        foreach (var character in message)
        {
            dialogText.text += character;
            //Reproduce un sonido aleatorio al escribir cada carácter, excepto en los espacios en blanco
            if (character != ' ')
            {
                SoundManager.SharedInstance.PlayRandomCharacterSound();
            }
            yield return new WaitForSeconds(1 / charactersPerSecond);
        }
        
        //Hace una pausa final de un segundo
        yield return new WaitForSeconds(1.0f);
        
        //Indica que la escritura ya ha finalizado
        isWriting = false;
    }

    /// <summary>
    /// Activa/desactiva el panel de texto del diálogo de batalla
    /// </summary>
    /// <param name="activated">Nuevo estado del panel de texto (true:activado/false:desactivado)</param>
    public void ToggleDialogText(bool activated)
    {
        dialogText.enabled = activated;
    }

    /// <summary>
    /// Activa/desactiva el panel de acciones en la batalla
    /// </summary>
    /// <param name="activated">Nuevo estado del panel (true:activado/false:desactivado)</param>
    public void ToggleActions(bool activated)
    {
        actionSelect.SetActive(activated);
    }

    /// <summary>
    /// Activa/desactiva el panel de movimientos o ataques en la batalla, así como el de su descripción
    /// </summary>
    /// <param name="activated">Nuevo estado del panel (true:activado/false:desactivado)</param>
    public void ToggleMovements(bool activated)
    {
        moveSelect.SetActive(activated);
        moveDescription.SetActive(activated);
    }


    /// <summary>
    /// Activa/desactiva la caja de elección "Sí/No"
    /// </summary>
    /// <param name="activated">nuevo estado de activación</param>
    public void ToggleYesNoBox(bool activated)
    {
        yesNoBox.SetActive(activated);
    }

    /// <summary>
    ///  Resalta en un color diferente la acción seleccionada por el player en el panel de acciones
    /// </summary>
    /// <param name="selectedAction">Posición de la acción seleccionada en la lista de posiciones</param>
    public void SelectAction(int selectedAction)
    {
        for (int i = 0; i < actionTexts.Count; i++)
        {
            actionTexts[i].color = i == selectedAction ? ColorManager.SharedInstance.SelectedColor : 
                ColorManager.SharedInstance.DefaultColor;;
        }
    }

    /// <summary>
    /// Rellena el panel de elección de movimientos o ataques con el nombre de los que puede ejecutar el pokemon
    /// </summary>
    /// <param name="moves">Lista de movimientos que el pokemon puede ejecutar</param>
    public void SetPokemonMovements(List<Move> moves)
    {
        for (int i = 0; i < movementTexts.Count; i++)
        {
            if (i < moves.Count)//Solo añadirá los movimientos del pokemon, que pueden ser menos que los que puede tener el panel
            {
                movementTexts[i].text = moves[i].Base.AttackName;
            }
            else
            {
                movementTexts[i].text = "---";//Los demás huecos se representarán como vacíos con unas rayas
            }
        }
    }
    
    /// <summary>
    /// Resalta en un color diferente el ataque seleccionado por el player en el panel de movimientos o ataques
    /// y actualiza la información con los PP y tipo del movimiento seleccionado en el HUD
    /// </summary>
    /// <param name="selectedMovement">Posición del ataque seleccionado en la lista de movimientos</param>
    /// <param name="move">Movimiento o ataque seleccionado</param>
    public void SelectMovement(int selectedMovement, Move move)
    {
        for (int i = 0; i < movementTexts.Count; i++)
        {
            movementTexts[i].color = i == selectedMovement ? ColorManager.SharedInstance.SelectedColor : 
                    ColorManager.SharedInstance.DefaultColor;
        }
        
        //Actualiza la información con los PP y tipo
        ppText.text = String.Format("PP {0}/{1}", move.Pp, move.Base.PP);
        
        typeText.text = move.Base.Type.ToString();
        
        //Se modifica el color de fondo del texto del ataque en función de su tipo
        moveDescription.GetComponent<Image>().color = ColorManager.TypeColor.GetColorFromType(move.Base.Type);
        
        //Se modifica el color del texto los PP en la UI en función del % de PP restantes
        ppText.color = ColorManager.SharedInstance.PPColor((float)move.Pp / move.Base.PP);
    }
    
    
    /// <summary>
    /// Resalta en un color diferente la opción seleccionada por el player en la caja de "Sí/No"
    /// </summary>
    /// <param name="yesSelected">Para indicar si el Yes está seleccionado</param>
    public void SelectYesNoAction(bool yesSelected)
    {
        if (yesSelected)
        {
            yesText.color = ColorManager.SharedInstance.SelectedColor;
            noText.color = ColorManager.SharedInstance.DefaultColor;
        }
        else
        {
            noText.color = ColorManager.SharedInstance.SelectedColor;
            yesText.color = ColorManager.SharedInstance.DefaultColor;
        }
    }
}
