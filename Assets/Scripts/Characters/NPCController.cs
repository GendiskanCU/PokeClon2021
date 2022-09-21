using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

//Estados en los que se puede encontrar el NPC
public enum NpcState
{
    Idle, //Quieto
    Walking //Andando
}

public class NPCController : MonoBehaviour, Interactable
{

    [SerializeField] [Tooltip("Diálogo del NPC")]
    private Dialog dialog;

    //Componente que controla el movimiento y otras posibles acciones del personaje
    private Character character;

    //Animator del NPC de la clase CustomAnimator
    private CustomAnimator animator;
    
    //Estado actual del NPC
    private NpcState state;
    
    [SerializeField] [Tooltip("Tiempo que el NPC permanecerá parado al llegar a un waypoint," +
                              " antes de dirigirse al siguiente")] private float idleTime;
    //Temporizador del tiempo que el NPC ha permanecido parado
    private float idleTimer = 0f;

    [SerializeField] [Tooltip("Lista de direcciones por las que se moverá el NPC")]
    private List<Vector2> moveDirections;

    //Dirección o waypoint actual
    private int currentDirection;
    
    
    //Implementación de la interface Interactable
    public void Interact()
    {
        //Se muestra el diálogo del NPC solo si éste está parado
        if(state == NpcState.Idle)
            DialogManager.SharedInstance.ShowDialog(dialog);
    }


    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Update()
    {
        MoveNPC();
        character.HandleUpdate();
    }

    /// <summary>
    /// Implanta el movimiento del NPC, haciendo una pausa cada vez que alcanza un punto de destino
    /// </summary>
    private void MoveNPC()
    {
        if (!DialogManager.SharedInstance.IsBeingShow)//Solo si el NPC no está mostrando el cuadro de diálogo andará
        {
            if (state == NpcState.Idle) //Cuando el NPC esté parado
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleTime) //Al superar el tiempo que debe permanecer parado
                {
                    idleTimer = 0f; //Resetea el cronómetro
                    //Inicia el siguiente paso
                    StartCoroutine(Walk());
                }
            }
        }
    }

    
    /// <summary>
    /// Hace caminar al NPC un paso hacia la siguiente dirección, si están definidas, o en dirección aleatoria
    /// </summary>
    /// <returns></returns>
    private IEnumerator Walk()
    {
        //Punto de destino
        Vector2 direction = Vector2.zero;
        
        //Cambia el estado del NPC indicando que ha iniciado un movimiento
        state = NpcState.Walking;
        
        //El NPC se dirigirá hacia la siguiente dirección, si hay direcciones definidas,
        //o hacia una dirección aleatoria en caso contrario
        if (moveDirections.Count > 0)
        {
            direction = moveDirections[currentDirection];
            //Cambia currentDirection de cara al próximo movimiento, manteniéndolo dentro de los límites de la lista
            currentDirection = (currentDirection + 1) % moveDirections.Count;
        }
        else
        {
            direction = new Vector2(Random.Range(1, 2), Random.Range(1, 2));
        }

        //Ejecuta el próximo paso en la dirección de destino
        yield return character.MoveToward(direction);
        
        //Cambia el estado del NPC indicando que se ha detenido
        state = NpcState.Idle;
    }
}
