using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA EL PANEL DE UI DE SELECCIÓN DE MOVIMIENTOS APRENDIDOS POR UN POKEMON

public class LearnedMovesSelectionUI : MonoBehaviour
{
  [SerializeField] [Tooltip("Cuadro de texto con el nombre del movimiento")]
  private List<Text> movementText;

  [SerializeField] [Tooltip("Color para resaltar el movimiento seleccionado")]
  private Color selectedColor = Color.red;

  //Para guardar el movimiento seleccionado en el panel
  private int currentSelectedMovement = 0;

  /// <summary>
  /// Rellena la UI de selección de movimientos, en la que el player escogerá el que quiere olvidar
  /// </summary>
  /// <param name="pokemonMoves">Los movimientos anteriormente aprendidos por el pokemon</param>
  /// <param name="newMove">El nuevo movimiento que puede aprender el pokemon</param>
  public void SetMovements(List<MoveBase> pokemonMoves, MoveBase newMove)
  {
    currentSelectedMovement = 0;
    
    int i;
    for (i = 0; i < pokemonMoves.Count; i++)
    {
      movementText[i].text = pokemonMoves[i].AttackName;
    }

    movementText[i].text = newMove.AttackName;
  }


  /// <summary>
  /// Implementa la lógica de elección del movimiento a olvidar en el panel de la UI
  /// </summary>
  /// <param name="onSelected">Evento de Unity para devolver la acción indicando el elemento seleccionado </param>
  public void HandleForgetMoveSelection(Action<int> onSelected)
  {
    //Captura el movimiento en vertical, cuando lo haya
    if (Input.GetAxisRaw("Vertical") != 0)
    {
      int direction = Mathf.FloorToInt(Input.GetAxisRaw("Vertical"));
      //Actualiza el movimiento seleccionado
      currentSelectedMovement -= direction;
      //Lo mantiene dentro de los límites de la lista de movimientos
      currentSelectedMovement = Mathf.Clamp(currentSelectedMovement, 0,
        PokemonBase.NUMBER_OF_LEARNABLE_MOVES);
      //Resalta el movimiento seleccionado
      UpdateColorForgetMoveSelection();
      //Invoca el evento devolviendo un -1 para indicar simplemente que el jugador ha pulsado una tecla
      //de forma que en el battlemanager pueda resetearse el timer entre pulsaciones
      onSelected?.Invoke(-1);
    }
    
    //A pulsar el botón de Submit
    if (Input.GetAxisRaw("Submit") != 0)
    {
      //Invoca el evento de Unity devolviendo el movimiento seleccionado.
      //La interrogación se pone para indicar que solo actúa si en algún sitio se está esperando la acción
      onSelected?.Invoke(currentSelectedMovement);
    }
  }

  /// <summary>
  /// Resalta el movimiento seleccionado con otro color y el resto del color por defecto
  /// </summary>
  private void UpdateColorForgetMoveSelection()
  {
    for (int i = 0; i <= PokemonBase.NUMBER_OF_LEARNABLE_MOVES; i++)
    {
      movementText[i].color = (i == currentSelectedMovement) ? selectedColor : Color.black;
    }
  }
}
