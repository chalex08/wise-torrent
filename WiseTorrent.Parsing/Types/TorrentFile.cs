using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Parsing.Types
{
	public class TorrentFile(ByteSize length, List<string> pathSegments)
	{
		public ByteSize Length { get; set; } = length;
		public List<string> PathSegments { get; set; } = pathSegments;
		public string RelativePath => string.Join(Path.DirectorySeparatorChar, PathSegments);
	}
}
