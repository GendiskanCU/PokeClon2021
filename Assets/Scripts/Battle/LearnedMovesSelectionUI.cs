using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GESTIONA EL PANEL DE UI DE SELECCIÓN DE MOVIMIENTOS APRENDIDOS POR UN POKEMON

public class LearnedMovesSelectionUI : MonoBehaviour
{
  [SerializeField] [Tooltip("Cuadro de texto con el nombre del movimiento")]
  private List<Text> movementText;

  /// <summary>
  /// Rellena la UI de selección de movimientos, en la que el player escogerá el que quiere olvidar
  /// </summary>
  /// <param name="pokemonMoves">Los movimientos anteriormente aprendidos por el pokemon</param>
  /// <param name="newMove">El nuevo movimiento que puede aprender el pokemon</param>
  public void SetMovements(List<MoveBase> pokemonMoves, MoveBase newMove)
  {
    int i;
    for (i = 0; i < pokemonMoves.Count; i++)
    {
      movementText[i].text = pokemonMoves[i].AttackName;
    }

    movementText[i].text = newMove.AttackName;
  }
}
