using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace JazzBot.Data
{
	public sealed class JazzBotConfig
	{
		/// <summary>
		/// Discord configuration.
		/// </summary>
		[JsonProperty("Discord")]
		public JazzBotConfigDiscord Discord { get; private set; }

		/// <summary>
		/// Database configuration.
		/// </summary>
		[JsonProperty("Database")]
		public JazzBotConfigDatabase Database { get; private set; }

		/// <summary>
		/// Lavalink configuration.
		/// </summary>
		[JsonProperty("Lavalink")]
		public JazzBotConfigLavalink Lavalink { get; private set; }

		/// <summary>
		/// Youtube configuration
		/// </summary>
		[JsonProperty("Youtube")]
		public JazzBotConfigYoutube Youtube { get; private set; }

		/// <summary>
		/// Miscellaneous configuration.
		/// </summary>
		[JsonProperty("Miscellaneous")]
		public JazzBotConfigMiscellaneous Miscellaneous { get; private set; }
	}

	public sealed class JazzBotConfigDiscord
	{
		/// <summary>
		/// Bot token for Discord Api.
		/// </summary>
		[JsonProperty("Token")]
		public string Token { get; private set; }

		/// <summary>
		/// Discord bot prefixes.
		/// </summary>
		[JsonProperty("Prefixes")]
		public string[] Prefixes { get; private set; }

		/// <summary>
		/// ID of <see cref="DiscordChannel"/> where cover arts of song albums shoul be sent.
		/// </summary>
		[JsonProperty("CoverArtsChannelID")]
		public ulong CoverArtsChannelID { get; private set; }

		/// <summary>
		/// ID of <see cref="DiscordChannel"/> where all important errors should be sent.
		/// </summary>
		[JsonProperty("ErrorChannelID")]
		public ulong ErrorChannelID { get; private set; }
	
		/// <summary>
		/// ID of <see cref="DiscordChannel"/> where all user reports should be sent.
		/// </summary>
		[JsonProperty("ReportChannelID")]
		public ulong ReportChannelID { get; private set; }

		/// <summary>
		/// ID of <see cref="DiscordChannel"/> where all user requests should be sent.
		/// </summary>
		[JsonProperty("RequestChannelID")]
		public ulong RequestChannelID { get; private set; }

	}

	/// <summary>
	/// Config for connection to PostgreSQL server
	/// </summary>
	public sealed class JazzBotConfigDatabase
	{

		/// <summary>
		/// Hostname of server
		/// </summary>
		[JsonProperty("Hostname")]
		public string Hostname { get; private set; }

		/// <summary>
		/// Port of server
		/// </summary>
		[JsonProperty("Port")]
		public int Port { get; private set; }

		/// <summary>
		/// Name of default database
		/// </summary>
		[JsonProperty("Database")]
		public string Database { get; private set; }

		/// <summary>
		/// User's username
		/// </summary>
		[JsonProperty("Username")]
		public string Username { get; private set; }

		/// <summary>
		/// User's password
		/// </summary>
		[JsonProperty("Password")]
		public string Password { get; private set; }
	}
	
	public sealed class JazzBotConfigLavalink
	{
		/// <summary>
		/// Password of the Lavalink Server.
		/// </summary>
		[JsonProperty("Password")]
		public string Password { get; private set; }

		/// <summary>
		/// Hostname of the Lavalink Server.
		/// </summary>
		[JsonProperty("Hostname")]
		public string Hostname { get; private set; }

		/// <summary>
		/// Port of the Lavalink Server.
		/// </summary>
		[JsonProperty("Port")]
		public int Port { get; private set; }
	}

	/// <summary>
	/// Config for interactingwith youtube
	/// </summary>
	public sealed class JazzBotConfigYoutube
	{
		/// <summary>
		/// Youtube Api key
		/// </summary>
		[JsonProperty("ApiKey")]
		public string ApiKey { get; private set; }
	}

	public sealed class JazzBotConfigMiscellaneous
	{
		/// <summary>
		/// Path to directory where .txt with playlists are saved.
		/// </summary>
		[JsonProperty("PathToDirectoryWithPlaylists")]
		public string PathToDirectoryWithPlaylists { get; private set; }

		/// <summary>
		/// Link to document with playlist.
		/// </summary>
		[JsonProperty("PlaylistLink")]
		public string PlaylistLink { get; private set; }

		/// <summary>
		/// Link to document with update notes.
		/// </summary>
		[JsonProperty("UpdateLink")]
		public string UpdateLink { get; private set; }
	}

}
