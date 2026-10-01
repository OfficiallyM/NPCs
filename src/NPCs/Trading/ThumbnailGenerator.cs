using NPCs.Trading.Core;
using NPCs.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Renders small pictures of trader stock in the condition and colour it will actually spawn with.
	/// Adapted from the M-ultiTool thumbnail generator. Results are cached in memory rather than on disk
	/// because the same item looks different at each condition and colour.
	/// </summary>
	internal static class ThumbnailGenerator
	{
		private const int Size = 200;
		private const int PerFrame = 2;
		private const int MaxCached = 128;
		private const int RenderLayer = 1;

		private struct Pending
		{
			public string Key;
			public TraderItem Item;
		}

		private class Runner : MonoBehaviour { }

		private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
		private static readonly Queue<string> _cacheOrder = new Queue<string>();
		private static readonly Queue<Pending> _queue = new Queue<Pending>();
		private static readonly Dictionary<string, List<Action<Texture2D>>> _waiting = new Dictionary<string, List<Action<Texture2D>>>();
		private static Runner _runner;
		private static bool _processing;

		/// <summary>
		/// Gets the picture for a stock entry.
		/// </summary>
		/// <param name="item">Stock entry to picture, including its condition and colour.</param>
		/// <param name="onGenerated">Called once the picture has been rendered. Not called if it was already cached.</param>
		/// <returns>The cached picture, or null if it is still being generated.</returns>
		public static Texture2D GetThumbnail(TraderItem item, Action<Texture2D> onGenerated)
		{
			if (item.Prefab == null)
				return null;

			string key = CacheKey(item);

			if (_cache.TryGetValue(key, out Texture2D cached))
			{
				if (cached != null)
					return cached;

				_cache.Remove(key);
			}

			// Already queued, so just wait for the same render.
			if (_waiting.TryGetValue(key, out List<Action<Texture2D>> callbacks))
			{
				if (onGenerated != null)
					callbacks.Add(onGenerated);
				return null;
			}

			callbacks = new List<Action<Texture2D>>();
			if (onGenerated != null)
				callbacks.Add(onGenerated);
			_waiting[key] = callbacks;

			_queue.Enqueue(new Pending { Key = key, Item = item });
			StartProcessing();
			return null;
		}

		private static string CacheKey(TraderItem item)
		{
			string colour = item.Color.HasValue ? ColorUtility.ToHtmlStringRGB(item.Color.Value) : "none";
			string condition = item.Condition.HasValue ? item.Condition.Value.ToString() : "none";
			return $"{item.Prefab.name}|{condition}|{colour}";
		}

		private static void StartProcessing()
		{
			if (_processing)
				return;

			if (_runner == null)
			{
				GameObject runnerObject = new GameObject("NPCs Thumbnail Runner");
				UnityEngine.Object.DontDestroyOnLoad(runnerObject);
				_runner = runnerObject.AddComponent<Runner>();
			}

			_runner.StartCoroutine(ProcessQueue());
		}

		private static IEnumerator ProcessQueue()
		{
			_processing = true;
			try
			{
				while (_queue.Count > 0)
				{
					for (int i = 0; i < PerFrame && _queue.Count > 0; i++)
					{
						Pending next = _queue.Dequeue();
						Texture2D texture = null;
						try
						{
							texture = Render(next.Item);
						}
						catch (Exception ex)
						{
							Logging.LogError($"Thumbnail generation failed for {next.Item.Prefab?.name ?? "unknown"} - {ex}");
						}

						Complete(next.Key, texture);
					}

					yield return null;
				}
			}
			finally
			{
				_processing = false;
			}
		}

		private static void Complete(string key, Texture2D texture)
		{
			if (texture != null)
			{
				_cache[key] = texture;
				_cacheOrder.Enqueue(key);

				// Keep memory bounded. The oldest entries are the least likely to still be on screen.
				while (_cache.Count > MaxCached && _cacheOrder.Count > 0)
				{
					string oldest = _cacheOrder.Dequeue();
					if (_cache.TryGetValue(oldest, out Texture2D old))
					{
						_cache.Remove(oldest);
						UnityEngine.Object.Destroy(old);
					}
				}
			}

			if (!_waiting.TryGetValue(key, out List<Action<Texture2D>> callbacks))
				return;

			_waiting.Remove(key);
			foreach (Action<Texture2D> callback in callbacks)
			{
				try
				{
					callback(texture);
				}
				catch (Exception ex)
				{
					Logging.LogError($"Thumbnail callback failed - {ex}");
				}
			}
		}

		private static Texture2D Render(TraderItem item)
		{
			// Build the item well away from the world, on a layer only our camera can see.
			GameObject root = new GameObject("NPCS THUMBNAIL FOR " + item.Prefab.name.ToUpper());
			root.transform.position = new Vector3(
				UnityEngine.Random.Range(-200f, 200f),
				UnityEngine.Random.Range(-1000f, -200f),
				UnityEngine.Random.Range(-200f, 200f)
			);
			root.layer = RenderLayer;
			root.SetActive(false);

			GameObject instance = UnityEngine.Object.Instantiate(item.Prefab, root.transform, false);
			instance.transform.localPosition = Vector3.zero;
			instance.layer = RenderLayer;

			// Wake the item up the same way a real spawn would so it can be given its condition and colour.
			root.SetActive(true);

			try
			{
				// Make sure nothing keeps playing or moves while we render.
				foreach (AudioSource source in instance.GetComponentsInChildren<AudioSource>(true))
				{
					source.Stop();
					source.enabled = false;
				}

				foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
					body.isKinematic = true;

				// Stop any cameras the item brings with it from conflicting with ours.
				foreach (Camera existing in instance.GetComponentsInChildren<Camera>(true))
					existing.enabled = false;
			}
			catch (Exception ex)
			{
				Logging.LogDebug($"Thumbnail setup failed for {item.Prefab.name} - {ex.Message}");
			}

			// Apply the same condition and colour the item will have once it is bought.
			try
			{
				partconditionscript condition = instance.GetComponentInChildren<partconditionscript>(true);
				if (condition != null && item.Condition.HasValue)
					condition.StartRandom2(item.Color ?? Color.white, 0, 4, item.Condition.Value);
			}
			catch (Exception ex)
			{
				Logging.LogDebug($"Thumbnail condition failed for {item.Prefab.name} - {ex.Message}");
			}

			// Left doors and gauges face the wrong way round.
			string name = item.Prefab.name.ToLowerInvariant();
			if (name.Contains("ldoor") || item.Prefab.GetComponent<meterscript>() != null)
			{
				Vector3 angle = instance.transform.localEulerAngles;
				angle.y = 180f;
				instance.transform.localEulerAngles = angle;
			}

			Bounds? bounds = GetBounds(instance);

			Camera camera = new GameObject("CAMERA").AddComponent<Camera>();
			camera.transform.SetParent(root.transform, true);
			camera.gameObject.layer = RenderLayer;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = Color.clear;
			camera.fieldOfView = 30f;
			camera.aspect = 1f;
			camera.cullingMask = 1 << RenderLayer;

			Light light = camera.gameObject.AddComponent<Light>();
			light.type = LightType.Directional;
			light.cullingMask = 1 << RenderLayer;

			// Frame the bounds so every item fills the picture, whatever its size.
			Vector3 direction = new Vector3(1f, -0.3f, -1f).normalized;
			Vector3 lookTarget = bounds?.center ?? instance.transform.position;
			float extentMagnitude = Mathf.Max(bounds?.extents.magnitude ?? 1f, 0.1f);
			float distance = extentMagnitude / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

			camera.transform.position = lookTarget - direction * distance;
			camera.transform.LookAt(lookTarget);
			camera.nearClipPlane = 0.0001f;
			camera.farClipPlane = distance * 3f;

			RenderTexture renderTexture = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
			camera.forceIntoRenderTexture = true;
			camera.targetTexture = renderTexture;
			camera.Render();

			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = renderTexture;
			Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
			texture.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
			texture.Apply();
			RenderTexture.active = previous;

			// The item woke up like a real one, so make sure the save system doesn't keep it.
			foreach (tosaveitemscript save in instance.GetComponentsInChildren<tosaveitemscript>(true))
				save.removeFromMemory = true;

			root.SetActive(false);
			camera.targetTexture = null;
			UnityEngine.Object.Destroy(renderTexture);
			UnityEngine.Object.Destroy(root);

			return texture;
		}

		private static Bounds? GetBounds(GameObject instance)
		{
			Bounds? bounds = null;

			foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
			{
				try
				{
					// Layer 18 renderers are helper meshes, not part of the item's body.
					if (renderer.gameObject.layer == 18)
					{
						renderer.enabled = false;
						continue;
					}

					// Particle systems can report a bounds at world origin.
					if (renderer is ParticleSystemRenderer)
						continue;

					// Guard against any other renderer reporting a zero-size bounds.
					if (renderer.bounds.size == Vector3.zero)
						continue;

					// Reject any renderers that are implausibly far away.
					if (Vector3.Distance(renderer.bounds.center, instance.transform.position) > 500f)
						continue;

					renderer.gameObject.layer = RenderLayer;

					if (bounds == null)
					{
						bounds = renderer.bounds;
					}
					else
					{
						Bounds expanded = bounds.Value;
						expanded.Encapsulate(renderer.bounds);
						bounds = expanded;
					}
				}
				catch
				{
					// Skip anything that can't report its bounds.
				}
			}

			return bounds;
		}
	}
}