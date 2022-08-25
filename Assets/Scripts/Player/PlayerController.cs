using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditorInternal;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Animator))]

public class PlayerController : MonoBehaviour
{
    //Para controlar si el player se mueve o no en cada momento
    //de forma que la animación se detenga en el último estado al parar
    private bool isMoving;

    [SerializeField] [Tooltip("Velocidad de movimiento")]
    private float speed = 1;

    [SerializeField] [Tooltip("Capa/s a la/s que está/n asignado/s los Tilemap de objetos sólidos por colisión")]
    private LayerMask solidObjectsLayer;
    
    [SerializeField] [Tooltip("Capa/s a la/s que está/n asignado/s los Tilemap de zonas de aparición de pokemon")]
    private LayerMask pokemonLayer;

    //Evento de la clase Action de Unity para indicar que se ha encontrado un pokemon y ha de iniciarse la batalla
    public event Action OnPokemonEncountered;
    
    //Para guardar los valores de los ejes x/y antes de transmitirlos al player
    private Vector2 input;
    
    //Animaciones del player
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    
    /// <summary>
    /// Método que iniciará el movimiento del player en el update cuando sea invocado desde el GameManager
    /// </summary>
    public void HandleUpdate()
    {
        MovePlayer();
    }

    private void LateUpdate()
    {
        //Al finalizar todos los cálculos, establece si se debe reproducir una animación de movimiento o de idle
        _animator.SetBool("IsMoving", isMoving);
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
                
                //Se asigna valor a la variable del animator que indicará tanto la animación que se debe reproducir al
                //caminar como la dirección hacia la que quedará mirando al detenerse el personaje
                _animator.SetFloat("MoveX", input.x);
                _animator.SetFloat("MoveY", input.y);
                
                //Se calcula la posición de destino (como va dirigida al
                //transform será un Vector3 aunque estemos en 2D)
                Vector3 targetPosition = transform.position;
                targetPosition.x += input.x;
                targetPosition.y += input.y;
                
                /*Nota: este tipo de movimiento hará que el personaje se mueva siempre una unidad hacia alguno de los
                 lados. Por ello, es importante no colocar al mismo en la posición x:0, y:0 de salida en la escena,
                 ya que al finalizar cada movimiento se quedaría en medio de cada "casilla" de la grid, y por tanto
                 se quedaría también en medio de puertas, obstáculos, etc... Lo ideal será que el player salga
                 desde la posición x:0.5, y en el eje y una cantidad algo superior de forma que la parte de abajo,
                 normalmente representada por una sombra aunque dependerá del artista, quede justo en el borde
                 inferior de la "casilla" inicial del grid. En este caso concreto el valor es de y:0.7*/

                //Se utilizará una corutina para evitar movimientos bruscos
                //que surgirían si cada movimiento dura más de 1 frame,
                //lo cual es lo habitual
                if(IsAvailable(targetPosition))//Solo si está disponible la posición de destino se moverá hacia ella
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
        
        //Al terminar un movimiento comprueba si se ha entrado en una zona pokemon
        CheckForPokemon();
    }

    /// <summary>
    /// Comprueba si un punto está libre para caminar hacia él (no se va a colisionar con un
    /// objeto que esté en las capas de colisión)
    /// </summary>
    /// <param name="target">Coordenadas del punto de destino</param>
    /// <returns>true si el destino está libre / false en caso contrario</returns>
    private bool IsAvailable(Vector3 target)
    {
        //Utiliza el motor de físicas 2D para hacer la comprobación. El método más adecuado es el que utiliza
        //un círculo en vez de una caja, para intentar evitar colisiones con esquinas muy pequeñas. Comprueba si
        //un círculo con centro en la posición a la que se desea caminar, con un radio ligeramente inferior a la
        //mitad de la "caja" que representaría al player, se produciría colisión con la capa de objetos físicos
        //Nota: para que funcione correctamente, el componente Composite Collider de la capa de objetos físicos
        //debe tener la propiedad "Geometry Type" en "Polygons"
        if (Physics2D.OverlapCircle(target, 0.2f, solidObjectsLayer) != null)
            return false;

        return true;
    }

    /// <summary>
    /// Comprueba si el player se encuentra en una zona pokemon y en caso afirmativo lanza la
    /// generación aleatoria de aparición de un pokemon y por tanto una batalla
    /// </summary>
    private void CheckForPokemon()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, pokemonLayer) != null)
        {
            if (Random.Range(0, 100) < 10) //% de probabilidad de aparición de un pokemon
            {
                OnPokemonEncountered();//Activa el evento indicando que la batalla debe dar comienzo
                //TODO: Puede ser necesario detener la animación del player con IsMoving = false;

                //Debug.Log("Aparece un Pokemon. Comienza la batalla pokemon");
            }
        }
    }
}
