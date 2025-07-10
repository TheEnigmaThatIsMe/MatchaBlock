using UnityEngine;
using UnityEngine.Serialization;

namespace Match3 {
    [RequireComponent(typeof(SpriteRenderer))]
    public class Gem : MonoBehaviour {
        public GemType type;
        private SpriteRenderer spriteRenderer;
        [SerializeField] private ParticleSystem highlightEffect;

        private void Awake() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            highlightEffect.Stop();
        }

        public void SetType(GemType type) {
            this.type = type;
            spriteRenderer.sprite = type.sprite;
        }
        
        public GemType GetType() => type;

        public void SetHighlight(bool active) {
            if (active) {
                highlightEffect.gameObject.SetActive(true);
                highlightEffect.Play();
            } else {
                highlightEffect.Stop();
                highlightEffect.gameObject.SetActive(false);
            }
        }
    }
}