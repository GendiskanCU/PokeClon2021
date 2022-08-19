using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//GESTIONA LA INFORMACIÓN QUE APARECERÁ EN LA CAJA DE DIÁLOGO DE LA PARTE INFERIOR EN LA BATALLA POKEMON
public class BattleDialogBox : MonoBehaviour
{
    [SerializeField] [Tooltip("Texto que contendrá el diálogo")]
    private Text dialogText;
    
    [SerializeField] [Tooltip("Velocidad a la que se irán mostrando los mensajes, en caracteres/segundo")]
    private float charactersPerSecond;


    /// <summary>
    /// Muestra un mensaje letra a letra en la caja de texto de la batalla pokemon
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    /// <returns></returns>
    public IEnumerator SetDialog(string message)
    {
        dialogText.text = "";
        foreach (var character in message)
        {
            dialogText.text += character;
            yield return new WaitForSeconds(1 / charactersPerSecond);
        }
    }
}
