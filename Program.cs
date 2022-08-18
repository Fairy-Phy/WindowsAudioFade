using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsAudioFade {

	class Program {

		static string SoundDevice;

		const float vlcNoTalkVol = 0.34f;

		const float vlcTalkVol = 0.09f;

		const float osuGameVol = 1.0f;

		const float MuteVol = 0.0001f;

		const float FadeTimeMS = 250.0f;

		static MMDevice GetAudioDevice() {
			MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
			return (from x in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active) where x.FriendlyName == SoundDevice select x).FirstOrDefault();
		}

		static void SelectDevice() {
			MMDeviceCollection devicecollection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
			int CurrentSelect = 0;
			//bool Breaked = false;
			StringBuilder SelectText = new StringBuilder();

			Console.Clear();
			SelectText.Clear();

			SelectText.AppendLine("Select Device:");
			for (int i = 0; i < devicecollection.Count; i++) {
				//if (CurrentSelect == i) SelectText.Append(">");
				//else SelectText.Append(" ");

				SelectText.AppendLine(string.Format(" {0}", devicecollection[i].FriendlyName));
			}
			SelectText.AppendLine(" Device Update");

			Console.Write(SelectText.ToString());
			int InputLeft = Console.CursorLeft;
			int InputTop = Console.CursorTop;

			Console.SetCursorPosition(0, 1 + CurrentSelect);
			Console.Write(">");

			while (true) {
				Console.SetCursorPosition(InputLeft, InputTop);
				ConsoleKeyInfo KeyInfo = Console.ReadKey();

				int PrevSelect = CurrentSelect;
				if (KeyInfo.Key == ConsoleKey.UpArrow) {
					if (CurrentSelect != 0) CurrentSelect--;
					//break;
				}
				else if (KeyInfo.Key == ConsoleKey.DownArrow) {
					if (CurrentSelect != devicecollection.Count) CurrentSelect++;
					//break;
				}
				else if (KeyInfo.Key == ConsoleKey.Enter) {
					if (CurrentSelect == devicecollection.Count) {
						SelectDevice();
						return;
					}

					SoundDevice = devicecollection[CurrentSelect].FriendlyName;
					break;
				}

				Console.SetCursorPosition(0, 1 + PrevSelect);
				Console.Write(" ");
				Console.SetCursorPosition(0, 1 + CurrentSelect);
				Console.Write(">");
			}

			Console.WriteLine("Select Device: {0}", SoundDevice);
		}

		readonly static string osuProcessName = "osu!";
		readonly static string vlcProcessName = "vlc";

		static (SimpleAudioVolume, SimpleAudioVolume, SimpleAudioVolume, SimpleAudioVolume) SessionFinder(MMDevice device) {
			SimpleAudioVolume vlcA, o1, o2, o3;
			(vlcA, o1, o2, o3) = (null, null, null, null);

			Console.WriteLine("{0}: {1}", device.FriendlyName, device.ID);
			for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++) {
				var session = device.AudioSessionManager.Sessions[i];

				try {
					var process = Process.GetProcessById((int) session.GetProcessID);
					Console.WriteLine("  {0}: {1}", session.GetProcessID, process.ProcessName);
					if (process.ProcessName == vlcProcessName) vlcA = session.SimpleAudioVolume;
					else if (process.ProcessName == osuProcessName) {
						if (o1 is null) o1 = session.SimpleAudioVolume;
						else if (o2 is null) o2 = session.SimpleAudioVolume;
						else o3 = session.SimpleAudioVolume;
					}
				}
				catch (ArgumentException) { }
			}

			return (vlcA, o1, o2, o3);
		}

		static async Task Main(string[] Args) {
			if (Args.Length > 0) {
				SoundDevice = string.Join(" ", Args);
				Console.WriteLine("Args sound device: {0}", SoundDevice);

				Console.WriteLine("Ipc starting...");
				IpcClient client = new IpcClient();
				Console.WriteLine("Event making...");

				GetAudioDevice().AudioSessionManager.OnSessionCreated += (object _, IAudioSessionControl newSession) => {
					newSession.GetGroupingParam(out Guid guid);

					Console.WriteLine(guid);

					var device = GetAudioDevice();
					device.AudioSessionManager.RefreshSessions();

					AudioSessionControl sessionRes = null;
					for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++) {
						var session = device.AudioSessionManager.Sessions[i];

						if (guid == session.GetGroupingParam()) {
							sessionRes = session;
							break;
						}
					}

					if (sessionRes is null) {
						Console.WriteLine("Session not found.");
						return;
					}
					Console.WriteLine("Session found!");
					Console.WriteLine("guid: {0}", sessionRes.GetGroupingParam());

					var process = Process.GetProcessById((int) sessionRes.GetProcessID);
					Console.WriteLine("Process name: {0}", process.ProcessName);
					if (process.ProcessName == "vlc") {
						switch (client.GetText()) {
							case string mode when mode == "0":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = vlcTalkVol;
								break;
							case string mode when mode == "1":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = MuteVol;
								break;
							case string mode when mode == "2":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = vlcTalkVol;
								break;
							case "3":
							default:
								Console.WriteLine("Current Value: 3 (default)");
								sessionRes.SimpleAudioVolume.Volume = vlcNoTalkVol;
								break;
						}
					}
					else if (process.ProcessName == "osu!") {
						switch (client.GetText()) {
							case string mode when mode == "0":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = MuteVol;
								break;
							case string mode when mode == "1":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = osuGameVol;
								break;
							case string mode when mode == "2":
								Console.WriteLine("Current Value: {0}", mode);
								sessionRes.SimpleAudioVolume.Volume = MuteVol;
								break;
							case "3":
							default:
								Console.WriteLine("Current Value: 3 (default)");
								sessionRes.SimpleAudioVolume.Volume = MuteVol;
								break;
						}
					}
					else Console.WriteLine("Not target process");
				};

				Console.WriteLine("Inited!");

				string Prev = "";
				while (true) {
					string current = client.GetText();
					if (current == "exit") break;
					
					if (current != Prev) {
						Console.WriteLine("Change Value: {0}", current);
						Prev = current;
					}
				}
				return;
			}

			SelectDevice();

			{
				MMDevice device = GetAudioDevice();

				if (device is null) {
					Console.WriteLine("Not exist");
					await Task.Delay(3000);
					return;
				}

				var (vlcVol, osuVol1, osuVol2, osuVol3) = SessionFinder(device);

				if (vlcVol is null) {
					Console.WriteLine("vlc is not found");
					await Task.Delay(3000);
					return;
				}
				if (osuVol1 is null && osuVol2 is null && osuVol3 is null) {
					Console.WriteLine("osu is not found");
					await Task.Delay(3000);
					return;
				}
				Console.WriteLine("Session Found!");

				Console.WriteLine("Sound volume change test...");
				Console.WriteLine("osu -> 100, vlc -> 0");
				vlcVol.Volume = MuteVol;
				osuVol1.Volume = osuGameVol;
				if (!(osuVol2 is null)) osuVol2.Volume = osuGameVol;
				if (!(osuVol3 is null)) osuVol3.Volume = osuGameVol;

				await Task.Delay(3000);

				Console.WriteLine("osu -> 0, vlc -> 34");
				vlcVol.Volume = vlcNoTalkVol;
				osuVol1.Volume = MuteVol;
				if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
				if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;
				Console.WriteLine("Test finished");
			}

			Console.WriteLine("Ipc starting...");
			IpcServer server = new IpcServer();
			server.Send("3");

			Process EventProcess = Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, SoundDevice));


			Console.WriteLine("Inited!");

			while (true) {
				Console.Write("Select Mode: ");
				string inStr = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(inStr)) continue;
				else if (inStr == "exit") break;

				if (!int.TryParse(inStr, out int mode)) continue;

				MMDevice device = GetAudioDevice();

				if (device is null) {
					Console.WriteLine("Not exist");
					continue;
				}

				var (vlcVol, osuVol1, osuVol2, osuVol3) = SessionFinder(device);

				if (vlcVol is null | (osuVol1 is null && osuVol2 is null && osuVol3 is null)) {
					Console.WriteLine("Vol is not found");
					continue;
				}

				Console.WriteLine("Session Found!");

				// vlc 34 -> 11
				if (mode == 0) {
					float decvol = (vlcTalkVol - vlcNoTalkVol) / FadeTimeMS;

					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;
					vlcVol.Volume = vlcNoTalkVol;
					for (float i = 0; i < FadeTimeMS; i++) {
						vlcVol.Volume += decvol;
						await Task.Delay(1);
					}
					vlcVol.Volume = vlcTalkVol;
					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;

					server.Send("0");
				}
				// vlc 11 -> 0, osu 0 -> 100
				else if (mode == 1) {
					float vlcdecvol = -vlcTalkVol / FadeTimeMS;
					float osuincvol = osuGameVol / FadeTimeMS;

					vlcVol.Volume = vlcTalkVol;
					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;
					for (float i = 0; i < FadeTimeMS; i++) {
						vlcVol.Volume += vlcdecvol;
						osuVol1.Volume += osuincvol;
						if (!(osuVol2 is null)) osuVol2.Volume += osuincvol;
						if (!(osuVol3 is null)) osuVol3.Volume += osuincvol;
						await Task.Delay(1);
					}
					vlcVol.Volume = MuteVol;
					osuVol1.Volume = osuGameVol;
					if (!(osuVol2 is null)) osuVol2.Volume = osuGameVol;
					if (!(osuVol3 is null)) osuVol3.Volume = osuGameVol;

					server.Send("1");
				}
				// vlc 0 -> 11, osu 100 -> 0
				else if (mode == 2) {
					float vlcincvol = vlcTalkVol / FadeTimeMS;
					float osudecvol = -osuGameVol / FadeTimeMS;

					vlcVol.Volume = MuteVol;
					osuVol1.Volume = osuGameVol;
					if (!(osuVol2 is null)) osuVol2.Volume = osuGameVol;
					if (!(osuVol3 is null)) osuVol3.Volume = osuGameVol;
					for (float i = 0; i < FadeTimeMS; i++) {
						vlcVol.Volume += vlcincvol;
						osuVol1.Volume += osudecvol;
						if (!(osuVol2 is null)) osuVol2.Volume += osudecvol;
						if (!(osuVol3 is null)) osuVol3.Volume += osudecvol;
						await Task.Delay(1);
					}
					vlcVol.Volume = vlcTalkVol;
					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;

					server.Send("2");
				}
				// vlc 11 -> 34
				else if (mode == 3) {
					float incvol = (vlcNoTalkVol - vlcTalkVol) / FadeTimeMS;

					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;
					vlcVol.Volume = vlcTalkVol;
					for (float i = 0; i < FadeTimeMS; i++) {
						vlcVol.Volume += incvol;
						await Task.Delay(1);
					}
					vlcVol.Volume = vlcNoTalkVol;
					osuVol1.Volume = MuteVol;
					if (!(osuVol2 is null)) osuVol2.Volume = MuteVol;
					if (!(osuVol3 is null)) osuVol3.Volume = MuteVol;

					server.Send("3");
				}
				else continue;

				Console.WriteLine("Success");
			}

			server.Send("exit");
			EventProcess.WaitForExit();
		}
	}
}
