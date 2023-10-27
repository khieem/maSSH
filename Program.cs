using System.Collections.Specialized;
using Renci.SshNet.Common;
using Salaros.Configuration;

namespace massh
{
    class Program
	{
		// cơ chế lock đơn giản, mong là đủ tránh race condition
		static volatile bool outputLock = false;

		// map chứa cặp key-value cấu hình, đọc từ App.conf
		// static readonly System.Collections.Specialized.NameValueCollection configs = ConfigurationManager.AppSettings;
		const string configPath = @"massh.conf";
		static ConfigParser configParser;
		
		// key: hostname, value: kết quả
		static volatile NameValueCollection runResults = new NameValueCollection();

		// danh sách hostname theo đúng thứ tự cung cấp
		static readonly List<string> orderedServers = new List<string>();
      static readonly List<Session> sessions = new List<Session>();
		const string HEADER = @"HEADER";

		/////////////////// APP.CONFIG ///////////////////
		static string logto = "console";
		static bool parallel = false;
		static bool sftp = false;
		static bool ssh = false;
		static string fpath;
		static string cpath = "commands.txt";

		//////////////////// FUNC ////////////////////////
		static void Preprocess()
		{
			string rawConfig = "";
			try
			{
				rawConfig = File.ReadAllText(configPath);
			}
			catch (FileNotFoundException)
			{
				File.WriteAllText("massh.conf", @"# sửa đổi, đóng góp: https://github.com/khieem/maSSH
#
# logto = [console|file|both]
#     console: chỉ in kết quả ra màn hình
#     file   : chỉ in kết quả ra log.txt
#     both   : cả 2
logto = console

# parallel = [on|true|enable] | [off|false|disable]
# kết nối song song giúp giảm thời gian chạy
# tuy nhiên thứ tự kết quả in ra màn hình xáo trộn
# kết quả lưu vào file không ảnh hưởng
# hiệu quả với sftp, ít hiệu quả hơn với ssh
parallel = off

# sftp = [on|true|enable] | [off|false|disable]
sftp = off

# fpath = <filepath>
# đường dẫn tới file cần truyền, không hỗ trợ thư mục
fpath = test.test

# ssh = [on|true|enable] | [off|false|disable]
ssh = off

# cpath = <commandpath>
# đường dẫn tới file chứa lệnh
# các lệnh nên nằm trên cùng 1 dòng và cách nhau bởi ';'
cpath = commands.txt");
				Console.WriteLine("massh.conf created.");
			}
			string config = "[HEADER]" + Environment.NewLine + rawConfig;

			ConfigParserSettings settings = new ConfigParserSettings
			{
				CommentCharacters = new string[] {"#"},
				MultiLineValues = MultiLineValues.Simple | MultiLineValues.QuoteDelimitedValues
			};
			configParser = new ConfigParser(config, settings);

			logto 	= configParser.GetValue(HEADER, "logto", logto);
			parallel = configParser.GetValue(HEADER, "parallel") == "true";
			sftp 		= configParser.GetValue(HEADER, "sftp"		) == "on";
			ssh 		= configParser.GetValue(HEADER, "ssh"		) == "on";
			fpath 	= configParser.GetValue(HEADER, "fpath", "nofile");
			cpath 	= configParser.GetValue(HEADER, "cpath", "nofile");
	
			if (sftp)
			{
				if (fpath == "nofile")
				{
					throw new FileNotFoundException("Cung cấp đường dẫn tới file cần truyền qua key fpath");
				}
			}
			if (ssh)
			{
				if (cpath == "nofile")
				{
					throw new FileNotFoundException("Cung cấp đường dẫn tới file chứa lệnh qua key cpath");
				}
			}

			// xoá log của phiên trước
			File.Delete("log.txt");
			foreach (var f in Directory.EnumerateFiles(".", "*stacktrace.txt"))
			{
				File.Delete(f);
			}
			foreach (var f in Directory.EnumerateFiles(".", "*error"))
			{
				File.Delete(f);
			}
		}
		static void WriteLog(string host, string cmd, string res)
		{		
			// độ dài của hostname dài nhất, dùng để căn đều khi có nhiều hostname
			int maxLength = orderedServers.Max(s => s.Length);
			int gap = maxLength - host.Length;
			
			// tách result thành nhiều dòng để cách dòng trên tất cả
			string[] result = res.Split(Environment.NewLine);

			// độn thêm chuỗi khoảng trắng để căn đều hostname
			Console.WriteLine($"{host}{new string(' ', gap)} | [In] : {cmd}");

			// dòng kết quả đầu nằm cùng dòng với chuỗi [Out] nên xử lý riêng
			Console.Write($"{new string(' ', maxLength)} | [Out]: {result[0]}");

			// từ dòng kết quả thứ 2 trở đi 
			for (int i = 1; i < result.Length; i++)
			{
				Console.WriteLine($"{new string(' ', maxLength+9)} {result[i]}");		// 9 là độ dài của đoạn ...[Out]... 
			}
		}

		// đọc hostname, ip, username, password từ file .csv
		static void ReadServersList(string path)
		{
			string name, host, usrn, pswd;
			string[] line;
			using (StreamReader sr = new StreamReader(path))
			{
				while (sr.Peek() >= 0)
				{
					line = sr.ReadLine().Split(',');
					orderedServers.Add(name = line[0]);
					sessions.Add(new Session()
					{
						name = name = line[0],
						host = host = line[1],
						usrn = usrn = line[2],
						pswd = pswd = line[3]
					});
				}
			}
		}

		/////////////////// MAIN ////////////////////////
		public static void Main(string[] args)
		{
			try
			{
				Preprocess();
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine(e.Message);
				return;
			}
			catch (Exception e)
			{
				File.WriteAllText("stacktrace.txt",e.ToString());
				return;
			}

			ReadServersList("server.csv");

			// đọc danh sách lệnh từ file
			List<string> commands = new();
			using StreamReader sr = new(cpath);
			while (sr.Peek() >= 0)
			{
				commands.Add(sr.ReadLine());
			}
			sr.Close();

			// mặc định chạy với tối đa số cpu, có thể cần chỉnh lại
			int maxConnections = parallel ? System.Environment.ProcessorCount : 1;
			var opts = new ParallelOptions { MaxDegreeOfParallelism = maxConnections };

			if (sftp)
			{
				Parallel.ForEach(sessions, opts, ss => {
					using SFTPc client = new SFTPc(ss, fpath);
					// using FileStream fs = new FileStream(fpath, FileMode.Open);
					client.UploadSingleFile();
				});
				
			}

			if (ssh)
			{
				Parallel.ForEach(sessions, opts, ss => {
					using (SSHc client = new SSHc(ss))
					{
						foreach (string command in commands)
						{
							string result = "";
							try
							{
								result = client.Execute(command);			// kết quả chạy lệnh
								WriteLog(ss.name, command, result);					// in kết quả ra màn hình
							}
							catch (Exception e)
							{
								File.WriteAllText(ss.name + ".stacktrace.txt", e.ToString());
							}
							if (!outputLock)											// cơ chế lock đơn giản, mong là đủ để ngăn race condition
							{
								outputLock = true;

								runResults[ss.name] += result;					// thêm kết quả vào map để sắp xếp và in ra file nếu cần

								outputLock = false;
							}
						}
					}
				});

				if (logto != "console")
				{
					StreamWriter o = new StreamWriter("log.txt");
					TextWriter stdOut = Console.Out;			// store old stdout before binding to logfile 
					o.AutoFlush = true;
					Console.SetOut(o);
					foreach (string s in orderedServers)
					{
						foreach (var cmd in commands)
						{
							WriteLog(s, cmd, runResults[s]);
						}
					}
					Console.SetOut(stdOut);						// restore stdout after writing 
				}
			}
			Console.WriteLine();
			Console.WriteLine("hoàn tất. nhấn phím bất kỳ để thoát.");
			Console.Write("> ");
			Console.ReadKey();
		}
	}
}
