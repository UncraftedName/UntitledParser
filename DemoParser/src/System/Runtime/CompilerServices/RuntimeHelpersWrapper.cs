namespace System.Runtime.CompilerServices {
	/* a way in for external code, otherwise this isn't testable -
	 * gotta love cross-assembly namespace hackery */
	public static class RuntimeHelpersWrapper {
		public static int GetHashCode(object o) {
			return RuntimeHelpers.GetHashCode(o);
		}
	}
}
