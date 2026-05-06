using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NPCs.Common
{
	/// <summary>
	/// A worldspace interactive canvas supporting button layout and player interaction
	/// via the game's existing E-key interact system.
	/// </summary>
	internal class WorldspaceInteractiveDisplay : MonoBehaviour
	{
		private Canvas _canvas;
		private RectTransform _canvasRect;
		private Vector2 _canvasSize;
		private Camera _cam;
		private PointerEventData _eventData;
		private List<RaycastResult> _raycastResults = new List<RaycastResult>();
		private List<Button> _buttons = new List<Button>();
		private List<GameObject> _labels = new List<GameObject>();
		private bool _isReady = false;
		private bool _isVisible = false;
		private float _interactDistance = 20f;

		private Vector3 _position;
		private Vector2 _size = new Vector2(400f, 600f);

		public void SetPosition(Vector3 position) => _position = position;
		public void SetSize(Vector2 size) => _size = size;
		public void SetInteractDistance(float distance) => _interactDistance = distance;

		public void Init()
		{
			_cam = mainscript.M.player.Cam;

			GameObject canvasObj = new GameObject("WorldspaceInteractiveDisplay");
			canvasObj.transform.SetParent(transform, false);

			_canvas = canvasObj.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.worldCamera = _cam;
			canvasObj.AddComponent<CanvasScaler>();
			canvasObj.AddComponent<GraphicRaycaster>();

			_canvasRect = _canvas.GetComponent<RectTransform>();
			_canvasRect.sizeDelta = _size;
			_canvasRect.localPosition = _position;
			_canvasRect.localRotation = Quaternion.identity;
			_canvasRect.localScale = Vector3.one * 0.0025f;
			_canvasRect.anchorMin = _canvasRect.anchorMax = _canvasRect.pivot = new Vector2(0.5f, 0.5f);

			GameObject bg = new GameObject("Background");
			bg.transform.SetParent(canvasObj.transform, false);
			RectTransform bgRect = bg.AddComponent<RectTransform>();
			bgRect.anchorMin = Vector2.zero;
			bgRect.anchorMax = Vector2.one;
			bgRect.offsetMin = Vector2.zero;
			bgRect.offsetMax = Vector2.zero;
			Image bgImage = bg.AddComponent<Image>();
			bgImage.color = new Color(0f, 0f, 0f, 0.6f);

			_isReady = true;
		}

		private void LateUpdate()
		{
			if (!_isReady || !_isVisible) return;

			// Always face camera.
			_canvas.transform.rotation = Quaternion.LookRotation(
				_canvas.transform.position - _cam.transform.position
			);

			float distance = Vector3.Distance(mainscript.M.player.transform.position, _canvas.transform.position);
			if (distance > _interactDistance)
			{
				_canvas.gameObject.SetActive(false);
				return;
			}

			if (!_canvas.gameObject.activeSelf)
				_canvas.gameObject.SetActive(true);

			HandleInteraction();
		}

		private void HandleInteraction()
		{
			fpscontroller player = mainscript.M.player;

			_eventData = new PointerEventData(EventSystem.current)
			{
				position = new Vector2(Screen.width / 2f, Screen.height / 2f)
			};

			_raycastResults.Clear();
			_canvas.GetComponent<GraphicRaycaster>().Raycast(_eventData, _raycastResults);

			if (_raycastResults.Count == 0) return;

			foreach (var result in _raycastResults)
			{
				Button button = result.gameObject.GetComponent<Button>();
				if (button == null || !button.interactable) continue;

				player.E = button.name;
				player.BcanE = true;

				if (Input.GetKeyDown(KeyCode.E))
					button.onClick.Invoke();
			}
		}

		/// <summary>
		/// Shows the display.
		/// </summary>
		public void Show()
		{
			_isVisible = true;
			_canvas.gameObject.SetActive(true);
		}

		/// <summary>
		/// Hides the display.
		/// </summary>
		public void Hide()
		{
			_isVisible = false;
			_canvas.gameObject.SetActive(false);
		}

		/// <summary>
		/// Clears all buttons from the display.
		/// </summary>
		public void Clear()
		{
			foreach (Button button in _buttons)
				Destroy(button.gameObject);
			foreach (GameObject label in _labels)
				Destroy(label);
			_buttons.Clear();
			_labels.Clear();
		}

		/// <summary>
		/// Creates a button on the display using percentage-based positioning.
		/// </summary>
		/// <param name="label">Text shown on the button.</param>
		/// <param name="interactLabel">Text shown in the player's interact prompt.</param>
		/// <param name="rect">Position and size as a percentage of canvas size.</param>
		/// <param name="onClick">Action invoked when the button is selected.</param>
		/// <returns>The created button.</returns>
		public Button CreateButton(string label, string interactLabel, RectPercent rect, System.Action onClick)
		{
			GameObject obj = new GameObject(interactLabel);
			obj.transform.SetParent(_canvas.transform, false);

			Button button = obj.AddComponent<Button>();
			Image image = obj.AddComponent<Image>();
			button.targetGraphic = image;
			button.interactable = true;
			image.color = new Color(0f, 0f, 0f, 0.6f);

			RectTransform rt = obj.GetComponent<RectTransform>();
			SetRect(rt, rect);

			GameObject labelObj = new GameObject("Label");
			labelObj.transform.SetParent(obj.transform, false);
			TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
			text.text = label;
			text.fontSize = 28f;
			text.alignment = TextAlignmentOptions.Center;
			text.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");

			RectTransform labelRect = labelObj.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

			button.onClick.AddListener(() => onClick?.Invoke());
			_buttons.Add(button);
			return button;
		}

		/// <summary>
		/// Creates a non-interactive text label on the display.
		/// </summary>
		/// <param name="text">Text to display.</param>
		/// <param name="rect">Position and size as a percentage of canvas size.</param>
		/// <returns>The created TMP text component for later updates.</returns>
		public TextMeshProUGUI CreateLabel(string text, RectPercent rect)
		{
			GameObject obj = new GameObject("Label");
			obj.transform.SetParent(_canvas.transform, false);

			TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = 28f;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			tmp.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");

			SetRect(obj.GetComponent<RectTransform>(), rect);
			_labels.Add(obj);
			return tmp;
		}

		private void SetRect(RectTransform rt, RectPercent rect)
		{
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
			rt.sizeDelta = new Vector2(
				(rect.Width / 100f) * _size.x,
				(rect.Height / 100f) * _size.y
			);
			rt.anchoredPosition = new Vector2(
				(rect.X / 100f) * _size.x - _size.x / 2f,
				-((rect.Y / 100f) * _size.y - _size.y / 2f)
			);
		}
	}
}