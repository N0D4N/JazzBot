using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace JazzBot.Data.Music
{
	public interface IMusicSource
	{
		bool IsPresent();

		Task<DiscordEmbed> GetCurrentSongEmbed();

		Uri GetCurrentSong();
	}
}