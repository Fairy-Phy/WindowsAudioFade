using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace WindowsAudioFade {

	class IpcClient {

		public IpcObject RemoteObject { get; set; }

		private IpcClientChannel Channel;

		public IpcClient() {
			Channel = new IpcClientChannel();

			ChannelServices.RegisterChannel(Channel, true);

			RemoteObject = Activator.GetObject(typeof(IpcObject), "ipc://_WAF_/pipe") as IpcObject;
		}

		public string GetText() => RemoteObject.Text;

	}
}
