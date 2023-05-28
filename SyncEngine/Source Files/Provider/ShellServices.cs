using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace SyncEngine
{
	//===============================================================
	// ShellServices
	//
	//	 Registers a bunch of COM objects that implement the various
	//	 whizbangs and gizmos that Shell needs for things like
	//	 thumbnails, context menus and custom states.
	//
	// Fakery Factor:
	//
	//   Not a lot here. The classes referenced are all fakes,
	//	 but you could prolly modify them with ease.
	//
	//===============================================================

	internal class ShellServices
	{

		public static void InitAndStartServiceTask()
		{
			// Will be implemented later... or not... idk
		}
	}
}
