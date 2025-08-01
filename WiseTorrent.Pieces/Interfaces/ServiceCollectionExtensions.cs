using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Pieces.Classes;

namespace WiseTorrent.Pieces.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddPiecesDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IPieceManager, PieceManager>();
			services.AddSingleton<IPieceSelector, PieceSelector>();
			return services;
		}
	}
}
