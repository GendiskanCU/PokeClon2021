using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//MOVIMIENTOS (ATAQUES) INSTANCIABLES.
//SE CONTROLAN SUS ESTADÍSTICAS PARTIENDO DE LAS ESTADÍSTICAS BASE Y LA CANTIDAD DE PUNTOS DE PODER DE CADA MOMENTO(PP)
public class Move
{
    //Propiedades y estadísticas del movimiento base
    private MoveBase _base;
    public MoveBase Base
    {
        get => _base;
        set => _base = value;
    }


    //Puntos de poder que harán variar las estadísticas
    private int _pp;
    public int Pp
    {
        get => _pp;
        set => _pp = value;
    }
    
    //Constructor que recibe el movimiento base y los PP de partida del mismo
    public Move(MoveBase mBase)
    {
        _base = mBase;
        _pp = mBase.PP;
    }
}
