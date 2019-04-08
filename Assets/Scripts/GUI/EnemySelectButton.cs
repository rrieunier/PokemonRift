using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySelectButton : MonoBehaviour
{
    public GameObject EnemyGameObject;
    private GameObject selector;

    private void Start()
    {
        selector = EnemyGameObject.transform.Find("Selector").gameObject;
    }

    public void SelectEnemy()
    {
        GameObject.Find("BattleManager").GetComponent<BattleStateMachine>().EnemySelectInput(EnemyGameObject);
        selector.SetActive(false);
    }

    private void ToggleSelector()
    {
        selector.SetActive(!selector.activeSelf);
    }
}