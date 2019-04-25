using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HandleTurn
{
    public string Attacker;

    /// <summary>
    /// 
    /// </summary>
    public string Type;

    public GameObject AttackerGO;
    public GameObject AttackerTarget;

    public BaseAttack Attack;

    public bool IsConsistent()
    {
        return Attacker != "" && Type != "" && AttackerGO && AttackerTarget && Attack;
    }

    public override string ToString()
    {
        string attackerGo = "undefined", targetGo = "undefined", attackName = "undefined";

        if (AttackerGO) attackerGo = AttackerGO.name;
        if (AttackerTarget) targetGo = AttackerTarget.name;
        if (Attack) attackName = Attack.attackName;

        return string.Format("Attacker GO : {0} name : {1}   Target : {2}   Attack : {3}", attackerGo, Attacker,
            targetGo, attackName);
    }
}