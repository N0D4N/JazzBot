﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace JazzBot.Attributes
{
	/// <summary>
	/// Specifies that command or group of commands can only be executed by owner of the bot or user with specified permissions.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class OwnerOrPermissionAttribute : CheckBaseAttribute
	{
		/// <summary>
		/// Permissions needed to execute command.
		/// </summary>
		public Permissions Permissions { get; private set; }

		public OwnerOrPermissionAttribute(Permissions permissions) => this.Permissions = permissions;

		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var app = ctx.Client.CurrentApplication;
			var me = ctx.Client.CurrentUser;

			if (app != null && app.Owners.Any(x => x.Id == ctx.User.Id))
				return Task.FromResult(true);

			if (ctx.User.Id == me.Id)
				return Task.FromResult(true);

			var usr = ctx.Member;
			if (usr == null)
				return Task.FromResult(false);
			var pusr = ctx.Channel.PermissionsFor(usr);

			return Task.FromResult((pusr & this.Permissions) == this.Permissions);
		}
	}
}
