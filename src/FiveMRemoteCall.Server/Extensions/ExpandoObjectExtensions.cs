using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace FiveMRemoteCall.Server.Extensions
{
	internal static class ExpandoObjectExtensions
	{
		public static object Cast(this ExpandoObject expandoObject, Type targetType)
		{
			var instance = Activator.CreateInstance(targetType);
			var values = expandoObject.ToDictionary(kv => kv.Key, kv => kv.Value);
			foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				property.SetValue(instance, values[property.Name]);

			return instance;
		}
	}
}
