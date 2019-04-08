using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BaseEnemy : BaseClass
{
    public enum Type
    {
        GRASS,
        FIRE,
        WATER,
        ELECTRIC,
    }

    public enum Rarity
    {
        COMMON,
        UNCOMMON,
        RARE,
        LEGENDARY,
    }

    public Type enemyType;
    public Rarity enemyRarity;
}