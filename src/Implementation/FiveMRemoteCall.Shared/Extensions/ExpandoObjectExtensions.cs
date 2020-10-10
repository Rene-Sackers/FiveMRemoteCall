using System;
using System.Dynamic;
using System.Linq;

namespace FiveMRemoteCall.Shared.Extensions
{
	internal static class ExpandoObjectExtensions
	{
		public static object Cast(this ExpandoObject expandoObject, Type targetType)
		{
			var parameterKeyValues = expandoObject?.ToList();
			var constructor = targetType
				.GetConstructors()
				.Select(c => new { Constructor = c, Parameters = c.GetParameters() })
				.OrderByDescending(c => c.Parameters.Length)
				.First();

			var parameterValues = new object[constructor.Parameters.Length];
			for (var i = 0; i < constructor.Parameters.Length; i++)
				parameterValues[i] = parameterKeyValues[i].Value;

			return Activator.CreateInstance(targetType, parameterValues);
		}
	}
}
