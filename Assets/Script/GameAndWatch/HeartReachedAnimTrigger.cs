using System.Collections;
using UnityEngine;
using GameAndWatch.Audio;

/// <summary>
/// Plays the HeartReached animation on the Heart Animator when the player reaches
/// the heart column, waits for it to complete, then fires LivesManager.FirePlayerReset()
/// to respawn the player.
///
/// Attach this to the Heart GameObject that owns the Animator with the HeartReached trigger.
/// </summary>
public class HeartReachedAnimTrigger : MonoBehaviour
{
    private const string HeartReachedTrigger = "HeartReached";
    private const string HeartReachedState   = "HeartReached";
    private const float  AnimationTimeout    = 5f;

    private Animator  _animator;
    private bool      _playing;

    private void Awake() => _animator = GetComponent<Animator>();

    private void OnEnable()  => PlayerController.OnPlayerReachedHeart += HandleHeartReached;
    private void OnDisable() => PlayerController.OnPlayerReachedHeart -= HandleHeartReached;

    private void HandleHeartReached()
    {
        if (_playing) return;
        AudioManager.Instance?.PlayOneShot(SoundIds.GameAndWatch.HeartReached);
        StartCoroutine(PlayHeartReachedRoutine());
    }

    private IEnumerator PlayHeartReachedRoutine()
    {
        _playing = true;

        _animator.SetTrigger(HeartReachedTrigger);

        // Wait one frame for the Animator to process the trigger.
        yield return null;

        // Wait until the HeartReached state is entered.
        float elapsed = 0f;
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName(HeartReachedState))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= AnimationTimeout) { FinishRoutine(); yield break; }
            yield return null;
        }

        // Wait for the state to complete.
        while (_animator.GetCurrentAnimatorStateInfo(0).IsName(HeartReachedState)
               && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        FinishRoutine();
    }

    private void FinishRoutine()
    {
        _playing = false;
        // Respawn the player now that the animation is over.
        // LivesManager.HandleHeartReached originally called OnPlayerReset directly —
        // we replaced that path: LivesManager now does nothing on HeartReached and
        // delegates to this class which calls FirePlayerReset after the animation.
        LivesManager.FirePlayerReset();
    }
}
