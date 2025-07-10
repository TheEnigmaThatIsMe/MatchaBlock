using UnityEngine;

namespace Match3 {
    [RequireComponent(typeof(SpriteRenderer))]
    public class Gem : MonoBehaviour {
        public GemType type;
        private SpriteRenderer spriteRenderer;

        private void Awake() {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetType(GemType type) {
            this.type = type;
            spriteRenderer.sprite = type.sprite;
        }
        
        public GemType GetType() => type;
    }
}