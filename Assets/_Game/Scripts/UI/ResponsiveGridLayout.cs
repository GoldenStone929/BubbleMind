using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public sealed class ResponsiveGridLayout : MonoBehaviour
    {
        private RectTransform rectTransform;
        private GridLayoutGroup grid;
        private float minimumCellWidth = 300f;
        private float aspectRatio = 1.5f;
        private int minimumColumns = 2;
        private int maximumColumns = 4;
        private float previousWidth = -1f;

        public void Configure(float minCellWidth, float widthToHeight, int minColumns, int maxColumns)
        {
            minimumCellWidth = Mathf.Max(1f, minCellWidth);
            aspectRatio = Mathf.Max(0.1f, widthToHeight);
            minimumColumns = Mathf.Max(1, minColumns);
            maximumColumns = Mathf.Max(minimumColumns, maxColumns);
            Recalculate(true);
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            grid = GetComponent<GridLayoutGroup>();
        }

        private void OnEnable()
        {
            Recalculate(true);
        }

        private void LateUpdate()
        {
            Recalculate(false);
        }

        private void Recalculate(bool force)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (grid == null)
            {
                grid = GetComponent<GridLayoutGroup>();
            }

            float width = rectTransform.rect.width;
            if (!force && Mathf.Abs(width - previousWidth) < 0.5f)
            {
                return;
            }

            previousWidth = width;
            float usable = Mathf.Max(1f, width - grid.padding.horizontal);
            int columns = Mathf.FloorToInt((usable + grid.spacing.x) / (minimumCellWidth + grid.spacing.x));
            columns = Mathf.Clamp(columns, minimumColumns, maximumColumns);
            float cellWidth = Mathf.Max(1f, (usable - grid.spacing.x * (columns - 1)) / columns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(cellWidth, cellWidth / aspectRatio);
        }
    }
}
