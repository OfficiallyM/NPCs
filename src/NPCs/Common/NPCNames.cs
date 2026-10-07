using NPCs.Enums;

namespace NPCs.Common
{
	public static class NPCNames
	{
		private static readonly string[] _male =
		{
			"Aaron", "Adam", "Alan", "Andy", "Barry", "Ben", "Bernie", "Billy",
			"Bob", "Brian", "Chris", "Cliff", "Clive", "Colin", "Connor", "Dan",
			"Danny", "Dave", "Dean", "Dennis", "Derek", "Doug", "Earl", "Ed", "Eric",
			"Ethan", "Finn", "Frank", "Fred", "Gary", "Gordon", "Graham", "Greg",
			"Harry", "Jack", "Jake", "James", "Jeff", "Jim", "Joe", "Josh", "Keith",
			"Ken", "Kyle", "Lenny", "Les", "Liam", "Lou", "Luke", "Malcolm", "Marty",
			"Matt", "Mike", "Nigel", "Oliver", "Owen", "Pat", "Pete", "Phil", "Ralph",
			"Ray", "Rick", "Rob", "Ron", "Roy", "Sam", "Sean", "Steve", "Ted", "Terry",
			"Tom", "Tony", "Trevor", "Vic", "Walt", "Wes", "Wilf", "Will", "Zach",
		};

		private static readonly string[] _female =
		{
			"Abby", "Alice", "Amy", "Angie", "Annie", "Barbara", "Beth", "Bev",
			"Carol", "Cathy", "Charlotte", "Cheryl", "Chloe", "Claire", "Dawn", "Debbie",
			"Dee", "Diane", "Donna", "Edith", "Ella", "Ellie", "Emma", "Erin",
			"Eva", "Fiona", "Gail", "Gwen", "Hannah", "Hazel", "Heather", "Holly",
			"Ivy", "Jan", "Jane", "Janet", "Jean", "Jenny", "Jess", "Joan",
			"Julie", "June", "Karen", "Kate", "Kay", "Kim", "Laura", "Lily",
			"Linda", "Lisa", "Lois", "Lucy", "Lynn", "Maggie", "Maria", "Martha",
			"Mary", "Maud", "Meg", "Molly", "Nancy", "Nell", "Nora", "Olive",
			"Pam", "Pat", "Peggy", "Penny", "Polly", "Rachel", "Rita", "Rose",
			"Ruth", "Sally", "Sam", "Sarah", "Sue", "Tess", "Tina", "Val", "Vera", "Zoe",
		};

		public static string[] For(Gender gender) => gender == Gender.Female ? _female : _male;
	}
}
