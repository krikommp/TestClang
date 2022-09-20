﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp3.TemplateEngine
{
    public static class EvaluatorExtension
    {
		public static string GetFriendlyName(this Type type)
		{
			string friendlyName = type.Name;
			if (type.IsGenericType)
			{
				int iBacktick = friendlyName.IndexOf('`');
				if (iBacktick > 0)
				{
					friendlyName = friendlyName.Remove(iBacktick);
				}

				friendlyName += "<";
				Type[] typeParameters = type.GetGenericArguments();
				for (int i = 0; i < typeParameters.Length; ++i)
				{
					string typeParamName = GetFriendlyName(typeParameters[i]);
					friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
				}

				friendlyName += ">";
			}

			return friendlyName;
		}
	}
}
