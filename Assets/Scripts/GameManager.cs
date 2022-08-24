using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Estados posibles del juego
public enum GameState
{
    TRAVEL, //El player está moviéndose por el mundo
    BATTLE //En batalla
}


public class GameManager : MonoBehaviour
{
    [SerializeField] [Tooltip("Controlador del player")]
    private PlayerController playerController;

    [SerializeField] [Tooltip("Controlador de las batallas")]
    private BattleManager battleManager;

    private GameState _gameState;

    private void Awake()
    {
        _gameState = GameState.TRAVEL;
    }

    private void Update()
    {
        if (_gameState == GameState.TRAVEL)
        {
            playerController.HandleUpdate();
        }
        else if (_gameState == GameState.BATTLE)
        {
            battleManager.HandleUpdate();
        }
    }
}
