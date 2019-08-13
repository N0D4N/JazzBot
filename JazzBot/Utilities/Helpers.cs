using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using JazzBot.Enums;
using TagLib;
using JazzBot.Data;
using Npgsql;

namespace JazzBot.Utilities
{
	static class Helpers
	{
		public static string ToSize(this Int64 value, SizeUnits unit)
		{
			return (value / (double)Math.Pow(1024, (Int64)unit)).ToString("0.00");
		}

		/// <summary>
		/// Get pseudorandom number within <paramref name="maxValue"/> and <paramref name="maxValue"/> bounds.
		/// </summary>
		/// <param name="minValue">Lower bound</param>
		/// <param name="maxValue">Higher bound</param>
		/// <returns>Pseudorandom number within <paramref name="minValue"/> and <paramref name="maxValue"/></returns>
		public static int Cryptorandom(int minValue, int maxValue)
		{
			var provider = new RNGCryptoServiceProvider();
			var _uint32Buffer = new byte[4];
			if (minValue > maxValue)
				throw new ArgumentOutOfRangeException("minValue");
			if (minValue == maxValue) return minValue;
			Int64 diff = maxValue - minValue;
			while (true)
			{
				provider.GetBytes(_uint32Buffer);
				UInt32 rand = BitConverter.ToUInt32(_uint32Buffer, 0);

				Int64 max = (1 + (Int64)UInt32.MaxValue);
				Int64 remainder = max % diff;
				if (rand < max - remainder)
				{
					return (Int32)(minValue + (rand % diff));
				}
			}
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
				int k = Math.Abs(bit) % (int)n;
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return Task.CompletedTask;
		}

		
		/// <summary>
		/// Get extended color for <see cref="DiscordMember".
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
						var botrole = currentMember.Roles.OrderByDescending(xr => xr.Position).First(xr => xr.Color.Value != 0);
						if (botrole != null)
							return botrole.Color;
					}
				}
			}
			return RandomColor();			
		}		

		
		public static double OrderingFormula(int seed, int Songid)
		{
			double id = Math.Abs(Math.Sin(Songid * 1.0) + Math.Cos(Math.Sqrt(seed) * Songid));
			return Math.Abs(Math.Sin(id) + Math.Cos(Math.Sqrt(seed) * id));
		}

		public static bool IsCoverArtLinkPresent(this Tag tag)
		{
			return (!string.IsNullOrEmpty(tag.Comment) || !string.IsNullOrWhiteSpace(tag.Comment))
				&& (tag.Comment.StartsWith("https://media.discordapp.net/attachments/") || tag.Comment.StartsWith("https://cdn.discordapp.com/"));
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
				Host = Program.Cfgjson.Database.Hostname,
				Port = Program.Cfgjson.Database.Port,

				Username = Program.Cfgjson.Database.Username,
				Password = Program.Cfgjson.Database.Password,
				Database = Program.Cfgjson.Database.Database
			}.ConnectionString;
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
