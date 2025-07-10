using UnityEngine;

namespace Match3 {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
        [SerializeField] private Match3 match3;
        [SerializeField] private float padding = 1f; // Extra space around the grid

        private Camera mainCamera;
        private float aspectRatio;
        private float gridWidth;
        private float gridHeight;

        private void Awake() {
            mainCamera = GetComponent<Camera>();
            
            // Get grid dimensions from Match3
            gridWidth = match3.Width * match3.CellSize;
            gridHeight = match3.Height * match3.CellSize;
        }

        private void Start() {
            AdjustCamera();
        }

        private void Update() {
            // Check if screen size has changed (e.g., device rotation)
            if (aspectRatio != (float)Screen.width / Screen.height) {
                AdjustCamera();
            }
        }

        private void AdjustCamera() {
            aspectRatio = (float)Screen.width / Screen.height;
            
            // Calculate the orthographic size needed to fit the grid
            float gridAspect = gridWidth / gridHeight;
            
            if (gridAspect > aspectRatio) {
                // Grid is wider than screen aspect ratio - fit to width
                mainCamera.orthographicSize = (gridWidth / aspectRatio) * 0.5f + padding;
            } else {
                // Grid is taller than screen aspect ratio - fit to height
                mainCamera.orthographicSize = gridHeight * 0.5f + padding;
            }

            // Center the camera on the grid
            Vector3 gridCenter = match3.OriginPosition + new Vector3(gridWidth * 0.5f, gridHeight * 0.5f, -10f);
            transform.position = gridCenter;
        }
    }
} 