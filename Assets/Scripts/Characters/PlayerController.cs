using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditorInternal;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

//[RequireComponent(typeof(Animator))]

public class PlayerController : MonoBehaviour
{
    //Para controlar que no se pueda hacer alguna acción hasta pasado un lapso aunque se mantenga pulsado
    private float timeSinceLastClick;
    [SerializeField][Tooltip("Tiempo para poder cambiar la elección en los paneles de acción, ataque, etc.")]
    private float timeBetweenClicks = 1.0f;

    //Evento de la clase Action de Unity para indicar que se ha encontrado un pokemon y ha de iniciarse la batalla
    public event Action OnPokemonEncountered;
    
    //Para guardar los valores de los ejes x/y antes de transmitirlos al player
    private Vector2 input;

    //Componente que controla el movimiento del personaje
    private Character _character;

    private void Awake()
    {
        _character = GetComponent<Character>();
    }

    
    /// <summary>
    /// Método que inicia el movimiento del player en el update, cuando sea invocado desde el GameManager,
    /// y también la interactuación con NPC u otros objetos
    /// </summary>
    public void HandleUpdate()
    {
        //Actualiza el contador entre clicks
        timeSinceLastClick += Time.deltaTime;
        
        
        //Movimiento del player
        MovePlayer();

        //Si se pulsa la tecla de acción, si es posible iniciará una interactuación
        if (Input.GetAxisRaw("Submit") != 0)
        {
            if (timeSinceLastClick >= timeBetweenClicks)//Controla que haya transcurrido el tiempo entre clicks
            {
                timeSinceLastClick = 0;//Reinicia el contador entre clicks
                Interact();
            }
        }
    }

    private void MovePlayer()
    {
        //El player no iniciará un nuevo movimiento hasta finalizar el actual
        
        if (!_character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");//Valor 0 ó 1 (GetAxisRaw)
            input.y = Input.GetAxisRaw("Vertical");//Valor 0 ó 1 (GetAxisRaw)
            
            //Solo habrá movimiento si alguno de los dos ejes no es cero
            if (input != Vector2.zero)
            {
                //Se inicia la corutina de movimiento del controlador de movimiento, pasándole el valor del input
                //y especificando que cuando se invoque el final de movimiento se chequee si ha dado con un pokemon
                StartCoroutine(_character.MoveToward(input, CheckForPokemon));
            }
        }
        
        //Se sincroniza el movimiento con la animación
        _character.HandleUpdate();
    }

    /// <summary>
    /// Comprueba si el player está lo suficientemente cerca de un objeto con el que puede interactuar y que además
    /// está "mirando" hacia el mismo. En caso afirmativo, inicia la interactuación
    /// </summary>
    private void Interact()
    {
        //Obtiene hacia dónde "mira" el player, aprovechando que el valor se guarda en los parámetros de su animación
        //var facingDirection = new Vector3(_animator.GetFloat("MoveX"), _animator.GetFloat("MoveY"), 0);
        //Con el animator personalizado:
        var facingDirection = new Vector3(_character.Animator.MoveX, _character.Animator.MoveY, 0);
        
        //Se calcula la posición contra la cual se quiere interactuar, sumando 1 unidad a la posición actual
        //en la dirección hacia la que se está mirando
        var interactPosition = transform.position + facingDirection;
        Debug.DrawLine(transform.position, interactPosition, Color.magenta, 1.0f);
        
        //Se comprueba si en un pequeño radio alrededor de la posición de posible interactuación hay algún
        //objeto de la capa de objetos con los que se puede interactuar
        var collider = Physics2D.OverlapCircle(interactPosition, 0.2f, 
            GameLayers.SharedInstance.InteractableLayer);
        if (collider != null)
        {
            //Se puede interactuar con el objeto. Se captura su componente Interactable, si la posee y se
            //ejecuta el método que lanza la interacción. Nota: el componente Interactable es una interface
            //implementada en el script que controla el objeto interactivo
            collider.GetComponent<Interactable>()?.Interact(transform.position);
        }
    }
    

    /// <summary>
    /// Comprueba si el player se encuentra en una zona pokemon y en caso afirmativo lanza la
    /// generación aleatoria de aparición de un pokemon y por tanto una batalla
    /// </summary>
    private void CheckForPokemon()
    {
        //Como el player no inicia la partida en una posición centrada como (0.5, 0.5) sino con un pequeño
        //desplazamiento en el eje y de 0.2,o sea comienza en (0.5, 0.7), hay que tenerlo en cuenta para el
        //cálculo del área de detección de una zona pokemon
        float offsetY = 0.2f;
        
        if (Physics2D.OverlapCircle(transform.position - new Vector3(0, offsetY, 0)
                , 0.2f, GameLayers.SharedInstance.PokemonLayer) != null)
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
