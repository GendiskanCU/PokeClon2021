using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GESTIONA LAS LÍNEAS DE DIÁLOGO QUE SE IRÁN MOSTRANDO EN LA UI CUADRO DE DIÁLOGO GENERAL AL INTERACTUAR CON NPC, ETC.
/// </summary>
[Serializable] public class Dialog
{
    [SerializeField] [Tooltip("Líneas que componen el diálogo")]
    private List<string> lines;
    public List<string> Lines => lines;
}
