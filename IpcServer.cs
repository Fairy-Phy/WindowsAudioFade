using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace WindowsAudioFade {

	class IpcServer {

		public IpcObject RemoteObject { get; set; }

		private IpcServerChannel IpcChannel;

		public IpcServer() {
			IpcChannel = new IpcServerChannel("_WAF_");

			ChannelServices.RegisterChannel(IpcChannel, true);

			RemoteObject = new IpcObject();
			RemotingServices.Marshal(RemoteObject, "pipe", typeof(IpcObject));
		}

		public void Send(string Text) => RemoteObject.Text = Text;
	}
}
