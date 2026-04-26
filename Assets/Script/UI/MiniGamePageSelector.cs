using System.Collections;
using UnityEngine;

/// <summary>
/// Contrôle le scroll du ContentSlider dans le MiniGameMenu.
/// Chaque page occupe une hauteur fixe (pageHeight).
/// Appeler SelectPage(int) pour animer vers la page voulue.
/// </summary>
public class MiniGamePageSelector : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Le RectTransform du ContentSlider à animer.")]
    [SerializeField] private RectTransform contentSlider;

    [Header("Configuration")]
    [Tooltip("Hauteur en pixels d'une page (doit correspondre à la hauteur du ContentViewport).")]
    [SerializeField] private float pageHeight = 600f;

    [Tooltip("Durée de l'animation de scroll en secondes.")]
    [SerializeField] private float scrollDuration = 0.35f;

    [Tooltip("Courbe d'interpolation du scroll (EaseInOut recommandé).")]
    [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private int _currentPage;
    private Coroutine _scrollCoroutine;

    /// <summary>Index de la page actuellement affichée.</summary>
    public int CurrentPageIndex => _currentPage;

    /// <summary>
    /// Sélectionne une page par son index (0 = Heart, 1 = Vessel, 2 = Brain).
    /// Branchable sur les boutons BTN_Heart, BTN_Vessel, BTN_Brain via OnClick.
    /// </summary>
    public void SelectPage(int pageIndex)
    {
        if (contentSlider == null)
        {
            Debug.LogWarning("[MiniGamePageSelector] ContentSlider non assigné.");
            return;
        }

        _currentPage = pageIndex;
        float targetY = pageIndex * pageHeight;

        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);

        _scrollCoroutine = StartCoroutine(ScrollToPage(targetY));
    }

    private IEnumerator ScrollToPage(float targetY)
    {
        float startY = contentSlider.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scrollDuration);
            float curvedT = scrollCurve.Evaluate(t);
            float currentY = Mathf.Lerp(startY, targetY, curvedT);

            contentSlider.anchoredPosition = new Vector2(contentSlider.anchoredPosition.x, currentY);
            yield return null;
        }

        contentSlider.anchoredPosition = new Vector2(contentSlider.anchoredPosition.x, targetY);
        _scrollCoroutine = null;
    }
}
