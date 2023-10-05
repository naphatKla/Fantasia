using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AttackState
{
    AttackState0,
    AttackState1,
    AttackState2,
}

[System.Serializable]
public struct AttackPattern
{
    public float power;
    public float delay;
    public float cooldown;
    public AttackState attackState;
}
public class CombatSystem : MonoBehaviour
{
    #region Declare Variable
    [Header("Attack Stats & Config")]
    [SerializeField] protected float attackStat;
    [SerializeField] protected Transform attackPoint;
    [SerializeField] protected Vector2 attackArea;
    [SerializeField] protected LayerMask targetLayer;
    protected List<Collider2D> targetInAttackArea;
    
    [Header("Normal Attack")]
    [SerializeField] protected List<AttackPattern> attackPatterns;
    private AttackPattern _currentAttackPattern;
    [SerializeField] private AttackState _attackState;
    private Coroutine _attackCoroutine;
    private Animator _animator;
    private float _lastAttackTime;
    #endregion
    
    protected virtual void Start()
    {
        _attackState = AttackState.AttackState0;
        _currentAttackPattern = attackPatterns[0];
        _animator = GetComponent<Animator>();
    }
    
    protected virtual void Update()
    {
        targetInAttackArea = Physics2D.OverlapBoxAll(attackPoint.position, attackArea, 0, targetLayer).ToList();
        AttackHandle();
    }

    #region Methods
    
    
    /// <summary>
    /// use for handle the attack system.
    /// Please override this method with attack condition before call this method.
    /// </summary>
    protected virtual void AttackHandle()
    {
        if (Time.time < _lastAttackTime + _currentAttackPattern.cooldown) return;   // cooldown check.
        if (_attackCoroutine != null) return;
        
        _currentAttackPattern = attackPatterns[(int)_attackState];
        _attackCoroutine = StartCoroutine(Attack(_currentAttackPattern.delay));
    }
    
    
    // ReSharper disable Unity.PerformanceAnalysis
    /// <summary>
    /// Attack target with delay.
    /// </summary>
    /// <param name="delay">delay time (sec)</param>
    /// <returns></returns>
    protected virtual IEnumerator Attack(float delay)
    {
        _lastAttackTime = Time.time;
        _animator.SetTrigger(_attackState.ToString());
        
        yield return new WaitForSeconds(delay);
        
        List<HealthSystem> targetHealthSystems = targetInAttackArea.Select(target => target.GetComponent<HealthSystem>()).ToList();
        targetHealthSystems.ForEach(target => target.TakeDamage(_currentAttackPattern.power * attackStat,gameObject));
        
        // if attackState is the last state or the last attack is longer than 2 second, reset attackState to 0.
        if ((int)_attackState >= attackPatterns.Count-1 || Time.time - _lastAttackTime > 2)
            _attackState = AttackState.AttackState0;
        else
            _attackState++;
        
        _attackCoroutine = null;
    }
    
    
    /// <summary>
    /// Cancel attacking.
    /// </summary>
    protected void CancelAttacking()
    {
        if (_attackCoroutine == null) return;
        StopCoroutine(_attackCoroutine);
        _attackCoroutine = null;
    }
    
    
    /// <summary>
    /// Just drawn gizmos on the inspector.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!attackPoint) return;
        Gizmos.DrawWireCube(attackPoint.position,attackArea);
    }
    #endregion
}
