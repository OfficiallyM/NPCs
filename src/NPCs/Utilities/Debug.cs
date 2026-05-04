using UnityEngine;

namespace NPCs.Utilities
{
	internal static class Debug
	{
		public static void FindTextureByName(GameObject root, string partialName)
		{
			foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
			{
				foreach (var mat in renderer.sharedMaterials)
				{
					if (mat == null) continue;
					foreach (var texName in mat.GetTexturePropertyNames())
					{
						var tex = mat.GetTexture(texName);
						if (tex != null && tex.name.Contains(partialName))
							Logging.LogDebug($"{renderer.gameObject.name} | {mat.name} | {texName} | {tex.name}");
					}
				}
			}
		}
	}
}
