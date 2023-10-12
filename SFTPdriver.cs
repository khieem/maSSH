// using System.Collections.Concurrent;
// using System.Diagnostics;
// using Renci.SshNet;

// class Program
// {
   
//    static void Main()
//    {
//     string host = @"127.0.0.1";
//     string username = @"khiemvn";
//     string password = @"khiemvn";

//     string username1 = @"root";
//     string password1 = @"root";

//     string remoteDirectory = "/home";

//     List<SftpClient> sftps = new();
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username1, password1));
//     sftps.Add(new SftpClient(host, username1, password1));
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username1, password1));
//     sftps.Add(new SftpClient(host, username1, password1));
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username, password));
//     sftps.Add(new SftpClient(host, username1, password1));
//     sftps.Add(new SftpClient(host, username1, password1));
//    var ops = new ParallelOptions {MaxDegreeOfParallelism = 16};
   
//    long avg = 0;

//    Parallel.ForEach(sftps, ops, sftp => 
//    {
//       string thread = $"Thread {Thread.CurrentThread.ManagedThreadId}";
   
//             sftp.Connect();

//             var files = sftp.ListDirectory(remoteDirectory);

//             // foreach (var file in files)
//             // {
//             //     Console.WriteLine(file.Name);
//             // }
//             // using FileStream fs = new FileStream("ftptest", FileMode.Open);
//             // sftp.UploadFile(fs, "ftptest");
//             // sftp.DownloadFile()
//             Console.WriteLine("thread " + Thread.CurrentThread.ManagedThreadId + " is sleeping.");
//             Thread.Sleep(5000);
//             sftp.Disconnect();
//    });

//       }

// }