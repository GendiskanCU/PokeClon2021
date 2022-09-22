using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// GESTIONA LA LÓGICA COMÚN DE CUALQUIER PERSONAJE DEL JUEGO (PLAYER, NPC, ETC.) COMO LA DEL MOVIMIENTO
/// </summary>
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

        //Para evitar movimientos en diagonal, se da prevalencia al movimiento en el eje X. Si hay movimiento en
        //ese eje, se anula el posible movimiento en el eje Y
        if (moveVector.x != 0)
        {
            moveVector.y = 0;
        }

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

        if (!IsPathAvailable(targetPosition))
        {
            //Si la ruta hasta el destino calculado no está disponible porque hay algún obstáculo entre medias
            //saldrá de la corutina sin hacer nada más
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


   /// <summary>
   /// Cambia la orientación del personaje, girándolo para que "mire" hacia el objetivo
   /// </summary>
   /// <param name="target">Objetivo hacia el que debe girar el personaje</param>
   public void LookTowards(Vector3 target)
   {
       //Calcula hacia dónde se debe girar, calculando la diferencia entre la posición hacia la que mirar y
       //la posición del personaje
       Vector3 difference = target - transform.position;
       
       //Guarda el valor X e Y por separado, redondeándolos a número entero
       int xDiff = Mathf.FloorToInt(difference.x);
       int yDiff = Mathf.FloorToInt(difference.y);

       //En principio, se ha diseñado el juego para que ningún personaje pueda moverse en diagonal, pero por si
       //se ha cometido algún error, aquí aseguramos que es así de forma que solo se actúa si alguna de las dos
       //coordenadas es cero, indicando que la dirección va solo hacia uno de los cuatro puntos cardinales
       if (xDiff == 0 | yDiff == 0)
       {
           //Se establece la dirección hacia la que el personaje debe mirar y se traslada a su animator
           _animator.MoveX = Mathf.Clamp(xDiff, -1, 1);
           _animator.MoveY = Mathf.Clamp(yDiff, -1, 1);
       }
       else
       {
           Debug.LogError("Error de diseño. El personaje no puede moverse ni mirar en diagonal");
       }   

       /*
       difference = difference.normalized;
       if (difference.x == 0 || difference.y == 0)
       {
           _animator.MoveX = difference.x;
           _animator.MoveY = difference.y;
       }
       else
       {
           Debug.LogError("Error de diseño. El personaje no puede moverse ni mirar en diagonal");
       }  */
   }
   
   

   /// <summary>
   /// Lleva a cabo acciones sobre un personaje que se vayan a ejecutar en el bucle Update
   /// </summary>
   public void HandleUpdate()
   {
       //Sincroniza el estado de movimiento del personaje con su animator igualando las booleanas
       _animator.IsMoving = isMoving;
   }


   /// <summary>
   /// Comprueba si en la ruta que debe se debe recorrer hasta llegar al punto de destino hay algún
   /// obstáculo con el que se vaya a colisionar
   /// </summary>
   /// <param name="target">El punto de destino</param>
   /// <returns>True si la ruta está libre</returns>
   private bool IsPathAvailable(Vector3 target)
   {
       //Calcula el trayecto a recorrer (destino - posición actual)
       Vector3 path = target - transform.position;
       //Se normaliza, para obtener la dirección de movimiento
       Vector3 direction = path.normalized;
       //Utiliza el motor de físicas 2D para "dibujar" una caja de un pequeño tamaño (0.3x0.3) que parte de la posición
       //actual a la que se suma una unidad (para que la caja no salga de centro y colisione con el propio carácter),
       //con un ángulo de 0 grados (es decir, sin rotar) en la dirección calculada al normalizar,
       //y de una dimensión dada por el trayecto a recorrer (a la que se le resta 1 unidad para
       //compensar la unidad que se ha sumado a la posición actual del character al principio)
       //Y se comprueba si la caja "colisionará" con algún objeto que esté en una capa de objetos físicos o interactivos
       if (Physics2D.BoxCast(transform.position + direction, new Vector2(0.3f, 0.3f), 0f,
               direction, path.magnitude - 1, GameLayers.SharedInstance.CollisionLayers) == true)
       {
           return false;//El camino o ruta no está libre
       }

       return true; //El camino o ruta está libre
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
