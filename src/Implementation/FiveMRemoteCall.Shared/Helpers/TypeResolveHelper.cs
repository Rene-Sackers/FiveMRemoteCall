using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using FiveMRemoteCall.Shared.Extensions;

namespace FiveMRemoteCall.Shared.Helpers
{
	internal static class TypeResolveHelper
	{
		private static readonly Type StringType = typeof(string);

		public static object[] ResolveMethodParameterTypes(ICollection<object> values, MethodInfo targetMethod, string playerHandle = null)
		{
			if (values?.Any() != true)
				return new object[0];

			var targetMethodParameters = targetMethod.GetParameters();
			if (values.Count != targetMethodParameters.Length)
			{
				LogHelper.Log($"Mismatched parameter count for method {targetMethod.DeclaringType.FullName}.{targetMethod.Name}. Expected: {targetMethodParameters.Length}, got: {values.Count}");
				return null;
			}
			
			var resolvedParameters = new object[values.Count];
			for (var i = 0; i < values.Count; i++)
			{
				var targetMethodParameter = targetMethodParameters[i];

				if (i == 0 && targetMethodParameter.Name == nameof(playerHandle) && targetMethodParameter.ParameterType == StringType)
					resolvedParameters[i] = playerHandle;
				else
					resolvedParameters[i] = ResolveType(values.ElementAt(i), targetMethodParameter.ParameterType);
			}

			return resolvedParameters;
		}

		public static object ResolveType(object parameter, Type targetType)
		{
			return parameter is ExpandoObject expandoObject ? expandoObject.Cast(targetType) : parameter;
		}
	}
}
