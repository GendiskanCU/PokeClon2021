using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA LOS TEXTOS QUE APARECERÁN EN EL RECUADRO DE DIÁLOGO GENERAL AL INTERACTUAR CON NPC U OBJETOS

public class DialogManager : MonoBehaviour
{
    [SerializeField] [Tooltip("Caja de diálogos generales")] private GameObject dialogBox;

    [SerializeField] [Tooltip("Recuadro de texto de los diálogos de la caja de diálogos generales")]
    private Text dialogText;
    
    [SerializeField] [Tooltip("Velocidad a la que se irán mostrando los mensajes, en caracteres/segundo")]
    private int charactersPerSecond = 20;
    
    //Para controlar que no se pueda hacer alguna acción hasta pasado un lapso aunque se mantenga pulsado Submit
    private float timeSinceLastClick;
    [SerializeField][Tooltip("Tiempo mínimo para poder volver a pulsar Submit")]
    private float timeBetweenClicks = 1.0f;
    
    //Diálogo actual que se debe ir mostrando
    private Dialog currentDialog;
    
    //Para conocer cuál es la siguiente línea de diálogo que se debe mostrar
    private int currentLine;
    
    //Para saber si se está escribiendo alguna línea de diálogo, para evitar comenzar otra antes de que finalice
    private bool isWriting;

    public static DialogManager SharedInstance;//Singleton


    /// <summary>
    /// Evento para indicar que un diálogo ha comenzado
    /// </summary>
    public event Action OnDialogStart;

    /// <summary>
    /// Evento para indicar que un diálogo ha finalizado
    /// </summary>
    public event Action OnDialogFinish;
    
    
    private void Awake()
    {
        //Singleton
        if (SharedInstance == null)
        {
            SharedInstance = this;
        }
    }

    /// <summary>
    /// Lleva a cabo la lógica de un diálogo con un NPC o algún objeto interactivo
    /// </summary>
    public void HandleUpdate()
    {
        //Si el player pulsa el botón Submit se irán mostrando las sucesivas líneas del diálogo
        //Se controla que pase un tiempo mínimo entre cada pulsación y que haya terminado de escribir la línea anterior
        timeSinceLastClick += Time.deltaTime;
        if (Input.GetAxisRaw("Submit") != 0 && !isWriting)
        {
            if (timeSinceLastClick >= timeBetweenClicks)
            {
                timeSinceLastClick = 0;
                //Pasa a la siguiente línea del diálogo
                currentLine++;
                if (currentLine < currentDialog.Lines.Count)//Si todavía quedaban líneas en el diálogo
                {
                    StartCoroutine(SetDialog(currentDialog.Lines[currentLine]));
                }
                else //Si el diálogo se ha terminado
                {
                    //Resetea la línea actual, preparando la variable para futuros diálogos
                    currentLine = 0;
                    //Desactiva la caja del diálogo
                    dialogBox.SetActive(false);
                    //Invoca el evento que indica que el diálogo ha finalizado. El GameManager volverá a cambiar
                    //el estado del juego para que ya no se ejecute este método HandleUpdate
                    OnDialogFinish?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Inicia un diálogo mostrando la primera línea del mismo
    /// </summary>
    /// <param name="dialog"></param>
    public void ShowDialog(Dialog dialog)
    {
        //Invoca el evento que indica que el diálogo ha dado comienzo, si al mismo hay alguien suscrito
        //En este caso, el GameManager cambiará el estado del juego para que comience a ejecutarse HandleUpdate
        OnDialogStart?.Invoke();
        
        //Muestra el cuadro de diálogo
        dialogBox.SetActive(true);
        
        //Guarda el diálogo actual y muestra la primera línea del diálogo (currentLine será 0 al llegar aquí
        //porque al terminar cualquier diálogo anterior se resetea)
        currentDialog = dialog;
        StartCoroutine(SetDialog(currentDialog.Lines[currentLine]));
    }
    
    
    /// <summary>
    /// Muestra una línea de diálogo letra a letra en la caja de texto del diálogo
    /// </summary>
    /// <param name="line">Línea de mensaje a mostrar</param>
    /// <returns></returns>
    public IEnumerator SetDialog(string line)
    {
        //Indica que se está escribiendo una línea
        isWriting = true;
        
        dialogText.text = "";
        foreach (var character in line)
        {
            dialogText.text += character;
            //Reproduce un sonido aleatorio al escribir cada carácter, excepto en los espacios en blanco
            if (character != ' ')
            {
                SoundManager.SharedInstance.PlayRandomCharacterSound();
            }
            yield return new WaitForSeconds(1.0f / charactersPerSecond);
        }
        //Indica que ha finalizado de escribir la línea
        isWriting = false;
    }
}
