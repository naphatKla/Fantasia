using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    #region Declare Variable

    public enum EnemyState
    {
        Idle,
        FollowTarget,
        ReturnToSpawn,
        WaitToReturn
    }

    public enum PriorityTag
    {
        Player,
        NPC,
        Tower
    }

    [SerializeField] private float viewDistance;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private List<PriorityTag> priorityTags;
    [SerializeField] private float roamDuration;
    [SerializeField] private float roamCooldown;
    [SerializeField] private Vector2 roamArea;
    private NavMeshAgent _agent;
    private Rigidbody2D _rigidbody2D;
    private GameObject _target;
    private Vector2 _spawnPoint;
    private bool _isWait;
    private bool _isRoam;
    public EnemyState enemyActionState;

    #endregion

    #region Unity Method

    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _isWait = false;
        _spawnPoint = transform.position;
        enemyActionState = EnemyState.Idle;
    }

    void Update()
    {
        Collider2D[] targetInDistances = Physics2D.OverlapCircleAll(transform.position, viewDistance, playerLayerMask);

        if (targetInDistances.Length > 0)
        {
            // Detect targets only on the first priority tag found
            for (int i = 0; i < priorityTags.Count; i++)
            {
                foreach (Collider2D col in targetInDistances)
                {
                    if (!col.gameObject.CompareTag(priorityTags[i].ToString())) continue;

                    _target = col.gameObject;
                    i += priorityTags.Count; // break out the loop
                    break;
                }
            }

            SetEnemyState(EnemyState.FollowTarget);
        }
        else if (enemyActionState == EnemyState.FollowTarget)
        {
            SetEnemyState(EnemyState.WaitToReturn);
        }
        else if ((Vector2)transform.position == _spawnPoint)
        {
            SetEnemyState(EnemyState.Idle);
        }

        PlayAction(enemyActionState);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }

    private IEnumerator WaitToReturn(float time = 0)
    {
        _isWait = true;
        yield return new WaitForSeconds(time);
        _isWait = false;
        enemyActionState = EnemyState.ReturnToSpawn;
    }

    private IEnumerator RoamAround(float time)
    {
        float _timeCount = 0;
        Vector2 randomPos = new Vector2(_spawnPoint.x + Random.Range(roamArea.x, roamArea.y),
            _spawnPoint.y + Random.Range(roamArea.x, roamArea.y));
        _isRoam = true;

        yield return new WaitForSeconds(roamCooldown);

        while (_timeCount < time)
        {
            _isRoam = true;
            _timeCount += Time.deltaTime;
            _agent.SetDestination(randomPos);

            if (enemyActionState != EnemyState.Idle)
            {
                _isRoam = false;
                yield break;
            }

            yield return null;
        }

        _isRoam = false;
    }

    #endregion

    #region Method

    public void SetEnemyState(EnemyState state)
    {
        enemyActionState = state;
    }

    public void PlayAction(EnemyState enemyState)
    {
        switch (enemyState)
        {
            case EnemyState.Idle:
            {
                if (_isRoam) return;
                StartCoroutine(RoamAround(roamDuration));
                break;
            }
            case EnemyState.FollowTarget:
            {
                _agent.SetDestination(_target.transform.position);
                break;
            }
            case EnemyState.ReturnToSpawn:
            {
                _agent.SetDestination(_spawnPoint);
                break;
            }
            case EnemyState.WaitToReturn:
            {
                _agent.SetDestination(_target.transform.position);

                if (!_isWait)
                    StartCoroutine(WaitToReturn(3));
                break;
            }
        }
    }

    #endregion
}
