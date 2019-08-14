﻿using System;
using System.Collections.Generic;
using System.Text;
using File = TagLib.File;
using DSharpPlus.Entities;
using JazzBot.Services;
using System.Linq;
using System.Threading.Tasks;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;
using JazzBot.Enums;
using System.IO;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace JazzBot.Data
{
	class RemoteMusicData
	{
		public List<RemoteMusicItem> Queue { get; private set; }

		public RemoteMusicData()
			=> this.Queue = new List<RemoteMusicItem>();
		

		public void Shuffle()
			=> this.Queue.Shuffle();

		public void Add(RemoteMusicItem song)
			=> this.Queue.Add(song);
		

		public void Add(IEnumerable<RemoteMusicItem> songs)
			=> this.Queue.AddRange(songs);
		

		public LavalinkTrack GetSong()
		{
			var track = this.Queue[0];
			this.Queue.RemoveAt(0);
			return track.Track;
		}
	}
}
