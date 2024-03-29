﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using F23.StringSimilarity;
using JazzBot.Attributes;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Exceptions;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace JazzBot.Commands
{
	[Group("Tag")]
	[Description("Команды тегов")]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class TagCommands : BaseCommandModule
	{
		private static readonly string[] ForbiddenNames = { "create", "make", "delete", "remove", "force_delete", "force_remove", "edit", "modify", "force_edit", "force_modify", "info", "list", "@everyone", "@here", "transfer", "give", "claim", "userstats", "userstat", "serverstats", "serverstat" };

		private DatabaseContext Database { get; }

		public TagCommands(DatabaseContext db)
		{
			this.Database = db;
		}

		[Command("Create")]
		[Description("Создает тег с заданным названием и контентом")]
		[Aliases("make")]
		[Cooldown(2, 180, CooldownBucketType.User)]
		public async Task CreateAsync(CommandContext context,
			[Description("Название тега")] string name,
			[RemainingText, Description("Содержимое тега")] string contents)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или иметь такое же название как команды.", nameof(name));

			if (string.IsNullOrWhiteSpace(contents))
				throw new DiscordUserInputException("Содержимое тега не может быть пустым или содержать только пробелы.", nameof(contents));

			if (contents.Length > 2000)
				throw new DiscordUserInputException("Длина содержимого тега не может превышать 2000 символов.", nameof(contents));

			name = name.ToLowerInvariant();

			var tag = new Tag
			{
				Id = Convert.ToInt64(DateTimeOffset.Now.ToUnixTimeMilliseconds()),
				Name = name,
				TagContent = contents,
				GuildId = (long) context.Guild.Id,
				OwnerId = (long) context.User.Id,
				CreationDate = DateTime.Now,
				TimesUsed = 0
			};

			if (this.Database.Tags?.Any(t => t.Name == tag.Name && t.GuildId == tag.GuildId) == true)
			{
				throw new DiscordUserInputException("Тег с таким именем существует на данном сервере.", nameof(name));
			}

			await this.Database.Tags.AddAsync(tag).ConfigureAwait(false);
			int rowsAffected = await this.Database.SaveChangesAsync();
			if (rowsAffected > 0)
			{
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Тег успешно создан")).ConfigureAwait(false);
			}
			else
			{
				throw new DatabaseException($"Не удалось создать тег {name}. Попробуйте снова.", DatabaseActionType.Add);
			}
		}

		[Command("Delete")]
		[Description("Удаляет выбранный тег")]
		[Aliases("remove")]
		public async Task DeleteTag(CommandContext context,
			[RemainingText, Description("Название тега который нужно удалить")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться так же как и команды.", nameof(name));

			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				this.Database.Tags.Remove(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно удален")).ConfigureAwait(false);

				}
				else
				{
					throw new DatabaseException(
						$"Не удалось удалить тег {name}. Убедитесь в том что тег существует, в правильности написания названия и в том что вы являетесь владельцем тега.",
						DatabaseActionType.Remove);
				}
			}
		}

		[Command("force_delete")]
		[Description("Удаляет тег. Создано для модераторов/администраторов")]
		[Aliases("force_remove")]
		[OwnerOrPermission(Permissions.ManageGuild)]
		public async Task ForceDeleteTag(CommandContext context,
			[RemainingText, Description("Название тега который нужно удалить")]string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));

			name = name.ToLowerInvariant();
			var gId = (long) context.Guild.Id;
			Tag tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
			{
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия названия тега.", nameof(name));
			}
			else
			{
				this.Database.Tags.Remove(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно удален")).ConfigureAwait(false);
				}
				else
					throw new DatabaseException($"Не удалось удалить тег {name}. Убедитесь в том что тег существует и правильности написания названия.", DatabaseActionType.Remove);
			}

		}

		[Command("Edit")]
		[Description("Обновляет содержимое тега")]
		[Aliases("Modify")]
		[Cooldown(2, 180, CooldownBucketType.User)]
		public async Task EditTag(CommandContext context,
			[Description("Название тега")]string name,
			[RemainingText, Description("Новое содержимое тега")]string newContent)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));


			if (string.IsNullOrWhiteSpace(newContent))
				throw new DiscordUserInputException("Содержимое тега не может быть пустым или содержать одни пробелы.", nameof(newContent));

			if (newContent.Length > 2000)
				throw new DiscordUserInputException("Длина содержимого тега не должна превышать.", nameof(newContent));


			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			Tag tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				tag.TagContent = newContent;
				this.Database.Tags.Update(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно изменен")).ConfigureAwait(false);

				}
				else
					throw new DatabaseException($"Не удалось изменить тег {name}", DatabaseActionType.Update);
			}

		}

		[Command("Force_edit")]
		[Description("Обновляет содержимое тега")]
		[Aliases("Force_modify")]
		[OwnerOrPermission(Permissions.ManageGuild)]
		public async Task ForceEditTag(CommandContext context,
			[Description("Название тега")]string name,
			[RemainingText, Description("Новое содержимое тега")]string newContent)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));

			if (string.IsNullOrWhiteSpace(newContent))
				throw new DiscordUserInputException("Контент тега не может быть пустым или содержать одни пробелы.", nameof(newContent));

			if (newContent.Length > 2000)
				throw new DiscordUserInputException("Контент тега не может содержать больше 2000 символов.", nameof(newContent));


			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;

			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия.", nameof(name));
			else
			{
				tag.TagContent = newContent;
				this.Database.Tags.Update(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle("Тег успешно изменен")).ConfigureAwait(false);
				}
				else
				{
					throw new DatabaseException($"Не удалось изменить тег {name}", DatabaseActionType.Update);
				}
			}
		}

		[Command("List")]
		[Description("Показывает список тегов на этом сервере")]
		[Cooldown(2, 600, CooldownBucketType.Channel)]
		[RequireBotPermissions(Permissions.AddReactions)]
		public async Task List(CommandContext context)
		{

			var gId = (long) context.Guild.Id;

			var tagsList = this.Database.Tags.Where(t => t.GuildId == gId).ToList();
			if (tagsList?.Any() == true)
			{
				var tagNames = tagsList.OrderBy(x=> x.Name).Select(x => $"{Formatter.InlineCode(x.Name)},").ToList();
				// Deleting last comma
				tagNames[tagNames.Count - 1] = tagNames[tagNames.Count - 1].Remove(tagNames[tagNames.Count - 1].Length - 1);

				int length = 0;
				foreach(var tagName in tagNames)
					length += tagName.Length;

				var embedRespond = EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Теги на этом сервере");
	
				if (length > 600)
				{
					var interactivity = context.Client.GetInteractivity();
					var tagsPaginated = Helpers.GeneratePagesInEmbed(tagNames, embedRespond, 500);
					await interactivity.SendPaginatedMessageAsync
						(context.Channel, context.User, tagsPaginated, behaviour: PaginationBehaviour.WrapAround, deletion: PaginationDeletion.DeleteEmojis);

				}
				else
				{
					string tags = string.Join(' ', tagNames);
					await context.RespondAsync(embed: embedRespond.WithDescription(tags)).ConfigureAwait(false);
				}
			}
			else
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle($"Тегов на данном сервере не найдено, вы можете создать новый используя {Formatter.InlineCode("J!tag create \"название тега\" \"его содержание\"")}"))
					.ConfigureAwait(false);
		}

		[Command("Info")]
		[Description("Показывает информацию про данный тег")]
		[Aliases("information")]
		public async Task Info(CommandContext context, [RemainingText, Description("Название тега")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));
			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;

			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
			{
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия.", nameof(name));
			}
			else
			{
				var ownId = (ulong) tag.OwnerId;
				DiscordUser tagAuthor = await context.Client.GetUserAsync(ownId).ConfigureAwait(false);
				await context.RespondAsync(embed: new DiscordEmbedBuilder
				{
					Title = $"Информация про тег {tag.Name}",
					Description = tag.TagContent,
					Timestamp = tag.CreationDate,
					Color = Helpers.ExtendedColor(context.Member, context.Guild.CurrentMember),
				}
				.WithAuthor($"Автор тега - {tagAuthor.Username}#{tagAuthor.Discriminator}", iconUrl: tagAuthor.AvatarUrl)
				.WithFooter("Дата создания")
				.AddField("Количество использований", tag.TimesUsed.ToString(), true)
				.AddField("ID тега", tag.Id.ToString(), true)).ConfigureAwait(false);
			}
		}

		[Command("Transfer")]
		[Description("Передает кому-либо владельство над тегом")]
		[Aliases("Give")]
		public async Task TransferTag(CommandContext context,
			[Description("Участник сервера которому нужно передать владельство над тегом")] DiscordMember memberToGive,
			[RemainingText, Description("Название тега")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или называться как команды.", nameof(name));
			if (memberToGive.Id == context.Member.Id)
				throw new DiscordUserInputException("Вы не можете передать себе владельство над тегом", nameof(memberToGive));

			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				throw new DiscordUserInputException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				tag.OwnerId = (long) memberToGive.Id;
				this.Database.Tags.Update(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Владельство над тегом {Formatter.InlineCode(tag.Name)} успешно передано {memberToGive.Mention}"))
						.ConfigureAwait(false);
				else
					throw new DatabaseException($"Не удалось передать владельство над тегом { Formatter.InlineCode(tag.Name) } - { memberToGive.Mention}", DatabaseActionType.Update);
			}
		}

		[Command("Claim")]
		[Description("Получить владельство над тегом если его ")]
		public async Task Claim(CommandContext context, [RemainingText, Description("Название тега")] string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new DiscordUserInputException("Название тега не должно быть пустым", nameof(name));
			var gId = (long) context.Guild.Id;
			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			DiscordUser owner = null;
			var ownId = (ulong) tag.OwnerId;
			try
			{
				owner = await context.Guild.GetMemberAsync(ownId).ConfigureAwait(false);
			}
			catch (NotFoundException)
			{
				tag.OwnerId = (long) context.User.Id;
				this.Database.Tags.Update(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Вы успешно получили владельство над тегом {tag.Name}")).ConfigureAwait(false);
					return;
				}
				else
					throw new DatabaseException("Не удалось обновить владельство над тегом  хотя его владелец покинул сервер", DatabaseActionType.Save);
			}
			
			owner = await context.Client.GetUserAsync(ownId).ConfigureAwait(false);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription($"Не удалось получить владельство над тегом {tag.Name}, так как его владелец {owner.Mention} все еще находится на сервере")).ConfigureAwait(false);
			
		}

		[Command("UserStats")]
		[Description("Показывает информацию про пользователя в качестве владельца тегов")]
		[Aliases("Userstat")]
		public async Task UserStats(CommandContext context, DiscordMember member = null)
		{
			if (member == null)
				member = context.Guild.CurrentMember;

			var embed = new DiscordEmbedBuilder()
				.WithAuthor($"{member.Username}#{member.Discriminator}", iconUrl: member.AvatarUrl);

			var gId = (long) context.Guild.Id;
			var mId = (long) member.Id;

			var tags = await this.Database.Tags.Where(x => x.OwnerId == mId && x.GuildId == gId).ToListAsync();

			if (tags?.Any() == false)
			{
				embed.Title = "Данный пользователь не владеет тегами";
				await context.RespondAsync(embed: embed).ConfigureAwait(false);
			}
			else
			{
				await context.RespondAsync(embed: embed.AddField("Количество владеемых тегов", tags.Count.ToString(), true)
								.AddField("Количество использований владеемых тегов", tags.Select(x => x.TimesUsed).Sum().ToString(), true)
								.AddField("Наиболее используемый тег", tags.OrderByDescending(x => x.TimesUsed).First().TimesUsed.ToString(), true)
								.WithColor(Helpers.ExtendedColor(member, context.Guild.CurrentMember)));
			}
		}


		[Command("ServerStats")]
		[Description("Показывает тэг-статистику на данном сервере")]
		[Aliases("serverstat")]
		public async Task ServerStats(CommandContext context)
		{

			var gId = (long) context.Guild.Id;

			var serverTags = await this.Database.Tags.Where(x => x.GuildId == gId).ToListAsync();
			if (serverTags?.Any() == false)
			{
				await context.RespondAsync("На данном сервере нет тегов").ConfigureAwait(false);
				return;
			}
			// I know next region is written in very stupid way but i don't know how to actually improve it.
			// TODO: FIX IT.
			#region stupid
			var tagsOwners = new List<TagsOwner>();
			foreach (var owner in serverTags.Select(x => x.OwnerId).Distinct())
			{
				tagsOwners.Add(new TagsOwner(owner));
			}

			tagsOwners.ForEach(x => x.TimesUsed = serverTags.Where(y => x.OwnerId == y.OwnerId).Select(y => y.TimesUsed).Sum());
			tagsOwners.ForEach(x => x.AmountOfTags = serverTags.Where(y => x.OwnerId == y.OwnerId).Count());
			Tag maxUses = serverTags.OrderByDescending(x => x.TimesUsed).First();
			TagsOwner maxAmountOfTags = tagsOwners.OrderByDescending(x => x.AmountOfTags).First();
			TagsOwner maxAmountOfUses = tagsOwners.OrderByDescending(x => x.TimesUsed).First();
			#endregion
			var embed = new DiscordEmbedBuilder
			{
				Title = "Информация о тегах на сервере " + context.Guild.Name,
				Description = $"{serverTags.Count} тегов, {serverTags.Select(x => x.TimesUsed).Sum()} использований"
			}.AddField("Наиболее исползуемый тег", $"{maxUses.Name} ({maxUses.TimesUsed} использований)", false)
			.AddField("Пользователь с наибольшим количеством тегов", $"<@{maxAmountOfTags.OwnerId}> ({maxAmountOfTags.AmountOfTags} тегов)", false)
			.AddField("Пользователь, теги которого имеют наибольшее количество использованй", $"<@{maxAmountOfUses.OwnerId}> ({maxAmountOfUses.TimesUsed} использований)", false)
			.AddField("Первый созданный тег", $"{serverTags.OrderByDescending(x => x.CreationDate).First().Name} ({serverTags.OrderByDescending(x => x.CreationDate).First().CreationDate.ToString("dddd, MMM dd yyyy", new CultureInfo("ru-Ru"))})", false)
			.AddField("Последний созданный тег", $"{serverTags.OrderBy(x => x.CreationDate).First().Name} ({serverTags.OrderBy(x => x.CreationDate).First().CreationDate.ToString("dddd, MMM dd yyyy", new CultureInfo("ru-Ru"))})");
			await context.RespondAsync(embed: embed).ConfigureAwait(false);
		}



		[GroupCommand]
		public async Task ExecuteGroup(CommandContext context, [RemainingText, Description("Название тега для отображения")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new DiscordUserInputException("Название тега не может быть пустым, полностью состоять из пробелов или быть называться также как и команды.", nameof(name));

			var gId = (long) context.Guild.Id;

			var tag = await this.Database.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
			{
				var nL = new NormalizedLevenshtein();
				var tags = await this.Database.Tags.Where(t => t.GuildId == gId).OrderBy(t => nL.Distance(t.Name, name)).ToArrayAsync().ConfigureAwait(false);
				if (tags?.Any() == false)
					throw new DiscordUserInputException("Тегов на этом сервере не найдено", nameof(tags));
				string suggestions =
					(tags.Length >= 10
					?
					string.Join(", ", tags.Take(10).OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct())
					:
					string.Join(", ", tags.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct()));
				await context.RespondAsync($"Нужного тега не найдено, вот некоторые {Formatter.Italic("возможные варианты того что вы искали")}:\n\u200b{suggestions}").ConfigureAwait(false);
			}
			else
			{
				string content = tag.TagContent.Replace("@here", "@\u200bhere").Replace("@everyone", "@\u200beveryone").Trim();
				await context.RespondAsync($"\u200b{content}").ConfigureAwait(false);
				tag.TimesUsed++;
				this.Database.Tags.Update(tag);
				int rowsAffected = await this.Database.SaveChangesAsync();
				if (rowsAffected <= 0)
					throw new DatabaseException("Не удалось обновить количество использований в базе данных", DatabaseActionType.Save);
			}


		}

	}

	sealed class TagsOwner
	{
		public long OwnerId { get; set; }
		public int TimesUsed { get; set; }
		public int AmountOfTags { get; set; }
		public TagsOwner(long id, int used)
		{
			this.OwnerId = id;
			this.TimesUsed = used;
		}
		public TagsOwner(long id)
			=> this.OwnerId = id;
	}
}