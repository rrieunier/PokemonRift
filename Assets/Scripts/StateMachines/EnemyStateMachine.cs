using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EnemyStateMachine : MonoBehaviour
{
    private BattleStateMachine _bsm;

    public BaseEnemy enemy;

    public enum TurnState
    {
        Processing,
        ChooseAction,
        Waiting,
        Action,
        Dead
    }

    public TurnState currentState;

    private float _currentCooldown;
    private const float MaximumCooldown = 2f;

    public GameObject selector;
    public Slider healthBar;
    private Image _healthBarColor;

    public GameObject heroTarget;
    private bool _actionStarted;
    private Vector3 _startPosition;
    private const float AnimationSpeed = 10f;

    public bool alive = true;

    // Use this for initialization
    void Start()
    {
        currentState = TurnState.Processing;
        _bsm = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        _startPosition = transform.position;
        selector.SetActive(false);
        healthBar.maxValue = enemy.BaseHp;
        healthBar.value = enemy.BaseHp;
        _healthBarColor = healthBar.GetComponentsInChildren<Image>()[1];
        
        enemy.CurHp = enemy.BaseHp;
        enemy.CurAtk = enemy.BaseAtk;
        enemy.CurDef = enemy.BaseDef;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case TurnState.Processing:
                UpgradeProgressBar();
                break;
            case TurnState.ChooseAction:
                if (_bsm.battleState == BattleStateMachine.PerformAction.EnemiesTurn)
                {
                    ChooseAction();
                    currentState = TurnState.Waiting;
                }
                break;
            case TurnState.Waiting:
                break;
            case TurnState.Action:
                StartCoroutine(TimeForAction());
                break;
            case TurnState.Dead:
                if (alive)
                {
                    alive = false;
                    gameObject.tag = "DeadEnemy";
                    _bsm.enemiesInBattle.Remove(gameObject);
                    HandleTurn enemyTurn = _bsm.performList.FirstOrDefault(turn => turn.AttackerGO == gameObject);
                    _bsm.performList.Remove(enemyTurn);

                    if (_bsm.enemiesInBattle.Count > 0)
                        _bsm.performList.Where(turn => turn.AttackerTarget == gameObject).ToList()
                            .ForEach(turn =>
                                turn.AttackerTarget = _bsm.enemiesInBattle[Random.Range(0, _bsm.enemiesInBattle.Count)]
                            );

                    transform.Find("pokemon").transform.Rotate(0, 0, -90);

                    _bsm.battleState = BattleStateMachine.PerformAction.CheckAlive;
                }
                break;
        }
    }

    void UpgradeProgressBar()
    {
        _currentCooldown += Time.deltaTime;

        if (_currentCooldown >= MaximumCooldown)
        {
            currentState = TurnState.ChooseAction;
        }
    }

    void ChooseAction()
    {
        if (_bsm.heroesInBattle.Count > 0)
        {
            HandleTurn attack = new HandleTurn
            {
                Attacker = enemy.Name,
                Type = "Enemy",
                AttackerGO = gameObject,
                AttackerTarget = _bsm.heroesInBattle[Random.Range(0, _bsm.heroesInBattle.Count)],
                Attack = enemy.Attacks[Random.Range(0, enemy.Attacks.Count)]
            };

            _bsm.CollectActions(attack);
        }
    }

    private IEnumerator TimeForAction()
    {
        if (_actionStarted)
        {
            yield break;
        }

        _actionStarted = true;

        var position = heroTarget.transform.position;
        Vector3 heroPosition = new Vector3(position.x, position.y, position.z + 1.5f);
        while (MoveTowardsEnemy(heroPosition))
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        DoDamage();

        while (MoveTowardsStart())
        {
            yield return null;
        }

        _bsm.performList.RemoveAt(0);
        _bsm.battleState = BattleStateMachine.PerformAction.CheckAlive;

        _actionStarted = false;

        _currentCooldown = 0f;

        currentState = TurnState.Processing;
    }

    private bool MoveTowardsEnemy(Vector3 target)
    {
        return target !=
               (transform.position = Vector3.MoveTowards(transform.position, target, AnimationSpeed * Time.deltaTime));
    }

    private bool MoveTowardsStart()
    {
        return _startPosition !=
               (transform.position =
                   Vector3.MoveTowards(transform.position, _startPosition, AnimationSpeed * Time.deltaTime));
    }

    public void TakeDamage(float damage)
    {
        enemy.CurHp -= damage - enemy.CurDef;
        if (enemy.CurHp <= 0)
        {
            enemy.CurHp = 0;
            currentState = TurnState.Dead;
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBar.value = enemy.CurHp;
        if (enemy.CurHp >= enemy.BaseHp / 2)
            _healthBarColor.color = Color.green;
        else if (enemy.CurHp >= enemy.BaseHp / 5)
            _healthBarColor.color = Color.yellow;
        else
            _healthBarColor.color = Color.red;
    }

    private void DoDamage()
    {
        float damage = enemy.CurAtk + _bsm.performList[0].Attack.attackDamage;
        heroTarget.GetComponent<HeroStateMachine>().TakeDamage(damage);
    }
}