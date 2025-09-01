using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Pieces.Classes;

namespace WiseTorrent.Pieces.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static void AddPiecesDependencies(this IServiceCollection services)
		{
			services.AddSingleton<Func<int, IPieceManager>>(totalPieceCount => new PieceManager(totalPieceCount));
		}
	}
}
