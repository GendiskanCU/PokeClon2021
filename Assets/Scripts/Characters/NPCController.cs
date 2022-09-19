using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{

    [SerializeField] [Tooltip("Diálogo del NPC")]
    private Dialog dialog;
    
    //Implementación de la interface Interactable
    public void Interact()
    {
        //Se muestra el diálog del NPC
        DialogManager.SharedInstance.ShowDialog(dialog);
    }
}
