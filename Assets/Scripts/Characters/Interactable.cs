using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DEFINE LOS COMPORTAMIENTOS GENERALES DE CUALQUIER OBJETO CON EL QUE SE PUEDA INTERACTUAR (NPC, ITEMS, ETC.)
//SERÁ UNA INTERFACE QUE LUEGO DEBERÁ SER IMPLEMENTADA EN CADA OBJETO INTERACTIVO CONCRETO

public interface Interactable
{
   /// <summary>
   /// Lanza la interacción con un objeto fuente, del que recibe su posición
   /// </summary>
   void Interact(Vector3 source);
}
