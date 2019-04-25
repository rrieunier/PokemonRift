using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HeroStateMachine : MonoBehaviour
{
    private BattleStateMachine _bsm;

    public BaseHero hero;

    public enum TurnState
    {
        Processing,
        AddToList,
        Waiting,
        Selecting,
        Action,
        TakeDamage,
        Dead
    }

    public TurnState currentState;

    public GameObject selector;

    public GameObject heroPanel;
    public Transform heroSpacer;
    private Slider _progressBar;
    private HeroPanelStats _stats;

    public GameObject enemyTarget;

    private float _currentCooldown;
    private const float MaximumCooldown = 2f;

    private bool _actionStarted;

    private Vector3 _startPosition;

    private const float AnimationSpeed = 10f;

    private bool _alive = true;

    // Use this for initialization
    private void Start()
    {
        hero.curHP = hero.baseHP;
        hero.curAtk = hero.baseAtk;
        hero.curDef = hero.baseDef;

        _currentCooldown = Random.Range(0, 2.5f);
        currentState = TurnState.Processing;
        _bsm = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        selector.SetActive(false);
        _startPosition = transform.position;

        CreateHeroPanel();
    }

    // Update is called once per frame
    private void Update()
    {
        switch (currentState)
        {
            case TurnState.Processing:
//                UpgradeProgressBar();
                currentState = hero.curHP > 0f ? TurnState.AddToList : TurnState.Dead;
                break;
            case TurnState.AddToList:
                if (_bsm.battleState == BattleStateMachine.PerformAction.HeroesTurn)
                {
                    _bsm.heroesToManage.Add(gameObject);
                    currentState = TurnState.Waiting;
                }
                break;
            case TurnState.Waiting:
                break;
            case TurnState.Selecting:
                break;
            case TurnState.Action:
                StartCoroutine(TimeForAction());
                break;
            case TurnState.TakeDamage:
//                UpgradeProgressBar();
                currentState = hero.curHP > 0f ? TurnState.AddToList : TurnState.Dead;
                break;
            case TurnState.Dead:
                if (_alive)
                {
                    _alive = false;
                    gameObject.tag = "DeadHero";
                    _bsm.heroesInBattle.Remove(gameObject);
                    _bsm.heroesToManage.Remove(gameObject);
                    selector.SetActive(false);
                    _bsm.actionPanel.SetActive(false);
                    HandleTurn heroTurn = _bsm.performList.FirstOrDefault(turn => turn.AttackerGO == gameObject);
                    _bsm.performList.Remove(heroTurn);

                    if (_bsm.heroesInBattle.Count > 0)
                        _bsm.performList.Where(turn => turn.AttackerTarget == gameObject).ToList().ForEach(turn =>
                            turn.AttackerTarget = _bsm.heroesInBattle[Random.Range(0, _bsm.heroesInBattle.Count)]
                            );

//                    gameObject.GetComponent<MeshRenderer>().material.color = new Color32(105, 105, 105, 255);
                    transform.Find("mentali").transform.Rotate(0, 0, -90);

                    _bsm.battleState = BattleStateMachine.PerformAction.CheckAlive;
                }
                break;
        }
    }

    private void UpgradeProgressBar()
    {
        _currentCooldown += Time.deltaTime;
        float calcCooldown = _currentCooldown / MaximumCooldown;

        float hp = hero.curHP / hero.baseHP;
        var localScale = _progressBar.transform.localScale;
        localScale = new Vector3(Mathf.Clamp(calcCooldown, 0, hp), localScale.y, localScale.z);
        _progressBar.transform.localScale = localScale;
        if (_currentCooldown >= MaximumCooldown)
        {
            currentState = hero.curHP > 0f ? TurnState.AddToList : TurnState.Dead;
            _currentCooldown = 0f;
        }
    }

    private void UpdateHeroPanel()
    {
        _stats.HP.text = String.Format("HP: {0}/{1}", hero.curHP, hero.baseHP);
        _progressBar.value = hero.curHP;
    }

    private IEnumerator TimeForAction()
    {
        if (_actionStarted)
        {
            yield break;
        }

        _actionStarted = true;

        var position = enemyTarget.transform.position;
        Vector3 enemyPosition = new Vector3(position.x, position.y, position.z - 1.5f);
        while (MoveTowardsEnemy(enemyPosition))
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
        currentState = TurnState.TakeDamage;
        hero.curHP -= damage - hero.curDef;
        if (hero.curHP < 0f)
        {
            hero.curHP = 0f;
        }
        UpdateHeroPanel();
    }

    private void DoDamage()
    {
        float damage = hero.curAtk + _bsm.performList[0].Attack.attackDamage;
        enemyTarget.GetComponent<EnemyStateMachine>().TakeDamage(damage);
    }

    private void CreateHeroPanel()
    {
        heroPanel = Instantiate(heroPanel, heroSpacer, false);
        _stats = heroPanel.GetComponent<HeroPanelStats>();
        _stats.Name.text = hero.theName;
        _stats.HP.text = string.Format("HP: {0}/{1}", hero.curHP, hero.baseHP);

        _progressBar = _stats.HealthBar;
        _progressBar.maxValue = hero.baseHP;
        _progressBar.value = hero.curHP;
    }
}