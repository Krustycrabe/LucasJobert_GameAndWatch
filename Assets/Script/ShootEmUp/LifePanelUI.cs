using UnityEngine;

/// <summary>
/// Animates the LifePanel RectTransform sliding to the right whenever the player loses a life.
/// The ScorePanel (rendered on top) progressively covers the hearts as the panel slides right.
/// Place this component on the LifePanel GameObject.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LifePanelUI : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    [SerializeField] private PlayerHealth playerHealth;

    private RectTransform _rectTransform;
    private float _startX;
    private float _targetX;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startX  = _rectTransform.anchoredPosition.x;
        _targetX = _startX;
    }

    private void OnEnable()  => playerHealth.OnDamaged += HandleDamaged;
    private void OnDisable() => playerHealth.OnDamaged -= HandleDamaged;

    private void Update()
    {
        Vector2 pos = _rectTransform.anchoredPosition;
        if (Mathf.Abs(pos.x - _targetX) < 0.5f)
        {
            pos.x = _targetX;
            _rectTransform.anchoredPosition = pos;
            return;
        }
        pos.x = Mathf.Lerp(pos.x, _targetX, gameData.lifePanelLerpSpeed * Time.deltaTime);
        _rectTransform.anchoredPosition = pos;
    }

    private void HandleDamaged(int currentLives, PlayerHealth.DamageSource _)
    {
        int livesLost = gameData.maxLives - currentLives;
        _targetX = _startX + livesLost * gameData.lifePanelStepX;
    }
}
