using UnityEngine;

namespace NPCs.Utilities
{
	internal static class Components
	{
		public static void StripComponents(GameObject obj)
		{
			if (obj == null)
				return;
			foreach (var component in obj.GetComponents<Component>())
			{
				var assemblyName = component.GetType().Assembly.GetName().Name;
				if (!assemblyName.StartsWith("UnityEngine") &&
					!assemblyName.StartsWith("Assembly-CSharp") &&
					!assemblyName.StartsWith("M_NPCs"))
				{
					GameObject.Destroy(component);
				}
			}
		}
	}
}
