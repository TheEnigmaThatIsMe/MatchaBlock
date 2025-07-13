using TMPro;
using UnityEngine;
using DG.Tweening;

namespace Match3 {
    public class ScoreUI : MonoBehaviour {
        [SerializeField] private Match3 match3;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private string scoreFormat = "Score: {0}";
        [SerializeField] private string highScoreFormat = "High Score: {0}";

        [Header("Score Animation")]
        [SerializeField] private float scorePunchScale = 1.2f;
        [SerializeField] private float scorePunchDuration = 0.3f;
        
        [Header("Bonus Text")]
        [SerializeField] private GameObject bonusTextPrefab;
        [SerializeField] private float bonusFloatDistance = 1f;
        [SerializeField] private float bonusFloatDuration = 1f;
        [SerializeField] private Ease bonusFloatEase = Ease.OutBack;
        [SerializeField] private Color bonusTextColor = Color.yellow;

        private void Awake() {
            // Ensure TextMeshPro components are properly set up
            if (scoreText != null) {
                scoreText.text = string.Format(scoreFormat, 0);
                EnsureTextVisible(scoreText);
            } else {
                Debug.LogError("Score Text component not assigned to ScoreUI!", this);
            }

            if (highScoreText != null) {
                highScoreText.text = string.Format(highScoreFormat, 0);
                EnsureTextVisible(highScoreText);
            } else {
                Debug.LogError("High Score Text component not assigned to ScoreUI!", this);
            }
        }

        private void Start() {
            if (match3 == null) {
                Debug.LogError("Match3 component not assigned to ScoreUI!", this);
                return;
            }

            // Set initial scores
            UpdateScoreDisplay(match3.CurrentScore);
            UpdateHighScoreDisplay(match3.HighScore);
            
            // Subscribe to score changes
            match3.OnScoreChanged.AddListener(UpdateScoreDisplay);
            match3.OnHighScoreChanged.AddListener(UpdateHighScoreDisplay);
            match3.OnScoreBonus.AddListener(ShowBonusPoints);
        }

        private void EnsureTextVisible(TextMeshProUGUI text) {
            // Ensure the text has proper settings for visibility
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = text.color.WithAlpha(1f); // Ensure full opacity
            text.enabled = true;

            // Ensure the RectTransform is properly sized
            var rectTransform = text.GetComponent<RectTransform>();
            if (rectTransform != null) {
                rectTransform.sizeDelta = new Vector2(4f, 1.2f); // Default size if none set
            }
        }

        private void OnDestroy() {
            if (match3 != null) {
                match3.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
                match3.OnHighScoreChanged.RemoveListener(UpdateHighScoreDisplay);
                match3.OnScoreBonus.RemoveListener(ShowBonusPoints);
            }
        }

        private void UpdateScoreDisplay(int newScore) {
            if (scoreText != null) {
                scoreText.text = string.Format(scoreFormat, newScore);
                // Animate score text
                scoreText.transform
                    .DOPunchScale(Vector3.one * scorePunchScale, scorePunchDuration)
                    .SetEase(Ease.OutElastic);
            }
        }

        private void UpdateHighScoreDisplay(int newHighScore) {
            if (highScoreText != null) {
                highScoreText.text = string.Format(highScoreFormat, newHighScore);
                // Animate high score text
                highScoreText.transform
                    .DOPunchScale(Vector3.one * scorePunchScale, scorePunchDuration)
                    .SetEase(Ease.OutElastic);
            }
        }

        private void ShowBonusPoints(int points, Vector3 worldPosition) {
            if (bonusTextPrefab == null) {
                Debug.LogWarning("Bonus Text Prefab not assigned to ScoreUI!", this);
                return;
            }

            // Create bonus text at the match position
            var bonusGO = Instantiate(bonusTextPrefab, transform);
            var bonusText = bonusGO.GetComponent<TextMeshProUGUI>();
            
            if (bonusText != null) {
                // Set text and color
                bonusText.text = $"+{points}";
                bonusText.color = bonusTextColor;
                EnsureTextVisible(bonusText);
                
                // Convert world position to screen position
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
                bonusGO.transform.position = screenPos;
                
                // Animate the bonus text
                var targetPos = screenPos + Vector3.up * bonusFloatDistance;
                
                Sequence bonusSequence = DOTween.Sequence();
                bonusSequence.Append(bonusGO.transform.DOMove(targetPos, bonusFloatDuration).SetEase(bonusFloatEase))
                            .Join(bonusText.DOFade(0, bonusFloatDuration))
                            .OnComplete(() => Destroy(bonusGO));
            }
        }
    }

    public static class ColorExtensions {
        public static Color WithAlpha(this Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
} 