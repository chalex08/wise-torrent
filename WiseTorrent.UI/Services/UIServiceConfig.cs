﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WiseTorrent.UI.Services
{
    public static class UIServiceConfig
    {
	    public static void ConfigureServices(IServiceCollection services)
	    {
		    services.AddSingleton<FullscreenStateService>();
	    }
	}
}
