using System;

namespace WindowsAudioFade {

	class IpcObject : MarshalByRefObject {

		public string Text { get; set; }

		public override object InitializeLifetimeService() => null;
	}
}
