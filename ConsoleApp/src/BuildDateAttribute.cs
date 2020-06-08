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


		internal static DateTime GetBuildDate(Assembly assembly) {
			var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
			return attribute?.DateTime ?? default;
		}
	}
}