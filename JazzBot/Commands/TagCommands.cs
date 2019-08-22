using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
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
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;

namespace JazzBot.Commands
{
	[Group("Tag")]
	[Description("Команды тегов")]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class TagCommands : BaseCommandModule
	{
		private static ImmutableArray<string> ForbiddenNames { get; } = new ImmutableArray<string>() { "create", "make", "delete", "remove", "force_delete", "force_remove", "edit", "modify", "force_edit", "force_modify", "info", "list", "@everyone", "@here", "transfer", "give", "claim", "userstats", "userstat", "serverstats", "serverstat" };

		[Command("Create")]
		[Description("Создает тег с заданным названием и контентом")]
		[Aliases("make")]
		[Cooldown(2, 180, CooldownBucketType.User)]
		public async Task CreateAsync(CommandContext context,
			[Description("Название тега")] string name,
			[RemainingText, Description("Содержимое тега")] string contents)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или иметь такое же название как команды.", nameof(name));

			if (string.IsNullOrWhiteSpace(contents))
				throw new ArgumentException("Содержимое тега не может быть пустым или содержать только пробелы.", nameof(contents));

			if (contents.Length > 2000)
				throw new ArgumentException("Длина содержимого тега не может превышать 2000 символов.", nameof(contents));

			name = name.ToLowerInvariant();

			var tag = new Tag
			{
				Id = Convert.ToInt64(DateTimeOffset.Now.ToUnixTimeMilliseconds()),
				Name = name,
				TagContent = contents,
				RevisionDate = DateTime.Now,
				GuildId = (long) context.Guild.Id,
				OwnerId = (long) context.User.Id,
				CreationDate = DateTime.Now,
				TimesUsed = 0
			};

			var db = new DatabaseContext();
			if (db.Tags?.Any(t => t.Name == tag.Name && t.GuildId == tag.GuildId) == true)
			{
				db.Dispose();
				throw new ArgumentException("Тег с таким именем существует на данном сервере.", nameof(name));
			}
			else
			{
				await db.Tags.AddAsync(tag).ConfigureAwait(false);
				var modCount = await db.SaveChangesAsync();
				db.Dispose();
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle("Тег успешно создан")).ConfigureAwait(false);
				}
				else
					throw new CustomJbException($"Не удалось создать тег {name}. Попробуйте снова.", ExceptionType.DatabaseException);
			}
		}

		[Command("Delete")]
		[Description("Удаляет выбранный тег")]
		[Aliases("remove")]
		public async Task DeleteTag(CommandContext context,
			[RemainingText, Description("Название тега который нужно удалить")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться так же как и команды.", nameof(name));

			var db = new DatabaseContext();
			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				db.Dispose();
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				db.Tags.Remove(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				db.Dispose();
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно удален")).ConfigureAwait(false);

				}
				else
					throw new CustomJbException($"Не удалось удалить тег {name}. Убедитесь в том что тег существует, в правильности написания названия и в том что вы являетесь владельцем тега.", ExceptionType.DatabaseException);
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
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));

			var db = new DatabaseContext();
			name = name.ToLowerInvariant();
			var gId = (long) context.Guild.Id;
			Tag tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
			{
				db.Dispose();
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия названия тега.", nameof(name));
			}
			else
			{
				db.Tags.Remove(tag);
				var modCount = await db.SaveChangesAsync();
				db.Dispose();
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно удален")).ConfigureAwait(false);
				}
				else
					throw new CustomJbException($"Не удалось удалить тег {name}. Убедитесь в том что тег существует и правильности написания названия.", ExceptionType.DatabaseException);
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
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));


			if (string.IsNullOrWhiteSpace(newContent))
				throw new ArgumentException("Содержимое тега не может быть пустым или содержать одни пробелы.", nameof(newContent));

			if (newContent.Length > 2000)
				throw new ArgumentException("Длина содержимого тега не должна превышать.", nameof(newContent));

			var db = new DatabaseContext();

			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			Tag tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				db.Dispose();
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				tag.TagContent = newContent;
				tag.RevisionDate = DateTime.Now;
				db.Tags.Update(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				db.Dispose();
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Тег {name} успешно изменен")).ConfigureAwait(false);

				}
				else
					throw new CustomJbException($"Не удалось изменить тег {name}", ExceptionType.DatabaseException);
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
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));

			if (string.IsNullOrWhiteSpace(newContent))
				throw new ArgumentException("Контент тега не может быть пустым или содержать одни пробелы.", nameof(newContent));

			if (newContent.Length > 2000)
				throw new ArgumentException("Контент тега не может содержать больше 2000 символов.", nameof(newContent));

			var db = new DatabaseContext();

			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;

			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия.", nameof(name));
			else
			{
				tag.TagContent = newContent;
				tag.RevisionDate = DateTime.Now;
				db.Tags.Update(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle("Тег успешно изменен")).ConfigureAwait(false);
					db.Dispose();
				}
				else
				{
					db.Dispose();
					throw new CustomJbException("Не удалось изменить тег", ExceptionType.DatabaseException);
				}
			}
		}

		[Command("List")]
		[Description("Показывает список тегов на этом сервере")]
		[Cooldown(2, 600, CooldownBucketType.Channel)]
		public async Task List(CommandContext context)
		{
			var db = new DatabaseContext();

			var gId = (long) context.Guild.Id;

			var tagsArray = await db.Tags.Where(t => t.GuildId == gId).ToArrayAsync().ConfigureAwait(false);
			db.Dispose();
			if (tagsArray?.Any() == true)
			{
				string tagsNames = string.Join(", ", tagsArray.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct());
				var embedRespond = EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Теги на этом сервере");
				if (tagsNames.Length > 2048)
				{
					var tags = tagsArray.OrderBy(x => x.Name).Select(x => $"{Formatter.InlineCode(x.Name)},").ToList();

					// Deleting last coma.
					tags[tags.Count - 1].Remove(tags[tags.Count - 1].Length - 1, 1);

					tagsNames = this.TagNamesNormalizer(tags);
					var interactivity = context.Client.GetInteractivity();
					var tagsPaginated = interactivity.GeneratePagesInEmbed(tagsNames, SplitType.Character, embedRespond);
					await interactivity.SendPaginatedMessageAsync
						(context.Channel, context.User, tagsPaginated, behaviour: PaginationBehaviour.WrapAround, deletion: PaginationDeletion.DeleteEmojis).ConfigureAwait(false);

				}
				else
				{
					await context.RespondAsync(embed: embedRespond.WithDescription(tagsNames)).ConfigureAwait(false);
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
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться также как и команды.", nameof(name));
			var db = new DatabaseContext();
			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;

			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			db.Dispose();
			if (tag == null)
			{
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия.", nameof(name));
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
				.AddField("Дата последнего изменения", tag.RevisionDate.ToString("yyyy-MM-dd HH:mm:ss zzz"), true)
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
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или называться как команды.", nameof(name));
			if (memberToGive.Id == context.Member.Id)
				throw new ArgumentException("Вы не можете передать себе владельство над тегом", nameof(memberToGive));
			var db = new DatabaseContext();
			name = name.ToLowerInvariant();

			var gId = (long) context.Guild.Id;
			var uId = (long) context.User.Id;

			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId && t.OwnerId == uId).ConfigureAwait(false);
			if (tag == null)
			{
				db.Dispose();
				throw new ArgumentException("Тега с таким названием на этом сервере не существует, убедитесь в правильности написания названия и в том что вы являетесь владельцем данного тега.", nameof(name));
			}
			else
			{
				tag.OwnerId = (long) memberToGive.Id;
				db.Tags.Update(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				db.Dispose();
				if (modCount > 0)
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Владельство над тегом {Formatter.InlineCode(tag.Name)} успешно передано {memberToGive.Mention}"))
						.ConfigureAwait(false);
				else
					throw new CustomJbException($"Не удалось передать владельство над тегом { Formatter.InlineCode(tag.Name) } - { memberToGive.Mention}", ExceptionType.DatabaseException);
			}
		}

		[Command("Claim")]
		[Description("Получить владельство над тегом если его ")]
		public async Task Claim(CommandContext context, [RemainingText, Description("Название тега")] string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Название тега не должно быть пустым", nameof(name));
			var db = new DatabaseContext();
			var gId = (long) context.Guild.Id;
			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			DiscordUser owner = null;
			var ownId = (ulong) tag.OwnerId;
			try
			{
				owner = await context.Guild.GetMemberAsync(ownId).ConfigureAwait(false);
			}
			catch (NotFoundException)
			{
				tag.OwnerId = (long) context.User.Id;
				db.Tags.Update(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				db.Dispose();
				if (modCount > 0)
				{
					await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
						.WithTitle($"Вы успешно получили владельство над тегом {tag.Name}")).ConfigureAwait(false);
				}
				else
					throw new CustomJbException("Не удалось обновить владельство над тегом  хотя его владелец покинул сервер", ExceptionType.DatabaseException);
			}
			finally
			{
				db.Dispose();
				owner = await context.Client.GetUserAsync(ownId).ConfigureAwait(false);
				throw new CustomJbException($"Не удалось получить владельство над тегом {tag.Name}, так как его владелец {owner.Username}#{owner.Discriminator} все еще находится на сервере", ExceptionType.Unknown);
			}
		}

		[Command("UserStats")]
		[Description("Показывает информацию про пользователя в качестве владельца тегов")]
		[Aliases("Userstat")]
		public async Task UserStats(CommandContext context, DiscordMember member = null)
		{
			if (member == null)
				member = context.Guild.CurrentMember;

			var db = new DatabaseContext();
			var embed = new DiscordEmbedBuilder()
				.WithAuthor($"{member.Username}#{member.Discriminator}", iconUrl: member.AvatarUrl);

			var gId = (long) context.Guild.Id;
			var mId = (long) member.Id;

			var tags = await db.Tags.Where(x => x.OwnerId == mId && x.GuildId == gId).ToListAsync();
			db.Dispose();

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
			var db = new DatabaseContext();

			var gId = (long) context.Guild.Id;

			var serverTags = await db.Tags.Where(x => x.GuildId == gId).ToListAsync();
			db.Dispose();
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
			.AddField("Первый созданный тег", $"{serverTags.OrderByDescending(x => x.CreationDate).First().Name} ({serverTags.OrderByDescending(x => x.CreationDate).First().CreationDate.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})", false)
			.AddField("Последний созданный тег", $"{serverTags.OrderBy(x => x.CreationDate).First().Name} ({serverTags.OrderBy(x => x.CreationDate).First().CreationDate.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})");
			await context.RespondAsync(embed: embed).ConfigureAwait(false);
		}



		[GroupCommand]
		public async Task ExecuteGroup(CommandContext context, [RemainingText, Description("Название тега для отображения")] string name)
		{
			if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
				throw new ArgumentException("Название тега не может быть пустым, полностью состоять из пробелов или быть называться также как и команды.", nameof(name));

			var db = new DatabaseContext();

			var gId = (long) context.Guild.Id;

			var tag = await db.Tags.SingleOrDefaultAsync(t => t.Name == name && t.GuildId == gId).ConfigureAwait(false);
			if (tag == null)
			{
				var nL = new NormalizedLevenshtein();
				var tags = await db.Tags.Where(t => t.GuildId == gId).OrderBy(t => nL.Distance(t.Name, name)).ToArrayAsync().ConfigureAwait(false);
				db.Dispose();
				if (tags?.Any() == false)
					throw new ArgumentException("Тегов на этом сервере не найдено");
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
				db.Tags.Update(tag);
				var modCount = await db.SaveChangesAsync().ConfigureAwait(false);
				db.Dispose();
				if (modCount <= 0)
					throw new CustomJbException("Не удалось обновить количество использований в базе данных", ExceptionType.DatabaseException);
			}


		}

		private string TagNamesNormalizer(IEnumerable<string> tagnames)
		{
			var separatedPartsTagNames = new List<string>();
			var tempString = new StringBuilder();
			int currentElement = 0;
			while (currentElement != tagnames.Count())
			{
				if (tempString.Length + tagnames.ElementAt(currentElement).Length <= 500)
				{
					tempString.Append(tagnames.ElementAt(currentElement));
					currentElement++;
				}
				else
				{
					while (tempString.Length < 500)
					{
						tempString.Append(' ');
					}
					separatedPartsTagNames.Add(tempString.ToString());
					tempString.Clear();
				}
			}
			return string.Concat(separatedPartsTagNames);
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