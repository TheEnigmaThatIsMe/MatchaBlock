using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3 {
    public class Match3 : MonoBehaviour {
        [SerializeField] int width = 8;
        [SerializeField] int height = 8;
        [SerializeField] float cellSize = 1f;
        [SerializeField] Vector3 originPosition = Vector3.zero;
        [SerializeField] bool debug = true;
        
        // Public properties for CameraController
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 OriginPosition => originPosition;

        [SerializeField] Gem gemPrefab;
        [SerializeField] GemType[] gemTypes;
        [SerializeField] Ease ease = Ease.InQuad;
        [SerializeField] GameObject explosion;
        
        InputReader inputReader;
        AudioManager audioManager;

        GridSystem2D<GridObject<Gem>> grid;

        Vector2Int selectedGem = Vector2Int.one * -1;

        void Awake() {
            inputReader = GetComponent<InputReader>();
            audioManager = GetComponent<AudioManager>();
        }
        
        void Start() {
            InitializeGrid();
            inputReader.Fire += OnSelectGem;
        }

        void OnDestroy() {
            inputReader.Fire -= OnSelectGem;
        }

        void OnSelectGem() {
            var gridPos = grid.GetXY(Camera.main.ScreenToWorldPoint(inputReader.Selected));
            
            if (!IsValidPosition(gridPos) || IsEmptyPosition(gridPos)) return;
            
            if (selectedGem == gridPos) {
                DeselectGem();
                audioManager.PlayDeselect();
            } else if (selectedGem == Vector2Int.one * -1) {
                SelectGem(gridPos);
                audioManager.PlayClick();
            } else if (AreGemsAdjacent(selectedGem, gridPos)) {
                StartCoroutine(RunGameLoop(selectedGem, gridPos));
            } else {
                // Not adjacent, deselect current and select new
                DeselectGem();
                SelectGem(gridPos);
                audioManager.PlayClick();
            }
        }

        bool AreGemsAdjacent(Vector2Int posA, Vector2Int posB) {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y) == 1;
        }

        IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB) {
            // Perform the swap
            yield return StartCoroutine(SwapGems(gridPosA, gridPosB));
            
            // Check for matches
            List<Vector2Int> matches = FindMatches();
            
            if (matches.Count == 0) {
                // No matches found, swap back
                yield return StartCoroutine(SwapGems(gridPosB, gridPosA));
                audioManager.PlayNoMatch();
            } else {
                // Process initial matches and any chain reactions
                yield return StartCoroutine(ProcessMatches(matches));
            }

            DeselectGem();
        }

        IEnumerator ProcessMatches(List<Vector2Int> initialMatches) {
            List<Vector2Int> currentMatches = initialMatches;
            
            while (currentMatches.Count > 0) {
                // Process current set of matches
                yield return StartCoroutine(ExplodeGems(currentMatches));
                yield return StartCoroutine(MakeGemsFall());
                yield return StartCoroutine(FillEmptySpots());
                
                // Check for any remaining matches
                currentMatches = FindMatches();
                if (currentMatches.Count > 0) {
                    audioManager.PlayMatch();
                }
            }
        }

        IEnumerator FillEmptySpots() {
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    if (grid.GetValue(x, y) == null) {
                        CreateGem(x, y);
                        audioManager.PlayPop();
                        yield return new WaitForSeconds(0.1f);
                        
                        // Check for matches after each new gem is created
                        var matches = FindMatches();
                        if (matches.Count > 0) {
                            yield return StartCoroutine(ProcessMatches(matches));
                            // After processing matches, we need to restart filling from the beginning
                            x = 0;
                            y = -1; // Will be incremented to 0 by the loop
                        }
                    }
                }
            }
        }

        IEnumerator MakeGemsFall() {
            for (var x = 0; x < width; x++) {
                int shiftDown = 0;
                List<(Gem gem, int distance)> fallingGems = new();

                // Process column from bottom to top
                for (var y = 0; y < height; y++) {
                    if (grid.GetValue(x, y) == null) {
                        shiftDown++;
                    }
                    else if (shiftDown > 0) {
                        var gem = grid.GetValue(x, y).GetValue();
                        fallingGems.Add((gem, shiftDown));
                        grid.SetValue(x, y - shiftDown, grid.GetValue(x, y));
                        grid.SetValue(x, y, null);
                    }
                }

                // Animate all gems in the column simultaneously
                if (fallingGems.Count > 0) {
                    foreach (var (gem, distance) in fallingGems) {
                        gem.transform
                            .DOLocalMove(gem.transform.localPosition + Vector3.down * (cellSize * distance), 0.5f)
                            .SetEase(ease);
                        audioManager.PlayWoosh();
                    }
                    yield return new WaitForSeconds(0.5f);
                    
                    // Check for matches after gems fall in this column
                    var matches = FindMatches();
                    if (matches.Count > 0) {
                        yield return StartCoroutine(ProcessMatches(matches));
                        // After processing matches, we need to stop since the grid has changed
                        yield break;
                    }
                }
            }
        }

        IEnumerator ExplodeGems(List<Vector2Int> matches) {
            

            foreach (var match in matches) {
                var gem = grid.GetValue(match.x, match.y).GetValue();
                grid.SetValue(match.x, match.y, null);

                ExplodeVFX(match);
                audioManager.PlayPop();
                
                gem.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.5f);
                
                yield return new WaitForSeconds(0.1f);
                
                Destroy(gem.gameObject, 0.1f);
            }
        }

        void ExplodeVFX(Vector2Int match) {
            // TODO: Pool
            var fx = Instantiate(explosion, transform);
            var position = grid.GetWorldPositionCenter(match.x, match.y);
            position.z = -1f; // Position the effect in front of the gems
            fx.transform.position = position;
            Destroy(fx, 5f);
        }

        List<Vector2Int> FindMatches() {
            HashSet<Vector2Int> matches = new();
            
            // Horizontal
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width - 2; x++) {
                    var gemA = grid.GetValue(x, y);
                    var gemB = grid.GetValue(x + 1, y);
                    var gemC = grid.GetValue(x + 2, y);
                    
                    if (gemA == null || gemB == null || gemC == null) continue;
                    
                    if (gemA.GetValue().GetType() == gemB.GetValue().GetType() 
                        && gemB.GetValue().GetType() == gemC.GetValue().GetType()) {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x + 1, y));
                        matches.Add(new Vector2Int(x + 2, y));
                    }
                }
            }
            
            // Vertical
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height - 2; y++) {
                    var gemA = grid.GetValue(x, y);
                    var gemB = grid.GetValue(x, y + 1);
                    var gemC = grid.GetValue(x, y + 2);
                    
                    if (gemA == null || gemB == null || gemC == null) continue;
                    
                    if (gemA.GetValue().GetType() == gemB.GetValue().GetType() 
                        && gemB.GetValue().GetType() == gemC.GetValue().GetType()) {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x, y + 1));
                        matches.Add(new Vector2Int(x, y + 2));
                    }
                }
            }

            if (matches.Count == 0) {
                audioManager.PlayNoMatch();
            } else {
                audioManager.PlayMatch();
            }
            
            return new List<Vector2Int>(matches);
        }

        IEnumerator SwapGems(Vector2Int gridPosA, Vector2Int gridPosB) {
            DeselectGem();
            var gridObjectA = grid.GetValue(gridPosA.x, gridPosA.y);
            var gridObjectB = grid.GetValue(gridPosB.x, gridPosB.y);
            
            // Animate both gems simultaneously
            var moveA = gridObjectA.GetValue().transform
                .DOLocalMove(grid.GetWorldPositionCenter(gridPosB.x, gridPosB.y), 0.3f)
                .SetEase(ease);
            
            var moveB = gridObjectB.GetValue().transform
                .DOLocalMove(grid.GetWorldPositionCenter(gridPosA.x, gridPosA.y), 0.3f)
                .SetEase(ease);
            
            // Update grid positions
            grid.SetValue(gridPosA.x, gridPosA.y, gridObjectB);
            grid.SetValue(gridPosB.x, gridPosB.y, gridObjectA);
            
            // Wait for both animations to complete
            yield return moveA.WaitForCompletion();
        }

        void CreateGem(int x, int y, bool checkForMatches = true) {
            GemType selectedType;
            if (checkForMatches) {
                // Get a list of gem types that wouldn't create a match
                var validTypes = GetValidGemTypes(x, y);
                selectedType = validTypes[Random.Range(0, validTypes.Count)];
            } else {
                selectedType = gemTypes[Random.Range(0, gemTypes.Length)];
            }

            var gem = Instantiate(gemPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
            gem.SetType(selectedType);
            var gridObject = new GridObject<Gem>(grid, x, y);
            gridObject.SetValue(gem);
            grid.SetValue(x, y, gridObject);
        }

        List<GemType> GetValidGemTypes(int x, int y) {
            List<GemType> validTypes = new List<GemType>();
            
            // Try each gem type
            foreach (var gemType in gemTypes) {
                if (!WouldCreateMatch(x, y, gemType)) {
                    validTypes.Add(gemType);
                }
            }
            
            // If no valid types (shouldn't happen with 5+ gem types), return all types
            if (validTypes.Count == 0) {
                validTypes.AddRange(gemTypes);
            }
            
            return validTypes;
        }

        bool WouldCreateMatch(int x, int y, GemType typeToCheck) {
            // Check horizontal matches
            if (x >= 2) {
                var gem1 = grid.GetValue(x - 2, y)?.GetValue();
                var gem2 = grid.GetValue(x - 1, y)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }
            if (x >= 1 && x < width - 1) {
                var gem1 = grid.GetValue(x - 1, y)?.GetValue();
                var gem2 = grid.GetValue(x + 1, y)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }
            if (x < width - 2) {
                var gem1 = grid.GetValue(x + 1, y)?.GetValue();
                var gem2 = grid.GetValue(x + 2, y)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }

            // Check vertical matches
            if (y >= 2) {
                var gem1 = grid.GetValue(x, y - 2)?.GetValue();
                var gem2 = grid.GetValue(x, y - 1)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }
            if (y >= 1 && y < height - 1) {
                var gem1 = grid.GetValue(x, y - 1)?.GetValue();
                var gem2 = grid.GetValue(x, y + 1)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }
            if (y < height - 2) {
                var gem1 = grid.GetValue(x, y + 1)?.GetValue();
                var gem2 = grid.GetValue(x, y + 2)?.GetValue();
                if (gem1 != null && gem2 != null &&
                    gem1.GetType() == typeToCheck && gem2.GetType() == typeToCheck) {
                    return true;
                }
            }

            return false;
        }

        void InitializeGrid() {
            grid = GridSystem2D<GridObject<Gem>>.VerticalGrid(width, height, cellSize, originPosition, debug);
            
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    CreateGem(x, y, true); // Use match checking for initial population
                }
            }
        }

        void DeselectGem() {
            if (selectedGem != Vector2Int.one * -1) {
                var gem = grid.GetValue(selectedGem.x, selectedGem.y)?.GetValue();
                gem?.SetHighlight(false);
            }
            selectedGem = Vector2Int.one * -1;
        }

        void SelectGem(Vector2Int gridPos) {
            selectedGem = gridPos;
            var gem = grid.GetValue(gridPos.x, gridPos.y)?.GetValue();
            gem?.SetHighlight(true);
        }

        bool IsEmptyPosition(Vector2Int gridPosition) => grid.GetValue(gridPosition.x, gridPosition.y) == null;

        bool IsValidPosition(Vector2 gridPosition) {
            return gridPosition.x >= 0 && gridPosition.x < width && gridPosition.y >= 0 && gridPosition.y < height;
        }
    }
}