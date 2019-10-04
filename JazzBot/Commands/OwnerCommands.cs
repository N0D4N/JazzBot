﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Exceptions;
using JazzBot.Services;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using File = TagLib.File;

namespace JazzBot.Commands
{
	[Group("Owner")]
	[Description("Команды доступные только для владельца бота")]
	[Aliases("o")]
	[RequireOwner]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class OwnerCommands : BaseCommandModule
	{
		private Bot Bot { get; }

		private DatabaseContext Database { get; }

		public OwnerCommands(Bot bot, DatabaseContext db)
		{
			this.Bot = bot;
			this.Database = db;
		}

		[Command("FillPlaylist")]
		[Description("Заполняет определенный плейлист по имени")]
		[Aliases("fillp")]
		public async Task FillPlaylist(CommandContext context, [RemainingText, Description("Название плейлиста")]string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new DiscordUserInputException("Название плейлиста не может быть пустым или состоять из пробелов", nameof(name));
			var file = new FileInfo(this.Bot.PathToDirectoryWithPlaylists + "\\" + name + ".txt");
			if (!file.Exists)
			{
				throw new DiscordUserInputException($"Файла плейлиста {name}.txt не существует", nameof(name));
			}
			await this.UpdatePlaylistAsync(name).ConfigureAwait(false);
			await this.CreateExcelAsync(context.Client).ConfigureAwait(false);
			await context.RespondAsync($"Плейлист {name} успешно обновлен").ConfigureAwait(false);
		}

		[Command("FillAllPlaylists")]
		[Description("Перезаполняет все доступные плейлисты")]
		[Aliases("fillap")]
		public async Task FillAllPlaylists(CommandContext context)
		{
			foreach (var file in new DirectoryInfo(this.Bot.PathToDirectoryWithPlaylists).GetFiles().Select(x => Path.GetFileNameWithoutExtension(x.FullName)))
			{
				await this.UpdatePlaylistAsync(file).ConfigureAwait(false);
			}

			await this.CreateExcelAsync(context.Client);
			await context.RespondAsync("Все плейлисты успешно обновлены").ConfigureAwait(false);
		}

		[Command("Exit")]
		[Description("Выключает бота")]
		public async Task Restart(CommandContext context, [Description("Code to exit bot with. 0 is for exiting bot completely. Any other for restart.")] int exitCode)
		{
			await context.RespondAsync("Бот будет выключен через 1 секунду").ConfigureAwait(false);
			await context.Client.UpdateStatusAsync(new DiscordActivity("Выключается", ActivityType.Playing), UserStatus.DoNotDisturb).ConfigureAwait(false);
			await context.Client.DisconnectAsync();
			Environment.Exit(exitCode);
		}

		[Command("test")]
		public async Task Test(CommandContext context)
		{
			await context.RespondAsync($"новое обновление {DateTime.Now:dd.MM.yyyy}");
		}

		[Command("ExcelSheet")]
		[Description("Заполняет таблицу плейлистов")]
		[Aliases("excel")]
		public async Task ExcelSheet(CommandContext context)
		{
			await this.CreateExcelAsync(context.Client);
		}

		[Command("UpdatePresence")]
		[Description("Обновляет статус бота")]
		[Aliases("updpr")]
		[Cooldown(5, 60, CooldownBucketType.Global)]
		public async Task UpdatePresence(CommandContext context, [RemainingText, Description("Строчка которая будет отображаться в статусе")]string presenceText)
		{
			if (string.IsNullOrWhiteSpace(presenceText))
				throw new DiscordUserInputException("Строчка-статус не должна быть пустой или состоять только из пробелов.", nameof(presenceText));

			if (presenceText.Length > 128)
				throw new DiscordUserInputException("Длина строчки-статуса не должна превышать 128 символов.", nameof(presenceText));


			var bId = (long) context.Client.CurrentUser.Id;
			var config = await this.Database.Configs.SingleOrDefaultAsync(x => x.Id == bId);
			config.Presence = presenceText;
			this.Database.Configs.Update(config);
			if (await this.Database.SaveChangesAsync() <= 0)
			{
				throw new DatabaseException("Не удалось обновить \"presence\" бота в базе данных", DatabaseActionType.Update);
			}
			await context.Client.UpdateStatusAsync(new DiscordActivity(presenceText, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);
		}

		[Command("FixedReport")]
		[Description("Сообщает юзеру что ошибка зарепорченная им была исправлена")]
		[Aliases("fixed")]
		public async Task FixedReport(CommandContext context,
			[Description("Id сервера")] ulong guildId,
				[Description("Id канала")]ulong channelId,
					[RemainingText, Description("Id пользователя")] ulong userId)
		{
			if (!context.Client.Guilds.TryGetValue(guildId, out var guild))
				throw new DiscordUserInputException($"Не удалось найти сервер с таким id {guildId}, проверьте правильность ввода если необходимо", nameof(guildId));
			if (!guild.Members.TryGetValue(userId, out var user))
			{
				await context.RespondAsync("Пользователь покинул сервер (скорее всего о-О)").ConfigureAwait(false);
				return;
			}
			if (!guild.Channels.TryGetValue(channelId, out var channel))
				throw new DiscordUserInputException($"Не удалось найти канал с таким id {channelId}, проверьте правильность ввода если необходимо", nameof(channelId));

			await channel.SendMessageAsync($"{user.Mention}, ошибка о которой вы сообщили была исправлена, спасибо за сотрудничество").ConfigureAwait(false);
		}

		[Command("UpdateBot")]
		[Description("Изменить статус бота на сообщение о новом обновлении")]
		[Aliases("updbot")]
		public async Task UpdateBot(CommandContext context)
		{
			string updatePresence = $"J!update, новое обновление {DateTime.Now:dd.MM.yyyy}";
			await context.Client.UpdateStatusAsync(new DiscordActivity(updatePresence, ActivityType.ListeningTo), UserStatus.Online)
				.ConfigureAwait(false);
			var bId = (long) context.Client.CurrentUser.Id;
			var config = await this.Database.Configs.FirstOrDefaultAsync(x => x.Id == bId);
			config.Presence = updatePresence;
			this.Database.Configs.Update(config);
			if (await this.Database.SaveChangesAsync() <= 0)
				throw new DatabaseException("Не удалось обновить \"presence\" бота в базе данных", DatabaseActionType.Update);


		}

		[Command("Nickname")]
		[Description("Присваивает боту новый никнейм на данном сервере")]
		[Aliases("nick")]
		[RequireBotPermissions(Permissions.ChangeNickname)]
		public async Task Nickname(CommandContext context, [RemainingText, Description("Новый никнейм")]string nickname)
		{
			await context.Guild.CurrentMember.ModifyAsync(x => x.Nickname = nickname).ConfigureAwait(false);
		}

		private async Task UpdatePlaylistAsync(string playlistName)
		{
			var file = new FileInfo(this.Bot.PathToDirectoryWithPlaylists + "\\" + playlistName + ".txt");

			this.Database.Playlist.RemoveRange(this.Database.Playlist.Where(x => x.PlaylistName == playlistName));
			await this.Database.SaveChangesAsync();
			var songs = new List<Songs>();
			string text = "nonwhitespacetext";

			var sr = new StreamReader(file.FullName);
			for (int i = this.Database.Playlist.Count() + 1; !string.IsNullOrWhiteSpace(text); i++)
			{
				text = await sr.ReadLineAsync();
				if (text == null)
					break;
				var songFile = File.Create(text);
				songs.Add(new Songs { Name = songFile.Tag.Title, Path = text, PlaylistName = playlistName, SongId = i });
			}
			sr.Close();
			sr.DiscardBufferedData();
			sr.Dispose();

			await this.Database.Playlist.AddRangeAsync(songs);

			int count = await this.Database.SaveChangesAsync();
			if (count <= 0)
				throw new DatabaseException("Не удалось сохранить обновленный плейлист в БД", DatabaseActionType.Update);
		}

		private async Task CreateExcelAsync(DiscordClient client)
		{
			var overallPlDuration = TimeSpan.Zero;

			using (var package = new ExcelPackage())
			{
				client.DebugLogger.LogMessage(LogLevel.Info, client.CurrentUser.Username, "Обновление начато", DateTime.Now);

				ExcelWorksheet infoWorksheet = package.Workbook.Worksheets.Add("Info");

				// Filling infoworksheet.
				infoWorksheet.Cells[1, 1].Value = "Big Daddy's Playlist";
				infoWorksheet.Cells[1, 1, 1, 10].Merge = true;
				infoWorksheet.Cells[1, 1, 1, 10].Style.Font.Bold = true;

				infoWorksheet.Cells[2, 1].Value = "Количество песен";
				infoWorksheet.Cells[2, 2].Value = await this.Database.Playlist.CountAsync();

				infoWorksheet.Cells[3, 1].Value = "Дата последнего обновления";
				infoWorksheet.Cells[3, 2].Value = DateTime.Now.ToString("dddd, MMM dd yyyy", new CultureInfo("ru-RU"));

				infoWorksheet.Cells[4, 1].Value = "Длина всех плейлистов";
				infoWorksheet.Cells.AutoFitColumns(1, 40);

				// We will pass value here after we would know length of every playlist and get the sum.
				client.DebugLogger.LogMessage(LogLevel.Info, client.CurrentUser.Username, "Инфо-страница заполнена", DateTime.Now);

				// Filling info for each playlist in DB.
				foreach (var playlist in this.Database.Playlist.Select(x => x.PlaylistName).Distinct())
				{
					TimeSpan plDuration = TimeSpan.Zero;

					client.DebugLogger.LogMessage(LogLevel.Info, client.CurrentUser.Username, $"Начата запись в таблицу {playlist}", DateTime.Now);

					ExcelWorksheet playlistWorksheet = package.Workbook.Worksheets.Add(playlist);
					playlistWorksheet.Cells[2, 1].Value = "№";
					playlistWorksheet.Cells[2, 2].Value = "Title";
					playlistWorksheet.Cells[2, 3].Value = "Artist";
					playlistWorksheet.Cells[2, 4].Value = "Album";
					playlistWorksheet.Cells[2, 5].Value = "Date";
					playlistWorksheet.Cells[2, 6].Value = "Requested by";
					foreach (var cell in playlistWorksheet.Cells[2, 1, 2, 6])
					{
						cell.Style.Border.BorderAround(ExcelBorderStyle.Thick, System.Drawing.Color.Black);
						cell.Style.Font.Size = 24;
					}

					string[] songs = await this.Database.Playlist.Where(x => x.PlaylistName == playlist).Select(x => x.Path).ToArrayAsync();
					var songsFiles = new List<File>();
					foreach (var song in songs)
					{
						songsFiles.Add(File.Create(song));
					}
					songsFiles = songsFiles.OrderBy(x => x.Tag.FirstPerformer + x.Tag.Year.ToString() + x.Tag.Album + x.Tag.Track).ToList();
					int songNum = 1, currentRow = 3;
					foreach (var song in songsFiles)
					{
						playlistWorksheet.Cells[currentRow, 1].Value = songNum;
						playlistWorksheet.Cells[currentRow, 2].Value = $"{song.Tag.Track}.{song.Tag.Title}";
						playlistWorksheet.Cells[currentRow, 3].Value = song.Tag.FirstPerformer;
						playlistWorksheet.Cells[currentRow, 4].Value = song.Tag.Album;
						playlistWorksheet.Cells[currentRow, 5].Value = song.Tag.Year;
						if (ulong.TryParse(song.Tag.FirstComposer, out ulong addedById))
						{
							var user = await client.GetUserAsync(addedById).ConfigureAwait(false);
							playlistWorksheet.Cells[currentRow, 6].Value = $"@{user.Username}";
						}
						else
							playlistWorksheet.Cells[currentRow, 6].Value = "~~~~~~~~~~~~~";
						songNum++;
						currentRow++;
						plDuration = plDuration.Add(song.Properties.Duration);
					}
					playlistWorksheet.Cells[3, 1, currentRow - 1, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);
					playlistWorksheet.Cells[3, 1, currentRow, 6].Style.Font.Size = 10;
					playlistWorksheet.Cells.AutoFitColumns(0.5, 80.0);
					playlistWorksheet.Cells[1, 1].Value = $"Длительность плейлиста: {plDuration:dd\\.hh\\:mm\\:ss}";
					overallPlDuration = overallPlDuration.Add(plDuration);
					client.DebugLogger.LogMessage(LogLevel.Info, client.CurrentUser.Username, $"Закончена запись в таблицу {playlist}", DateTime.Now);
				}
				infoWorksheet.Cells[4, 2].Value = overallPlDuration.ToString(@"dd\.hh\:mm\:ss");
				client.DebugLogger.LogMessage(LogLevel.Info, client.CurrentUser.Username, "Запись закончена", DateTime.Now);
				package.SaveAs(new FileInfo($@"..\..\..\Playlists\{DateTime.Today:d}.xlsx"));

			}
		}
	}
}