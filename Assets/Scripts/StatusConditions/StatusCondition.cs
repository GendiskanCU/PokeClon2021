using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DEFINE LOS DIVERSOS ESTADOS ALTERADOS QUE PUEDEN AFECTAR A UN POKEMON DURANTE LA BATALLA


public class StatusCondition
{
    //Nombre del estado alterado
    public string Name { get; set; }
    
    //Descripción del estado alterado
    public string Description { get; set; }
    
    //Mensaje que se mostrará en la UI si el estado alterado se ha activado
    public string StartMessage { get; set; }
    
    //Acción (delegado o evento) que será activado al final de cada turno, de forma que sea entonces cuando se apliquen
    //de forma efectiva el efecto del estado alterado que sufre el pokemon que ha sido atacado
    public Action<Pokemon> OnFinishTurn { get; set; }
}
