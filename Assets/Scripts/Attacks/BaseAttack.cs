using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BaseAttack : MonoBehaviour
{
    public string attackName;
    public string attackDescription;
    public float attackDamage;
    public float attackCost;

    public BaseAttack(string name, float damage, float cost, string description = "")
    {
        attackName = name;
        attackDamage = damage;
        attackCost = cost;
        attackDescription = description;
    }
}