using System;
using System.Collections;
using UnityEngine;

public class PlayerHitAnimTrigger : MonoBehaviour
{
    private const string ExplodeTrigger = "Explode";
    private const string ReappearTrigger = "Reappear";
    private const string ExplodeStateName = "Explode";
    private const float AnimationTimeout = 5f;

    /// <summary>Déclenché après que l'animation de mort (game over) est terminée.</summary>
    public static event Action OnDeathAnimationComplete;

    private Animator _animator;

    private void Awake() => _animator = GetComponent<Animator>();

    private void OnEnable()
    {
        LivesManager.OnPlayerNeedsReset += HandleNeedsReset;
        LivesManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        LivesManager.OnPlayerNeedsReset -= HandleNeedsReset;
        LivesManager.OnGameOver -= HandleGameOver;
    }

    private void HandleNeedsReset() => StartCoroutine(ExplodeThenRespawn());
    private void HandleGameOver() => StartCoroutine(ExplodeThenDie());

    /// <summary>Joue l'explosion, reset le joueur, puis joue le respawn en reverse.</summary>
    private IEnumerator ExplodeThenRespawn()
    {
        yield return PlayExplodeAnimation();

        LivesManager.FirePlayerReset();

        _animator.SetTrigger(ReappearTrigger);
    }

    /// <summary>Joue l'explosion sans respawn, puis notifie le game over screen.</summary>
    private IEnumerator ExplodeThenDie()
    {
        yield return PlayExplodeAnimation();

        OnDeathAnimationComplete?.Invoke();
    }

    private IEnumerator PlayExplodeAnimation()
    {
        _animator.SetTrigger(ExplodeTrigger);

        yield return null; // laisse l'animator traiter le trigger

        // Attend l'entrée dans l'état Explode
        float elapsed = 0f;
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName(ExplodeStateName))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= AnimationTimeout) yield break;
            yield return null;
        }

        // Attend la fin de l'état Explode
        while (_animator.GetCurrentAnimatorStateInfo(0).IsName(ExplodeStateName)
               && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }
}
