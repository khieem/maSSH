using System.Collections.Specialized;
using System.Configuration;

namespace massh
{
    class Program
	{
		// map chứa cặp key-value cấu hình, đọc từ App.conf
		static readonly System.Collections.Specialized.NameValueCollection configs = ConfigurationManager.AppSettings;

		// key: hostname, value: kết quả
		static readonly NameValueCollection runResults = new NameValueCollection();

		// danh sách hostname theo đúng thứ tự cung cấp
		static readonly List<string> orderedServers = new List<string>();
      static readonly List<Session> sessions = new List<Session>();
		static readonly string logto = configs["logto"] ?? "console";

		static void WriteLog(string host, string cmd, string res, string output)
		{
			// độ dài của hostname dài nhất, dùng để căn đều khi có nhiều hostname
			int maxLength = orderedServers.Max(s => s.Length);
			int gap = maxLength - host.Length;

			// tách result thành nhiều dòng để cách dòng trên tất cả
			string[] result = res.Split(new [] { '\r', '\n' });

			// độn thêm chuỗi khoảng trắng để căn đều hostname
			Console.WriteLine($"{host}{new string(' ', gap)} | [In] : {cmd}");

			// dòng kết quả đầu nằm cùng dòng với chuỗi [Out] nên xử lý riêng
			Console.Write($"{new string(' ', maxLength)} | [Out]: {result[0]}");

			// từ dòng kết quả thứ 2 trở đi 
			for (int i = 1; i < result.Length; i++)
			{
			Console.WriteLine($"{new string(' ', maxLength+9)} {result[i]}");		// 9 là độ dài của đoạn ...[Out]... 
			}
			Console.WriteLine();

			if (output != "console")
			{
				// using StreamWriter sw = new("log");
				foreach (string s in orderedServers)
				{
					result = runResults[s].Split(new [] { '\r', '\n'});
					File.AppendAllText("log", $"{host}{new string(' ', gap)} | [In] : {cmd}\n");
					// sw.WriteLine($"{host}{new string(' ', gap)} | [In] : {cmd}");
					// sw.Write($"{new string(' ', maxLength)} | [Out]: {result[0]}");
					File.AppendAllText("log", $"{new string(' ', maxLength)} | [Out]: {result[0]}\n");
					for (int i = 1; i < result.Length; i++)
					{
					// sw.WriteLine($"{new string(' ', maxLength+9)} {result[i]}");
					File.AppendAllText("log", $"{new string(' ', maxLength+9)} {result[i]}\n");
					}
					File.AppendAllText("log", "\n");
				}
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

		public static void Main(string[] args)
		{
			// xoá trắng file log của phiên trước
			File.WriteAllText("log", null);

			ReadServersList("server.csv");

			// đọc danh sách lệnh từ file
			List<string> commands = new();
			using StreamReader sr = new("commands.txt");
			while (sr.Peek() >= 0)
			{
				commands.Add(sr.ReadLine());
			}

			// mặc định chạy với tối đa số cpu, có thể cần chỉnh lại
			int maxConnections = System.Environment.ProcessorCount;
			var opts = new ParallelOptions { MaxDegreeOfParallelism = maxConnections };
			
			Parallel.ForEach(sessions, opts, ss => {
				using (var client = new SSHc(ss))
				{
					foreach (string command in commands) {
						string result = client.Execute(command);		// kết quả chạy lệnh
						runResults[ss.name] += result;					// thêm kết quả vào map để sắp xếp và in ra file nếu cần
						WriteLog(ss.name, command, result, logto);	// in kết quả ra màn hình
					}
				}
			});
		}
	}
}
