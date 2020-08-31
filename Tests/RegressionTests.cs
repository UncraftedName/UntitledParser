using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Tests {
	
	public class RegressionTests {
		
		[Test]
		public void TestStupidReflectionHack() {
			object o = new int[1];
			object o2 = new int[1];
			Assert.AreNotEqual(RuntimeHelpersWrapper.GetHashCode(o), 0);
			Assert.AreNotEqual(RuntimeHelpersWrapper.GetHashCode(o2), 0);
			Assert.AreNotEqual(RuntimeHelpersWrapper.GetHashCode(o), RuntimeHelpersWrapper.GetHashCode(o2));
		}
	}
}
