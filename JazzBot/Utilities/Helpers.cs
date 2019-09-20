using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using JazzBot.Data;
using JazzBot.Enums;
using Npgsql;
using Tag = TagLib.Tag;

namespace JazzBot.Utilities
{
	static class Helpers
	{
		public static string ToSize(this long value, SizeUnits unit)
		{
			return (value / Math.Pow(1024, (long) unit)).ToString("0.00");
		}

		/// <summary>
		/// Get pseudorandom number within <paramref name="maxValue"/> and <paramref name="maxValue"/> bounds.
		/// </summary>
		/// <param name="minValue">Lower bound</param>
		/// <param name="maxValue">Higher bound</param>
		/// <returns>Pseudorandom number within <paramref name="minValue"/> and <paramref name="maxValue"/></returns>
		public static int CryptoRandom(int minValue, int maxValue)
		{
			if (minValue > maxValue)
				throw new ArgumentOutOfRangeException(nameof(minValue));
			if (minValue == maxValue)
				return minValue;
			var provider = new RNGCryptoServiceProvider();

			int diff = maxValue - minValue;
			int remainder, rand, result;
			var buffer = new byte[4];

			do
			{
				provider.GetBytes(buffer);
				rand = Math.Abs(BitConverter.ToInt32(buffer, 0));
				remainder = rand % diff;
				result = remainder + minValue;
			} while (result > maxValue || result < minValue);

			return result;

		}

		/// <summary>
		/// Shuffles the element order of the specified list.
		/// </summary>
		public static Task Shuffle<T>(this IList<T> list)
		{
			var provider = new RNGCryptoServiceProvider();
			int n = list.Count;
			while (n > 1)
			{
				byte[] box = new byte[sizeof(int)];
				provider.GetBytes(box);
				int bit = BitConverter.ToInt32(box, 0);
				int k = Math.Abs(bit) % n;
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return Task.CompletedTask;
		}


		/// <summary>
		/// Get extended color for <see cref="DiscordMember"></see>.
		/// </summary>
		/// <param name="member">Member from which color gets</param>
		/// <param name="currentMember">Current member of the guild</param>
		/// <returns>Highest color of <paramref name="member"/> if none, highest color of current member, if none - <see cref="DiscordColor.Black"/></returns>
		public static DiscordColor ExtendedColor(DiscordMember member, DiscordMember currentMember)
		{
			if (member?.Roles?.Any() == true)
			{
				var role = member.Roles.OrderByDescending(xr => xr.Position).First(xr => xr.Color.Value != 0);
				if (role != null)
					return role.Color;
				else
				{
					if (currentMember?.Roles?.Any() == true)
					{
						var botRole = currentMember.Roles.OrderByDescending(xr => xr.Position).First(xr => xr.Color.Value != 0);
						if (botRole != null)
							return botRole.Color;
					}
				}
			}
			return RandomColor();
		}


		public static double OrderingFormula(int seed, int songId)
		{
			double id = Math.Abs(Math.Sin(songId * 1.0) + Math.Cos(Math.Sqrt(seed) * songId));
			return Math.Abs(Math.Sin(id) + Math.Cos(Math.Sqrt(seed) * id));
		}

		public static bool IsCoverArtLinkPresent(this Tag tag)
		{
			var comment = tag.Comment;
			return (!string.IsNullOrEmpty(comment) || !string.IsNullOrWhiteSpace(comment))
				&& (comment.StartsWith("https://media.discordapp.net/attachments/") || comment.StartsWith("https://cdn.discordapp.com/"));
		}

		public static DiscordColor RandomColor()
		{
			var provider = new RNGCryptoServiceProvider();
			byte[] rgb = new byte[3];
			provider.GetNonZeroBytes(rgb);
			return new DiscordColor(rgb[0], rgb[1], rgb[2]);
		}

		public static string NpgSqlConnectionString(this JazzBotConfigDatabase config)
		{
			return new NpgsqlConnectionStringBuilder
			{
				Host = Program.CfgJson.Database.Hostname,
				Port = Program.CfgJson.Database.Port,

				Username = Program.CfgJson.Database.Username,
				Password = Program.CfgJson.Database.Password,
				Database = Program.CfgJson.Database.Database
			}.ConnectionString;
		}

		public static Page[] GeneratePagesInEmbed(IEnumerable<string> input, DiscordEmbedBuilder embedBase = null, int charsOnPage = 500)
		{
			var embed = embedBase ?? new DiscordEmbedBuilder();

			var result = new List<Page>();
			var split = new List<string>();

			var tempString = new StringBuilder();

			int currentElement = 0;
			while(currentElement < input.Count())
			{
				var currentString = input.ElementAt(currentElement);
				if(tempString.Length + currentString.Length < charsOnPage)
				{
					tempString.Append(currentString);
					currentElement++;
				}
				else
				{
					split.Add(tempString.ToString());
					tempString.Clear();
				}
			}
			int page = 1;
			foreach(var el in split)
			{
				result.Add(new Page("", new DiscordEmbedBuilder(embed).WithDescription(el).WithFooter($"Страница {page}/{split.Count}")));
				page++;
			}
			return result.ToArray();
		}

		public static TimeSpan GetProcessUptime(Process process)
		{
			return DateTime.Now - process.StartTime;
		}

		public static string ToReadableString(this TimeSpan span)
		{
			return string.Format("{0}{1}:{2}",
				span.Days > 0 ? span.Days.ToString() + "д. " : string.Empty,
				span.Hours + "ч",
				span.Minutes + "м");
		}

		#region unused_and_old_code_i_dont_want_to_delete
		//public static bool IsFileLocked(FileInfo file)
		//{
		//	FileStream stream = null;
		//	try
		//	{
		//		stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
		//	}
		//	catch (IOException)
		//	{
		//		//the file is unavailable because it is:
		//		//still being written to
		//		//or being processed by another thread
		//		//or does not exist (has already been processed)
		//		return true;
		//	}
		//	finally
		//	{
		//		if (stream != null)
		//			stream.Close();
		//	}
		//	//file is not locked
		//	return false;
		//}

		//public static async Task<DiscordChannel> GetDiscordChannel(string channelMentionstring, CommandContext context)
		//{
		//	string channel = "";
		//	if (channelMentionstring.StartsWith('<'))
		//	{
		//		channel = channelMentionstring.Remove(0, 2);
		//		channel = channel.Remove(channel.Length - 1);
		//		if (ulong.TryParse(channel, out ulong channelid))
		//		{
		//			return await context.Client.GetChannelAsync(channelid).ConfigureAwait(false);
		//		}
		//		throw new ArgumentException($"channelMentionString [{channelMentionstring}] не выглядит как <#id>");
		//	}
		//	else if (channelMentionstring.StartsWith('#'))
		//	{
		//		channel = channelMentionstring.Remove(0, 1);
		//		foreach (var chn in context.Guild.Channels.Values)
		//		{
		//			if (chn.Name == channelMentionstring)
		//			{
		//				return chn;
		//			}
		//		}
		//		throw new ArgumentException($"На данном сервере не существует канала с таким названием - [{channelMentionstring}]");
		//	}
		//	else
		//	{
		//		throw new ArgumentException($"На данном сервере не существует канала с таким названием - [{channelMentionstring}] и он не выглядит как <#id>");
		//	}
		//}
		#endregion
	}



}
