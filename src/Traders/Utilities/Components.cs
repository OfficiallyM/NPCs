using UnityEngine;

namespace Traders.Utilities
{
	internal static class Components
	{
		public static void StripComponents(GameObject obj)
		{
			foreach (var component in obj.GetComponents<Component>())
			{
				var assemblyName = component.GetType().Assembly.GetName().Name;
				if (!assemblyName.StartsWith("UnityEngine") &&
					!assemblyName.StartsWith("Assembly-CSharp") &&
					!assemblyName.StartsWith("M_Traders"))
				{
					GameObject.Destroy(component);
				}
			}
		}
	}
}
