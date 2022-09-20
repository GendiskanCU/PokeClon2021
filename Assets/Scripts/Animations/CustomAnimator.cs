using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GESTIONA LOS SPRITES QUE SE MOSTRARÁN EN CADA NPC CONFORME SE VAYA MOVIENDO O ESTÉ PARADO,
//CONSTITUYENDO ASÍ UNA ESPECIE DE ANIMATOR PERSONALIZADO DESDE CÓDIGO

public class CustomAnimator
{
    private SpriteRenderer renderer;//El componente Sprite Renderer

    private List<Sprite> animFrames;//Los frames que conformarán la animación
    public List<Sprite> AnimFrames => animFrames;

    private float frameRate; //Velocidad de la animación (tiempo entre cada frame de la animación)

    private int currentFrame;//Para conocer el frame actual de la animación

    private float timer;//Para controlar cuánto tiempo ha transcurrido en el actual frame
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rendererParam"></param>
    /// <param name="animFramesParam"></param>
    /// <param name="frameRateParam"></param>
    /// <param name="???"></param>
    public CustomAnimator(SpriteRenderer rendererParam, List<Sprite> animFramesParam,
        float frameRateParam = 0.25f)
    {
        renderer = rendererParam;
        animFrames = animFramesParam;
        frameRate = frameRateParam;
    }


    
    /// <summary>
    /// Inicializa el sistema de animaciones del animator personalizado
    /// </summary>
    public void Start()
    {
        currentFrame = 0;
        timer = 0f;

        renderer.sprite = animFrames[currentFrame];
    }

    /// <summary>
    /// Reproduce la animación
    /// </summary>
    public void HandleUpdate()
    {
        timer += Time.deltaTime;//Incrementa el contador de paso de tiempo
        if (timer > frameRate)//Cuando se deba pasar al siguiente frame
        {
            currentFrame = (currentFrame + 1) % animFrames.Count;//Aumenta el contador, siempre dentro de la lista
            renderer.sprite = animFrames[currentFrame];//Muestra el sprite del frame actual
            
            //Reinicia el contador, pero para lograr mayor precisión, especialmente en equipos muy rápidos, en vez
            //de ponerlo en cero le descontaremos la duración total que debería tener el frame actual
            timer -= frameRate;
        }
    }
}
