using System;

namespace Sabresaurus.Sidekick
{
	public abstract class BasePane
	{
		protected static bool SearchMatches(string searchTerm, string candidateName)
		{
			if (string.IsNullOrEmpty(searchTerm)) return true;

			string[] split = searchTerm.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);

			foreach (string searchTermPart in split)
			{
				if (!candidateName.Contains(searchTermPart, StringComparison.InvariantCultureIgnoreCase))
					return false;
			}
			return true;
		}
	}
}