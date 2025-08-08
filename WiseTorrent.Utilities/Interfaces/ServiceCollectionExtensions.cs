using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Utilities.Classes;

namespace WiseTorrent.Utilities.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddUtilityDependencies(this IServiceCollection services)
		{
			services.AddSingleton(typeof(ILogger<>), typeof(BufferLogger<>));
			services.AddSingleton<ILogService, LogService>();
			return services;
		}
	}

}
