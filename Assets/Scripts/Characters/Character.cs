using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LA LÓGICA COMÚN DE CUALQUIER PERSONAJE DEL JUEGO (PLAYER, NPC, ETC.) COMO LA DEL MOVIMIENTO

public class Character : MonoBehaviour
{
    [SerializeField] [Tooltip("Velocidad de movimiento del personaje")]
    private float speed = 1;
    
    //Animator personalizado del personaje
    private CharacterAnimator _animator;
    public CharacterAnimator Animator => _animator;
    
    //Para controlar si el personaje se está moviendo
    private bool isMoving;
    public bool IsMoving => isMoving;


    private void Awake()
    {
        _animator = GetComponent<CharacterAnimator>();
    }

    
    
   /// <summary>
   /// Mueve de forma progresiva al personaje hacia un punto de destino
   /// </summary>
   /// <param name="moveVector">Vector de movimiento 2D del personaje</param>
   /// <param name="OnMoveFinish">Acción opcional a realizar por el personaje al finalizar un movimiento</param>
   /// <returns></returns>
    public IEnumerator MoveToward(Vector2 moveVector, Action OnMoveFinish = null)
    {
        /*Nota: este tipo de movimiento hará que el personaje se mueva siempre una unidad hacia alguno de los
        lados. Por ello, es importante no colocar al mismo en la posición x:0, y:0 de salida en la escena,
        ya que al finalizar cada movimiento se quedaría en medio de cada "casilla" de la grid, y por tanto
        se quedaría también en medio de puertas, obstáculos, etc... Lo ideal será que el player salga
        desde la posición x:0.5, y en el eje y una cantidad algo superior de forma que la parte de abajo,
        normalmente representada por una sombra aunque dependerá del artista, quede justo en el borde
        inferior de la "casilla" inicial del grid. En este caso concreto el valor es de y:0.7*/
        //Se utilizará una corutina para evitar movimientos bruscos que surgen si cada movimiento dura más de 1 frame,
        //lo cual es lo habitual
        
        //Se asigna valor a la variable del animator que indicará la animación que se debe reproducir al caminar
        //manteniéndola siempre entre -1 y 1, que son los valores adecuados para el animator
        /*_animator.SetFloat("MoveX", input.x);
        _animator.SetFloat("MoveY", input.y);  //Esto es con el Animator de Unity, se usará el personalizado:*/
        _animator.MoveX = Mathf.Clamp(moveVector.x, -1, 1);
        _animator.MoveY = Mathf.Clamp( moveVector.y, -1, 1);
                
        //Se calcula la posición de destino (como va dirigida al
        //transform será un Vector3 aunque estemos en 2D)
        Vector3 targetPosition = transform.position;
        targetPosition.x += moveVector.x;
        targetPosition.y += moveVector.y;

        if (!IsAvailable(targetPosition))
        {
            //Si la posición de destino calculada no está disponible saldrá de la corutina sin hacer nada más
            yield break;
        }
        
        isMoving = true; //Indica que se inicia el movimiento
        
        //Utiliza un bucle while que se ejecutará mientras no se haya
        //alcanzado el destino. En vez de comparar con > 0 se utiliza
        //> Mathf.Epsilon, que nos devolverá el valor de precisión del
        //propio hardware donde se ejecuta el juego, que no es 0 exacto
        //aunque sí un valor muy pequeño.
        while (Vector3.Distance(transform.position, targetPosition)
               > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                targetPosition, speed * Time.deltaTime);

            //Espera al siguiente frame sin hacer nada más
            yield return null;
        }

        //Al salir del bucle, como habíamos comparado con el valor de precición
        //en vez del cero absoluto, asegura que la posición coincida con el destino
        //de forma exacta
        transform.position = targetPosition;

        isMoving = false; //Indica que se finaliza el movimiento
        
        //Al finalizar un movimiento se invoca la posible acción a realizar por el personaje
        OnMoveFinish?.Invoke();
    }


   public void HandleUpdate()
   {
       //Sincroniza el estado de movimiento del personaje con su animator
       _animator.IsMoving = isMoving;
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
       //Comprueba igualmente si el movimiento produciría colisión con algún NPC u objeto que pertenezca
       //a la capa de objetos con los que el player puede interactuar
       if (Physics2D.OverlapCircle(target, 0.2f, GameLayers.SharedInstance.SolidObjectsLayer |
                                                 GameLayers.SharedInstance.InteractableLayer) != null)
       {
           return false;
       }

       return true;
   }
}
