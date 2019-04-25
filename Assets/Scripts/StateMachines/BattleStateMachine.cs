using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BattleStateMachine : MonoBehaviour
{
    public enum PerformAction
    {
        Wait,
        TakeAction,
        PerformAction,
        CheckAlive,
        HeroesTurn,
        EnemiesTurn,
        Win,
        Lose
    }

    public PerformAction battleState;

    public List<HandleTurn> performList = new List<HandleTurn>();

    public List<GameObject> enemiesInBattle = new List<GameObject>();
    public List<GameObject> heroesInBattle = new List<GameObject>();
    public List<GameObject> heroesToManage = new List<GameObject>();

    public enum HeroGui
    {
        Activate,
        SelectAction,
        SelectItem,
        SelectTarget,
        Done
    }

    public GameObject heroName;
    private Text _heroNameText;

    public HeroGui heroInput;
    private HandleTurn _heroChoice;

    public GameObject turnActionPanel;
    private Text _turnActionText;

    public GameObject actionPanel;
    private Transform _actionSpacer;

    public GameObject attackPanel;
    private Transform _attackSpacer;

    public GameObject enemySelectPanel;
    private Transform _enemiesSpacer;

    public GameObject actionButtonPrefab;
    public GameObject attackButtonPrefab;
    public GameObject targetButtonPrefab;

    public AudioSource globalAudio;
    
    private readonly List<GameObject> _enemyButtons = new List<GameObject>();
    private readonly List<GameObject> _attackButtons = new List<GameObject>();

    // Use this for initialization
    void Start()
    {
        battleState = PerformAction.Wait;
        heroesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Hero"));
        enemiesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));

        _heroNameText = heroName.transform.Find("Text").GetComponent<Text>();

        heroInput = HeroGui.Activate;

        turnActionPanel.SetActive(false);
        _turnActionText = turnActionPanel.transform.Find("TurnText").GetComponent<Text>();
        actionPanel.SetActive(false);
        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(false);

        _enemiesSpacer = enemySelectPanel.transform;
        _actionSpacer = actionPanel.transform;
        _attackSpacer = attackPanel.transform;

        CreateActionButtons();
    }

    // Update is called once per frame
    void Update()
    {
        switch (battleState)
        {
            case PerformAction.Wait:
                turnActionPanel.SetActive(false);
                ClearAttackPanel();
                battleState = PerformAction.HeroesTurn;
                break;
            case PerformAction.HeroesTurn:
                if (performList.Count == heroesInBattle.Count)
                {
                    heroName.SetActive(false);
                    battleState = PerformAction.EnemiesTurn;
                }
                break;
            case PerformAction.EnemiesTurn:
                if (performList.Count == heroesInBattle.Count + enemiesInBattle.Count)
                {
                    battleState = PerformAction.TakeAction;
                }
                break;
            case PerformAction.TakeAction:
                HandleTurn performance = performList[0];
                GameObject performer = performance.AttackerGO;

                string turnText = "";

                if (performList[0].Type == "Enemy")
                {
                    EnemyStateMachine esm = performer.GetComponent<EnemyStateMachine>();
                    // If the target is still in the battle, the enemy attacks it
                    if (heroesInBattle.Count(hero => hero.gameObject == performance.AttackerTarget) > 0)
                    {
                        esm.heroTarget = performance.AttackerTarget;
                        esm.currentState = EnemyStateMachine.TurnState.Action;
                    }
                    // If the target is not in the battle anymore but there are still heroes, takes one randomly
                    else if (heroesInBattle.Count > 0)
                    {
                        esm.heroTarget = heroesInBattle[Random.Range(0, heroesInBattle.Count)];
                        esm.currentState = EnemyStateMachine.TurnState.Action;
                    }
                    else
                    {
                        break;
                    }
                    turnText = String.Format("{0} attaque {1} avec {2} !", performance.Attacker,
                        performance.AttackerTarget.GetComponent<HeroStateMachine>().hero.Name,
                        performance.Attack.attackName);
                }
                else if (performList[0].Type == "Hero")
                {
                    HeroStateMachine hsm = performer.GetComponent<HeroStateMachine>();
                    hsm.enemyTarget = performList[0].AttackerTarget;
                    hsm.currentState = HeroStateMachine.TurnState.Action;
                    turnText = String.Format("{0} attaque {1} avec {2} !", performance.Attacker,
                        performance.AttackerTarget.GetComponent<EnemyStateMachine>().enemy.Name,
                        performance.Attack.attackName);
                }
                if (turnText != "")
                {
                    _turnActionText.text = turnText;
                    turnActionPanel.SetActive(true);
                }

                battleState = PerformAction.PerformAction;
                break;
            case PerformAction.PerformAction:
                break;
            case PerformAction.CheckAlive:
                if (heroesInBattle.Count < 1)
                    battleState = PerformAction.Lose;
                else if (enemiesInBattle.Count < 1)
                    battleState = PerformAction.Win;
                else
                {
                    ClearAttackPanel();
                    battleState = performList.Count <= 0 ? PerformAction.Wait : PerformAction.TakeAction;
                }
                break;
            case PerformAction.Win:
                _turnActionText.text = "C'est gagné, belle victoire !";
                turnActionPanel.SetActive(true);
                heroesInBattle.ForEach(
                    hero => { hero.GetComponent<HeroStateMachine>().currentState = HeroStateMachine.TurnState.Waiting; });
                break;
            case PerformAction.Lose:
                _turnActionText.text = "C'est perdu, dommage.";
                turnActionPanel.SetActive(true);
                break;
        }

        switch (heroInput)
        {
            case HeroGui.Activate:
                if (heroesToManage.Count > 0)
                {
                    GameObject hero = heroesToManage[0];
                    hero.transform.Find("Selector").gameObject.SetActive(true);
                    _heroChoice = new HandleTurn();

                    actionPanel.SetActive(true);

                    heroName.SetActive(true);
                    turnActionPanel.SetActive(true);
                    _heroNameText.text = _turnActionText.text = hero.GetComponent<HeroStateMachine>().hero.Name;

                    heroInput = HeroGui.SelectAction;
                }
                break;
            case HeroGui.SelectAction:
                break;
            case HeroGui.SelectItem:
                break;
            case HeroGui.SelectTarget:
                break;
            case HeroGui.Done:
                HeroInputDone();
                break;
        }
    }

    public void CollectActions(HandleTurn input)
    {
        performList.Add(input);
    }

    private void CreateActionButtons()
    {
        GameObject attackButton = Instantiate(actionButtonPrefab, _actionSpacer, false);
        attackButton.transform.Find("Text").GetComponent<Text>().text = "Attaques";
        attackButton.GetComponent<Button>().onClick.AddListener(AttackInput);
//        attackButton.GetComponent<VRTargetItem>().m_completionEvent.AddListener(AttackInput);

        GameObject itemsButton = Instantiate(actionButtonPrefab, _actionSpacer, false);
        itemsButton.transform.Find("Text").GetComponent<Text>().text = "Objets";
//        itemsButton.GetComponent<Button>().onClick.AddListener(AttackInput);

        GameObject pkmnButton = Instantiate(actionButtonPrefab, _actionSpacer, false);
        pkmnButton.transform.Find("Text").GetComponent<Text>().text = "Pokémons";
//        pkmnButton.GetComponent<Button>().onClick.AddListener(AttackInput);
    }

    //////////////////////////////////////////
    // Attack input management             //
    ////////////////////////////////////////
    private void AttackInput()
    {
        CreateAttackButtons();
        actionPanel.SetActive(false);
        attackPanel.SetActive(true);
        heroInput = HeroGui.SelectItem;
    }

    private void CreateAttackButtons()
    {
        if (_attackButtons.Count > 0)
        {
            _attackButtons.ForEach(Destroy);
            _attackButtons.Clear();
        }
        heroesToManage[0].GetComponent<HeroStateMachine>().hero.Attacks.ForEach(attack =>
        {
            GameObject newButton = Instantiate(attackButtonPrefab, _attackSpacer, false);
            newButton.transform.Find("Text").GetComponent<Text>().text = attack.attackName;

            AttackButton atckBtn = newButton.GetComponent<AttackButton>();
            atckBtn.Attack = attack;

            _attackButtons.Add(newButton);
        });
    }


    //////////////////////////////////////////
    // Target input management             //
    ////////////////////////////////////////
    public void SelectTargetInput(BaseAttack attack)
    {
        _heroChoice.Attacker = heroesToManage[0].name;
        _heroChoice.AttackerGO = heroesToManage[0];
        _heroChoice.Type = "Hero";
        _heroChoice.Attack = attack;
        CreateEnemyButtons();
        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(true);
        heroInput = HeroGui.SelectTarget;
    }

    private void CreateEnemyButtons()
    {
        if (_enemyButtons.Count > 0)
        {
            _enemyButtons.ForEach(Destroy);
            _enemyButtons.Clear();
        }
        enemiesInBattle.Where(enemy => enemy.GetComponent<EnemyStateMachine>().alive).ToList().ForEach(enemy =>
        {
            GameObject newButton = Instantiate(targetButtonPrefab, _enemiesSpacer, false);

            EnemyStateMachine currentEnemy = enemy.GetComponent<EnemyStateMachine>();
            newButton.transform.Find("Text").GetComponent<Text>().text = currentEnemy.enemy.Name;

            EnemySelectButton enemyButton = newButton.GetComponent<EnemySelectButton>();
            enemyButton.EnemyGameObject = enemy;

            _enemyButtons.Add(newButton);
        });
    }

    public void EnemySelectInput(GameObject chosenEnemy)
    {
        _heroChoice.Attacker = heroesToManage[0].GetComponent<HeroStateMachine>().hero.Name;
        _heroChoice.AttackerTarget = chosenEnemy;
        heroInput = HeroGui.Done;
    }

    private void HeroInputDone()
    {
        performList.Add(_heroChoice);
        enemySelectPanel.SetActive(false);
        heroesToManage[0].transform.Find("Selector").gameObject.SetActive(false);
        heroesToManage.RemoveAt(0);
        heroInput = HeroGui.Activate;
    }

    private void ClearAttackPanel()
    {
        actionPanel.SetActive(false);
        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(false);
    }

    public void VoiceInput(HandleTurn turn)
    {
        performList.Add(turn);
        actionPanel.SetActive(false);
        heroesToManage[0].transform.Find("Selector").gameObject.SetActive(false);
        heroesToManage.Remove(turn.AttackerGO);
        heroInput = HeroGui.Activate;
    }
}