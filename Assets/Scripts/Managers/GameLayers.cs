using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SINGLETON PARA GESTIONAR LAS DIVERSAS CAPAS QUE DEBEN SER TENIDAS EN CUENTA EN OTRAS CLASES

public class GameLayers : MonoBehaviour
{
    [SerializeField] [Tooltip("Capa/s a la/s que está/n asignado/s los Tilemap de objetos sólidos con colisión")]
    private LayerMask solidObjectsLayer;
    public LayerMask SolidObjectsLayer => solidObjectsLayer;

    [SerializeField] [Tooltip("Capa/s a la/s que está/n asignado/s los Tilemap de zonas de aparición de pokemon")]
    private LayerMask pokemonLayer;
    public LayerMask PokemonLayer => pokemonLayer;

    [SerializeField] [Tooltip("Capa/s en la/s que está/n los objetos con los que puede interactura el player")]
    private LayerMask interactableLayer;
    public LayerMask InteractableLayer => interactableLayer;

    [SerializeField] [Tooltip("Capa a la que pertenece el propio Player")]
    private LayerMask playerLayer;
    public LayerMask PlayerLayer => playerLayer;

    //Singleton
    public static GameLayers SharedInstance;

    private void Awake()
    {
        if (SharedInstance == null)
        {
            SharedInstance = this;
        }
    }


    /// <summary>
    /// Devuelve las capas que son de tipo colisión, las que el Player o un NPC no podrá atravesar cuando se mueve
    /// </summary>
    public LayerMask CollisionLayers => SolidObjectsLayer | InteractableLayer | PlayerLayer;
}
