using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackButton : MonoBehaviour
{
    public BaseAttack Attack;

    public void CastAttack()
    {
        GameObject.Find("BattleManager").GetComponent<BattleStateMachine>().SelectTargetInput(Attack);
    }
}