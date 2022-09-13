using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DEFINE LOS DIVERSOS ESTADOS ALTERADOS O VOLÁTILES QUE PUEDEN AFECTAR A UN POKEMON DURANTE LA BATALLA


public class StatusCondition
{
    //Id del estado alterado
    public StatusConditionID Id { get; set; }
    //Nombre del estado alterado
    public string Name { get; set; }
    
    //Descripción del estado alterado
    public string Description { get; set; }
    
    //Mensaje que se mostrará en la UI si el estado alterado se ha activado
    public string StartMessage { get; set; }
    
    
    //Para estados alterados que tengan efecto al inicio del turno: congelado, dormido, etc.
    //Acción (delegado o evento) que será activado al inicio de cada turno, de forma que sea entonces cuando se aplique
    //de forma efectiva, si es el caso, el efecto del estado alterado que sufre el pokemon que ha sido atacado
    //Se define de tipo Func para permitir que tenga un valor de retorno, en este caso un booleano que indique si tras
    //el efecto alterado el pokemon afectado podrá o no atacar en el siguiente turno
    public Func<Pokemon, bool> OnStartTurn { get; set; }
    
    //Para estados alterados que tengan efecto al final del turno: quemado, envenenado, etc.
    //Acción (delegado o evento) que será activado al final de cada turno, de forma que sea entonces cuando se aplique
    //de forma efectiva, si es el caso, el efecto del estado alterado que sufre el pokemon que ha sido atacado
    //En este caso es de tipo Action ya que no necesitamos que se devuelva ningún valor
    public Action<Pokemon> OnFinishTurn { get; set; }
    
    
    //Para estados alterados que deban fijar algunos valores antes del turno, nada más que el estado sea aplicado.
    //Ej: el estado "dormido" dura un número determinado de turnos. Cuando este estado sea aplicado, se establecerá cuál
    //es el número de turnos que va a durar el efecto, de tal forma que después, al inicio de cada turno, descontemos
    //ese contador hasta que llegue a cero, momento en que el estado dormido dejará de estar activo 
    public Action<Pokemon> OnApplyStatusCondition { get; set; }
}
