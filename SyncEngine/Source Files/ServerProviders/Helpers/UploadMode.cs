using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public enum UploadMode : short
	{
		Create = 0,
		Update = 1,
		Resume = 3,
	}
}
