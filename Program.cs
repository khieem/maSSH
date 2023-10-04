
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;

namespace massh
{
	class Program
	{
		// map chứa cặp key-value cấu hình, đọc từ App.conf
		static readonly System.Collections.Specialized.NameValueCollection configs = ConfigurationManager.AppSettings;

		static readonly string logto = configs["logto"] ?? "console";

		public static void Main(string[] args)
		{
			// đầu ra cho kết quả: logfile và (hoặc) console
			Trace.Listeners.Clear();
			TextWriterTraceListener logfile = new TextWriterTraceListener("result.txt");
			ConsoleTraceListener console = new ConsoleTraceListener(false);

			if (logto == "both")
			{
				Trace.Listeners.Add(logfile);
				Trace.Listeners.Add(console);
			}
			else if (logto == "console")
			{
				Trace.Listeners.Add(console);
			}
			else
			{
				Trace.Listeners.Add(logfile);
			}
			Trace.AutoFlush = true;

			Session ss = new()
			{
				host = "127.0.0.1",
			// string port = "22";
				usrn = "khiemvn",
				pswd = "khiemvn"
			};

			Session _ss = new()
			{
				host = "127.0.0.1",
				// string port = "22";
				usrn = "khiemvn",
				pswd = "khiemvn",
			};

			Session __ss = new()
			{
				host = "127.0.0.1",
				// string port = "22";
				usrn = "root",
				pswd = "root",
			};

			int maxConnections = System.Environment.ProcessorCount;
			var opts = new ParallelOptions { MaxDegreeOfParallelism = maxConnections };
			Session[] sessions = { ss, _ss, __ss };
			
			Parallel.ForEach(sessions, opts, ss => {
				using (var client = new SSHc(ss))
				{
					List<string> commands = new List<string>()
					{
						"w",
						"date",
						"sleep 5"
					};

					foreach (string command in commands) {
						// Console.WriteLine("[In]: " + command);
						Trace.WriteLine(client.Execute(command));         
					}
				}
			});
		}
	}
}
