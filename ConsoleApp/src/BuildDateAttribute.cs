using System;
using System.Globalization;
using System.Reflection;

namespace ConsoleApp {

	// https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm

	[AttributeUsage(AttributeTargets.Assembly)]
	public class BuildDateAttribute : Attribute {

		private DateTime DateTime {get;}

		public BuildDateAttribute(string value) {
			DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
		}

		public static DateTime? GetBuildDate(Assembly assembly) {
			return assembly.GetCustomAttribute<BuildDateAttribute>()?.DateTime;
		}
	}
}
