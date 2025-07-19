using BencodeNET.Torrents;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Parsing.Classes;

namespace WiseTorrent.Parsing.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddParsingDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IBEncodeReader, BEncodeReader>();
			services.AddSingleton<ITorrentParser, Classes.TorrentParser>();
			return services;
		}
	}

}
