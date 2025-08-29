using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Storage.Classes;

namespace WiseTorrent.Storage.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddStorageDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IDiskAllocator, DiskAllocator>();
			services.AddSingleton<IFileIO, FileIO>();
			services.AddSingleton<IFileManager, FileManager>();
			services.AddSingleton<IStorageServiceTaskClient, StorageServiceTaskClient>();
			return services;
		}
	}
}
