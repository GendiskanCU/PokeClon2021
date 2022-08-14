using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditorInternal;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    //Para controlar si el player se mueve o no en cada momento
    //de forma que la animación se detenga en el último estado al parar
    private bool isMoving;

    [SerializeField] [Tooltip("Velocidad de movimiento")]
    private float speed = 1;
    
    //Para guardar los valores de los ejes x/y antes de transmitirlos al player
    private Vector2 input;

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        //El player no iniciará un nuevo movimiento hasta finalizar el actual
        //para lograr lo cual se utilizará también la booleana isMoving
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");//Valor 0 ó 1 (GetAxisRaw)
            input.y = Input.GetAxisRaw("Vertical");//Valor 0 ó 1 (GetAxisRaw)
            
            //Solo habrá movimiento si alguno de los dos ejes no es cero
            if (input != Vector2.zero)
            {
                //El player no se moverá en diagonal, por lo que se opta por
                //anular el movimiento en vertical cuando ya lo haya en horizontal
                if (input.x != 0)
                    input.y = 0;
                
                //Se calcula la posición de destion (como va dirigida al
                //transform será un Vector3 aunque estemos en 2D)
                Vector3 targetPosition = transform.position;
                targetPosition.x += input.x;
                targetPosition.y += input.y;

                //Se utilizará una corutina para evitar movimientos bruscos
                //que surgirían si cada movimiento dura más de 1 frame,
                //lo cual es lo habitual
                StartCoroutine(MoveToward(targetPosition));
            }
        }
    }

    /// <summary>
    /// Mueve de forma progresiva al player al punto de destino
    /// </summary>
    /// <param name="destination">punto de destino</param>
    /// <returns></returns>
    private IEnumerator MoveToward(Vector3 destination)
    {
        isMoving = true; //Se inicia el movimiento
        
        //Utiliza un bucle while que se ejecutará mientras no se haya
        //alcanzado el destino. En vez de comparar con > 0 se utiliza
        //> Mathf.Epsilon, que nos devolverá el valor de precisión del
        //propio hardware donde se ejecuta el juego, que no es 0 exacto
        //aunque sí un valor muy pequeño.
        while (Vector3.Distance(transform.position, destination)
               > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                destination, speed * Time.deltaTime);

            //Espera al siguiente frame sin hacer nada más
            yield return null;
        }

        //Al salir del bucle, como habíamos comparado con el valor de precición
        //en vez del cero absoluto, asegura que la posición coincida con el destino
        //de forma exacta
        transform.position = destination;

        isMoving = false; //Se finaliza el movimiento
    }
}
