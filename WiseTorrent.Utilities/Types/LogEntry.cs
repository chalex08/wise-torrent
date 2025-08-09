using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Utilities.Types
{
    public class LogEntry
    {
	    public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
