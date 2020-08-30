using NUnit.Framework;
using System.Runtime.CompilerServices;

namespace Tests {
	class RegressionTests {
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
