namespace IslandHopper {
	public static class Debug {
		public static string state = "";
		public static bool printing = true;
		public static void Print(string state) {
			Debug.state = state;
			if(printing)
				System.Console.WriteLine(state);
		}
		public static void Print(bool condition, string state) {
			if (condition)
				Print(state);
		}
	}
}

