using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			return services;
		}
	}
}
