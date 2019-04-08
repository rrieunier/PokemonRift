using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HandleTurn
{
    public string Attacker;
    public string Type;
    public GameObject AttackerGO;
    public GameObject AttackerTarget;

    public BaseAttack Attack;
}