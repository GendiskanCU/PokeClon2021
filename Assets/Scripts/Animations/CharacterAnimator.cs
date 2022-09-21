using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LAS ANIMACIONES DE UN PERSONAJE NO CONTROLABLE POR EL PLAYER

public class CharacterAnimator : MonoBehaviour
{
    private float moveX, moveY;//Valores del movimiento en los dos ejes
    public float MoveX
    {
        get => moveX;
        set => moveX = value;
    }
    public float MoveY
    {
        get => moveY;
        set => moveY = value;
    }
    
    private bool isMoving;//Para indicar si el personaje está en movimiento
    public bool IsMoving
    {
        get => isMoving;
        set => isMoving = value;
    }

    //Animators personalizados para cada estado del personaje
    private CustomAnimator walkDownAnim, walkUpAnim, walkLeftAnim, walkRightAnim;

    [SerializeField] [Tooltip("Lista de sprites para la animación correspondiente del personaje")]
    private List<Sprite> walkDownSprites, walkUpSprites, walkLeftSprites, walkRightSprites;

    private SpriteRenderer spriteRend;//Componente que contiene el sprite a mostrar el cada momento

    private CustomAnimator currentAnimator;//Para controlar cuál es el animator activo en cada momento

    //Para controlar si el personaje se estaba moviendo en el anterior frame y compararlo con el frame actual
    private bool wasPreviouslyMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        //Crea los animators personalizados
        walkDownAnim = new CustomAnimator(spriteRend, walkDownSprites);
        walkUpAnim = new CustomAnimator(spriteRend, walkUpSprites);
        walkLeftAnim = new CustomAnimator(spriteRend, walkLeftSprites);
        walkRightAnim = new CustomAnimator(spriteRend, walkRightSprites);

        //Primer animator que se pone en marcha
        currentAnimator = walkDownAnim;
    }

    // Update is called once per frame
    void Update()
    {
        var previousAnimator = currentAnimator;
        //Cambia el animator activo en función del valor del movimiento actual del personaje
        if (moveX == 1)
        {
            currentAnimator = walkRightAnim;
        }
        else if (moveX == -1)
        {
            currentAnimator = walkLeftAnim;
        }
        else if (moveY == 1)
        {
            currentAnimator = walkUpAnim;
        }
        else if (moveY == -1)
        {
            currentAnimator = walkDownAnim;
        }

        if (previousAnimator != currentAnimator || isMoving != wasPreviouslyMoving)
        {
            //Si ha cambiado la animación o el estado de estar parado/moviéndose, habrá que reinicializar la nueva anim.
            currentAnimator.Start();
        }
        
        //Reproduce la animación correspondiente solo en caso de que el personaje se esté moviendo
        if (isMoving)
        {
            currentAnimator.HandleUpdate();
        }
        else//Si está parado (estado Idle) siempre se mostrará el primer sprite del último animator activo
        {
            spriteRend.sprite = currentAnimator.AnimFrames[0];
        }

        //De cara a arrancar correctamente la próxima la animación
        wasPreviouslyMoving = isMoving;
    }
}
