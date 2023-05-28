using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class ServerProviderStateChangedEventArgs : EventArgs
	{
        private ServerProviderStatus _status;
        private string _message;

        public ServerProviderStatus Status { get { return _status; } }
		public string Message { get { return _message; } }

        public ServerProviderStateChangedEventArgs(ServerProviderStatus status)
        {
            _status = status;
            _message=status.ToString();
        }

		public ServerProviderStateChangedEventArgs(ServerProviderStatus status, string message)
        {
            _status = status;
            _message = message;
        }
    }

    public class FileChangedEventArgs : EventArgs
    {
        public WatcherChangeTypes ChangeType;
        public bool ResyncSubDirectories;
    }
}
