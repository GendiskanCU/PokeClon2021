using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerController : MonoBehaviour
{
    [SerializeField] [Tooltip("Sprite para representar un símbolo de exclamación")]
    private GameObject ExclamationMessage;

    [SerializeField] [Tooltip("Diálogo del entrenador pokemon")]
    private Dialog dialog;
    
    private Character character;


    private void Awake()
    {
        character = GetComponent<Character>();
    }


    /// <summary>
    /// Inicia la batalla con un entrenador pokemon cuando éste detecta al player dentro de campo de visión
    /// </summary>
    /// <param name="player">El player</param>
    /// <returns></returns>
    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        //El trainer muestra un mensaje de exclamación al detectar al player
        ExclamationMessage.SetActive(true);
        yield return new WaitForSeconds(0.6f);
        ExclamationMessage.SetActive(false);
        
        //El trainer se moverá hacia el player
        //Se calcula el vector de movimiento como la diferencia entre posiciones de ambos
        Vector3 difference = player.transform.position - transform.position;
        //Le restamos una unidad para que el trainer se detenga una posición antes del alcanzar al player
        Vector3 moveVector = difference - difference.normalized;
        //Se redondea para asegurar que resultan valores enteros
        moveVector = new Vector2(Mathf.RoundToInt(moveVector.x), Mathf.RoundToInt(moveVector.y));
        //El trainer se mueve a la posición calculada. La corutina espera a que llegue al destino
        yield return character.MoveToward(moveVector);
        
        //El trainer abre su diálogo con el player
        DialogManager.SharedInstance.ShowDialog(dialog, () =>
        {
            //TODO: falta implementar el inicio de la batalla con el entrenador pokemon cuando finalice el diálogo
        });
    }
}
