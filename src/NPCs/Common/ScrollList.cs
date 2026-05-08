using UnityEngine;
using UnityEngine.UI;

namespace NPCs.Common
{
	/// <summary>
	/// A scrollable list within a worldspace display.
	/// Rows are added via AddRow and the list scrolls by one row height per input.
	/// </summary>
	internal class ScrollList
	{
		private readonly RectTransform _viewport;
		private readonly RectTransform _content;
		private readonly Button _upButton;
		private readonly Button _downButton;
		private readonly float _rowHeight;
		private readonly int _visibleRows;
		private readonly Vector2 _canvasSize;

		private int _scrollIndex = 0;
		private int _totalRows = 0;

		public ScrollList(RectTransform viewport, RectTransform content, Button upButton, Button downButton, float rowHeight, int visibleRows, Vector2 canvasSize)
		{
			_viewport = viewport;
			_content = content;
			_upButton = upButton;
			_downButton = downButton;
			_rowHeight = rowHeight;
			_visibleRows = visibleRows;
			_canvasSize = canvasSize;
			_content.anchoredPosition = Vector2.zero;

			UpdateArrows();
		}

		/// <summary>
		/// Adds a row to the scroll list and returns its RectTransform for further layout.
		/// </summary>
		/// <returns>The row RectTransform.</returns>
		public RectTransform AddRow()
		{
			float rowHeightPixels = _rowHeight * (_canvasSize.y / 100f);

			GameObject row = new GameObject($"Row_{_totalRows}");
			row.transform.SetParent(_content, false);

			RectTransform rt = row.AddComponent<RectTransform>();
			rt.anchorMin = new Vector2(0f, 1f);
			rt.anchorMax = new Vector2(1f, 1f);
			rt.pivot = new Vector2(0.5f, 1f);
			rt.sizeDelta = new Vector2(0f, rowHeightPixels);
			rt.anchoredPosition = new Vector2(0f, -_totalRows * rowHeightPixels);

			_totalRows++;
			_content.sizeDelta = new Vector2(_content.sizeDelta.x, _totalRows * rowHeightPixels);

			UpdateArrows();
			return rt;
		}

		/// <summary>
		/// Scrolls the list up by one row.
		/// </summary>
		public void ScrollUp()
		{
			if (_scrollIndex <= 0) return;
			_scrollIndex--;
			ApplyScroll();
		}

		/// <summary>
		/// Scrolls the list down by one row.
		/// </summary>
		public void ScrollDown()
		{
			if (_scrollIndex >= MaxScroll()) return;
			_scrollIndex++;
			ApplyScroll();
		}

		/// <summary>
		/// Resets the scroll position to the top.
		/// </summary>
		public void Reset()
		{
			_scrollIndex = 0;
			ApplyScroll();
		}

		private void ApplyScroll()
		{
			float rowHeightPixels = _rowHeight * (_canvasSize.y / 100f);
			_content.anchoredPosition = new Vector2(0f, _scrollIndex * rowHeightPixels);
			UpdateArrows();
		}

		private void UpdateArrows()
		{
			_upButton.interactable = _scrollIndex > 0;
			_downButton.interactable = _scrollIndex < MaxScroll();
		}

		private int MaxScroll() => Mathf.Max(0, _totalRows - _visibleRows);
	}
}
