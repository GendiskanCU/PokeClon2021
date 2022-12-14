using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrainerController : MonoBehaviour, Interactable
{
    [SerializeField] [Tooltip("Sprite para representar un símbolo de exclamación")]
    private GameObject exclamationMessage;

    [SerializeField] [Tooltip("Campo de visión del entrenador")]
    private GameObject fov;

    [SerializeField] [Tooltip("Diálogo del entrenador pokemon antes de perder la batalla")]
    private Dialog dialog;

    [SerializeField] [Tooltip("Diálogo del entrenador pokemon después de perder la batalla")]
    private Dialog afterLostDialog;

    [SerializeField] [Tooltip("Sprite que representa al entrenador pokemon en una batalla contra entrenador")]
    private Sprite trainerSprite;
    public Sprite TrainerSprite => trainerSprite;

    [SerializeField] [Tooltip("Nombre del entrenador pokemon")]
    private String trainerName;
    public string TrainerName => trainerName;

    //Para controlar si el entrenador ya ha perdido la batalla, por lo que ya no se deberá repetir
    private bool trainerLostBattle;
    
    private Character character;


    //Implementación de la interface Interactable
    public void Interact(Vector3 source)
    {
        //Gira al entrenador hacia la posición de la fuente con la que se va a interactuar
        character.LookTowards(source);
        
        if (!trainerLostBattle)
        {
            //Al interactuar con un entrenador que aún no ha perdido la batalla
            StartCoroutine(ShowExclamationMark()); //Muestra una exclamación
            
            //El trainer abre su diálogo prebatalla con el player
            DialogManager.SharedInstance.ShowDialog(dialog, () =>
            {
                //Al finalizar el diálogo notifica al GameManager que la batalla con éste entrenador dé comienzo
                GameManager.SharedInstance.StartTrainerBattle(this);
            });
        }
        else //Si el entrenador ya ha sido derrotado muestra el diálogo alternativo sin hacer nada más
        {
            DialogManager.SharedInstance.ShowDialog(afterLostDialog);
        }
    }
    
    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        //Ajusta el campo de visión a la dirección hacia la que mira el personaje por defecto
        SetFovDirection(character.Animator.DefaultDirection);
    }


    private void Update()
    {
        character.HandleUpdate();
    }


    /// <summary>
    /// Inicia la batalla con un entrenador pokemon cuando éste detecta al player dentro de campo de visión
    /// </summary>
    /// <param name="player">El player</param>
    /// <returns></returns>
    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        //El trainer muestra un mensaje de exclamación al detectar al player
        yield return ShowExclamationMark();
        
        //El trainer se moverá hacia el player
        //Se calcula el vector de movimiento como la diferencia entre posiciones de ambos
        var difference = player.transform.position - transform.position;
        
        //Le restamos una unidad para que el trainer se detenga una posición antes del alcanzar al player
        var moveVector = difference - difference.normalized;
        
        //Se redondea para asegurar que resultan valores enteros
        moveVector = new Vector2(Mathf.RoundToInt(moveVector.x), Mathf.RoundToInt(moveVector.y));
        //El trainer se mueve a la posición calculada. La corutina espera a que llegue al destino
        yield return character.MoveToward(moveVector);
        
        //El trainer abre su diálogo con el player
        DialogManager.SharedInstance.ShowDialog(dialog, () =>
        {
            //Al finalizar el diálogo notifica al GameManager que la batalla con éste entrenador dé comienzo
            GameManager.SharedInstance.StartTrainerBattle(this);
        });
    }

    /// <summary>
    /// Coloca adecuadamente el campo de visión del entrenador, en función de la dirección hacia la que está mirando
    /// </summary>
    /// <param name="direction">Dirección hacia la que mira (enumerado declarado en CharacterAnimator)</param>
    public void SetFovDirection(FacingDirection direction)
    {
        //Se calculan los grados del ángulo de rotación
        float angle = 0f;//Valor por defecto, que se mantendrá si mira hacia abajo
        
        if (direction == FacingDirection.Right)
        {
            angle = 90f;
        }
        else if (direction == FacingDirection.Up)
        {
            angle = 180f;
        }
        else if (direction == FacingDirection.Left)
        {
            angle = 270f;
        }
        
        //Rota el fov en el eje Z, utilizando eulerAngles ya que vamos utilizar datos directamente en grados
        fov.transform.eulerAngles = new Vector3(0, 0, angle);

    }


    /// <summary>
    /// Muestra un símbolo de exclamación sobre el personaje durante un breve momento
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowExclamationMark()
    {
        exclamationMessage.SetActive(true);
        yield return new WaitForSeconds(0.6f);
        exclamationMessage.SetActive(false);
    }


    /// <summary>
    /// Realiza las acciones necesarias cuando un entrenador pokemon pierde la batalla contra el player
    /// </summary>
    public void AfterTrainerLostBattle()
    {
        //Para evitar que el trainer pueda iniciar una nueva batalla con el player, se desactiva su FOV
        //Y se cambia el estado de la booleana
        fov.gameObject.SetActive(false);
        trainerLostBattle = true;
    }
}
