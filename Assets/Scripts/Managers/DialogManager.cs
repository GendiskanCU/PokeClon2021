using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LOS TEXTOS QUE APARECERÁN EN EL RECUADRO DE DIÁLOGO GENERAL AL INTERACTUAR CON NPC U OBJETOS

public class DialogManager : MonoBehaviour
{
    [SerializeField] [Tooltip("Caja de diálogos generales")] private GameObject dialogBox;

    [SerializeField] [Tooltip("Recuadro de texto de los diálogos de la caja de diálogos generales")]
    private Text dialogText;
    
    [SerializeField] [Tooltip("Velocidad a la que se irán mostrando los mensajes, en caracteres/segundo")]
    private float charactersPerSecond = 0.2f;


    public void ShowDialog(Dialog dialog)
    {
        //Muestra el cuadro de diálogo
        dialogBox.SetActive(true);
        
        //Muestra la primera línea del diálogo
        StartCoroutine(SetDialog(dialog.Lines[0]));
    }
    
    
    /// <summary>
    /// Muestra un mensaje letra a letra en la caja de texto del diálogo
    /// </summary>
    /// <param name="line">Línea de mensaje a mostrar</param>
    /// <returns></returns>
    public IEnumerator SetDialog(string line)
    {
        dialogText.text = "";
        foreach (var character in line)
        {
            dialogText.text += character;
            //Reproduce un sonido aleatorio al escribir cada carácter, excepto en los espacios en blanco
            if (character != ' ')
            {
                SoundManager.SharedInstance.PlayRandomCharacterSound();
            }
            yield return new WaitForSeconds(1 / charactersPerSecond);
            
        }
    }
}
