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

    [SerializeField] [Tooltip("Lista de sprites de la animación del NPC")]
    private List<Sprite> sprites;

    //Animator del NPC de la clase CustomAnimator
    private CustomAnimator animator;
    
    //Implementación de la interface Interactable
    public void Interact()
    {
        //Se muestra el diálog del NPC
        DialogManager.SharedInstance.ShowDialog(dialog);
    }


    private void Start()
    {
        //Se crea el animator personalizado y se inicializa el sistema de animaciones
        animator = new CustomAnimator(GetComponent<SpriteRenderer>(), sprites);
        animator.Start();
    }


    private void Update()
    {
        //Va reproduciendo las animaciones del NPC
        animator.HandleUpdate();
    }
}
