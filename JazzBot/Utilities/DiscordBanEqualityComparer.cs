using System.Collections.Generic;
using DSharpPlus.Entities;

namespace JazzBot.Utilities
{
	class DiscordBanEqualityComparer : IEqualityComparer<DiscordBan>
	{
		public bool Equals(DiscordBan x, DiscordBan y)
			=> x.User.Id == y.User.Id;

		public int GetHashCode(DiscordBan obj)
			=> obj.Reason.GetHashCode() ^ obj.User.GetHashCode();
	}
}
