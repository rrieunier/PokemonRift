using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MyExtensions
{
    public static bool HeroExists(this IEnumerable<GameObject> heroes, string heroName, out GameObject recognizedHero,
        ref BaseHero hsm)
    {
        recognizedHero = heroes.FirstOrDefault(
            hero => hero.GetComponent<HeroStateMachine>().hero.Name.ToLower().Contains(heroName.ToLower()));
        Debug.Log(string.Format("{0}   {1}", heroName, heroes.toString("heroes")));
        if (recognizedHero)
        {
            hsm = recognizedHero.GetComponent<HeroStateMachine>().hero;
        }
        return recognizedHero != null;
    }

    private static string toString(this IEnumerable<GameObject> heroes, string type)
    {
        switch (type)
        {
            case "heroes":
                return heroes.Aggregate("", (acc, hero) => acc + hero.GetComponent<HeroStateMachine>().hero.Name);
            case "enemies":
                return heroes.Aggregate("", (acc, hero) => acc + hero.GetComponent<EnemyStateMachine>().enemy.Name);
            default:
                return "undefined";
        }
    }

    public static bool EnemyExists(this IEnumerable<GameObject> enemies, string enemyName,
        out GameObject recognizedEnemy, ref BaseEnemy esm)
    {
        recognizedEnemy = enemies.FirstOrDefault(
            enemy => enemy.GetComponent<EnemyStateMachine>().enemy.Name.ToLower().Contains(enemyName.ToLower()));
        Debug.Log(string.Format("{0}   {1}", enemyName, enemies.toString("enemies")));
        if (recognizedEnemy)
        {
            esm = recognizedEnemy.GetComponent<EnemyStateMachine>().enemy;
        }
        return recognizedEnemy != null;
    }

    public static bool AttackExists(this IEnumerable<BaseAttack> attacks, string attackName,
        out BaseAttack recognizedAttack)
    {
        recognizedAttack = attacks.FirstOrDefault(attack => attack.attackName.ToLower().Contains(attackName.ToLower()));
        return recognizedAttack != null;
    }
}