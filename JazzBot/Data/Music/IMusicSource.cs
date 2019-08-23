using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace JazzBot.Data.Music
{
	public interface IMusicSource
	{
		/// <summary>
		/// Checks if songs are present in this music source
		/// </summary>
		bool IsPresent();

		/// <summary>
		/// Get <see cref="DiscordEmbed"/> representing info about currently playing song
		/// </summary>
		Task<DiscordEmbed> GetCurrentSongEmbed();

		/// <summary>
		/// Get <see cref="Uri"/> for song to play
		/// </summary>
		Task<Uri> GetCurrentSong();

		/// <summary>
		/// Clears queue of songs to play
		/// </summary>
		void ClearQueue();
	}
}