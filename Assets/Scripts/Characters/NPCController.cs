using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{

    [SerializeField] [Tooltip("Diálogo del NPC")]
    private Dialog dialog;

    //Componente que controla el movimiento y otras posibles acciones del personaje
    private Character character;

    //Animator del NPC de la clase CustomAnimator
    private CustomAnimator animator;
    
    
    //Implementación de la interface Interactable
    public void Interact()
    {
        //Se muestra el diálogo del NPC
        //DialogManager.SharedInstance.ShowDialog(dialog);

        StartCoroutine(character.MoveToward(new Vector2(0, 1)));
    }


    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Update()
    {
        character.HandleUpdate();
    }
}
