using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Animator))]

public class PlayerController : MonoBehaviour, IDamageable, IHealthable
{
    [Header("References")]
    [HideInInspector]
    public Rigidbody2D rb;

    public PlayerParameters playerParams;

    private SpriteRenderer rbSprite;

    private Collider2D collider;

    private PlayerAction _input;

    public float health { get => _hp; set => _hp = value; }

    private float _hp;
    // -- Movement

    private Vector2 _direction;
    private bool _isMovementPressed;
    private int _facingDirection;

    // -- Attack
    private bool _isAttacking;
    private bool _canAttack = true;

    // -- Dodge
    private bool _isDodging;
    private bool _canDodge = true;


    // -- ANIMATOR
    private Animator animator;

    private int _isWalkingHash;
    private int _isDodgingHash;
    private int _takeDamageHash;
    private int _isAttackingHash;
    private int _attackTypeHash;
    private int _isDeadHash;

    private int _randomAttack;


    // -- TAKE DAMAGE
    public static event Action<float> onDamage;

    private void Awake()
    {

        //-- REFERENCES INSTANTIATION 
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();

        rbSprite = GetComponent<SpriteRenderer>();

        collider = GetComponent<Collider2D>();

        // --- INPUT SETUP
        _input = new PlayerAction();
        _input.PlayerController.Movement.started += onMovementInput;
        _input.PlayerController.Movement.performed += onMovementInput;
        _input.PlayerController.Movement.canceled += onMovementInput;

        _input.PlayerController.Attack.started += onAttackInput;
        _input.PlayerController.Attack.performed += onAttackInput;
        _input.PlayerController.Attack.canceled += onAttackInput;

        _input.PlayerController.Dodge.performed += onDodgeInput;
    }


    void onMovementInput(InputAction.CallbackContext context)
    {
        Vector2 tmp = context.ReadValue<Vector2>();
        _direction = new Vector2(tmp.x, tmp.y);
        _isMovementPressed = _direction != Vector2.zero;
        _facingDirection = (_isMovementPressed)
            ? (_direction.x >= 0) ? 1 : -1
            : _facingDirection;
    }

    void onAttackInput(InputAction.CallbackContext context)
    {
        _isAttacking = context.ReadValueAsButton();
    }

    void onDodgeInput(InputAction.CallbackContext context)
    {
        _isDodging = context.ReadValueAsButton();
    }

    void Start()
    {
        _isWalkingHash = Animator.StringToHash("isWalking");
        _isDodgingHash = Animator.StringToHash("isDodging");
        _isAttackingHash = Animator.StringToHash("isAttacking");
        _attackTypeHash = Animator.StringToHash("attackType");
        _takeDamageHash = Animator.StringToHash("takeDamage");
        _isDeadHash = Animator.StringToHash("isDead");

        _hp = playerParams.hp;
    }

    private void Update()
    {
        StartAttack();
        Dodge();
    }

    private void FixedUpdate()
    {
        Move();
    }


    private void Move()
    {
        rb.MovePosition(rb.position + _direction * playerParams.speed * Time.fixedDeltaTime);
        rbSprite.flipX = _facingDirection < 0;
        animator.SetBool(_isWalkingHash, _direction != Vector2.zero);
    }

    #region Attack
    private void StartAttack()
    {
        if (!_canAttack || !_isAttacking)
            return;

        AttackAnimationHandler();
        StartCoroutine(ResetAttack(playerParams.attackCooldown));
    }

    private void AttackHit()
    {
        Array.ForEach(OnHit, collider =>
        {
            Debug.Log(collider);
            CombatManager.instance.ApplyDmgOutput(collider, playerParams.attackDmg);
        });
    }

    private Collider2D[] OnHit => Physics2D.OverlapCircleAll((Vector2)transform.position + _direction,
        playerParams.attackRadius, playerParams.enemyMask);

    IEnumerator ResetAttack(float attackCD)
    {
        _canAttack = false;
        yield return new WaitForSeconds(attackCD);
        _canAttack = true;
    }

    private void AttackAnimationHandler()
    {
        if (_isAttacking)
        {
            _randomAttack = Random.Range(0, 3);
            animator.SetTrigger(_isAttackingHash);
            animator.SetInteger(_attackTypeHash, _randomAttack);
        }

    }

    #endregion

    #region Dodge


    private void Dodge()
    {
        if (!_isDodging)
            return;

        animator.SetTrigger(_isDodgingHash);
        StartCoroutine(DodgeReset());
    }

    IEnumerator DodgeReset()
    {
        _canDodge = false;
        _isDodging = true;
        yield return new WaitForSeconds(playerParams.dodgeCooldown);
        _canDodge = true;
        _isDodging = false;

    }


    void DodgeActive()
    {
        collider.enabled = false;
    }


    void DodgeInactive()
    {
        collider.enabled = true;
    }

    #endregion

    #region Input system utils

    private void OnEnable()
    {
        _input.PlayerController.Enable();
    }

    private void OnDisable()
    {
        _input.PlayerController.Disable();
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)transform.position + _direction, playerParams.attackRadius);
    }

    private void TakeDamage(float dmgAmount)
    {
        health -= dmgAmount;
        if (health <= 0)
        {
            Debug.Log("Muori");
            animator.SetBool(_isDeadHash, true);
            return;
        }
        animator.SetTrigger(_takeDamageHash);
    }

    public void ApplyDamage(float dmgAmount)
    {
        onDamage += TakeDamage;
        onDamage += UiManager.ChangeHpValue;
        onDamage?.Invoke(dmgAmount);
    }
}
