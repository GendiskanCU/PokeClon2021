using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{
    //Implementación de la interface Interactable
    public void Interact()
    {
        Debug.Log("Podemos hablar con el NPC");
    }

}
