using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace FtpClient
{
    class Program
    {
        public  static string rootDestDir = "F:/";
        public static string rootAddress = "ftp://ftp.ussg.iu.edu/";
        public static string currentAddress = "", prevAddress = "";

        static void Main(string[] args)
        {
            Program program = new Program();
            ConsoleKeyInfo key;
            prevAddress = currentAddress = rootAddress;
            Client client;
            while (true)
            {
                Console.Clear();

              
                client = new Client(currentAddress, "", "");

                List<FileDirectoryInfo> list = program.GetFilesAndDirectories(client);
                Console.WriteLine(client.URI);
                program.WriteFiles(list);

                program.ShowMenu();
                key = Console.ReadKey();

                if (key.Key == ConsoleKey.O)
                {
                    Console.WriteLine("\nEnter the directory name:");
                    String name = Console.ReadLine(); 
                    if (name.Equals("..."))
                    {
                        currentAddress = prevAddress;
                    }
                    else
                    {
                        prevAddress = client.URI;
                        currentAddress = client.URI + name + "/";
                    }
                }
                else if (key.Key == ConsoleKey.D)
                {
                    Console.WriteLine("\nEnter the file name:");
                    String name = Console.ReadLine();
                    client.DownloadFile(name, rootDestDir + name);
                }
                else if (key.Key == ConsoleKey.E)
                {
                    break;
                }
            }
        }

        public void ShowMenu()
        {
            Console.WriteLine("Press 'O' to open directory");
            Console.WriteLine("Press 'D' to download file");
            Console.WriteLine("Press 'E' to exit");
        }

        

        public void WriteFiles(List<FileDirectoryInfo> list)
        {
            foreach (FileDirectoryInfo fdi in list)
            {
                if (fdi.Type.Equals("DIR"))
                {
                    Console.Write(fdi.Name);
                    Console.WriteLine(" -DIR-");
                }
                else if (fdi.Type.Equals("FILE"))
                {
                    Console.Write(fdi.Name);
                    Console.WriteLine(" -FILE-");
                }
                else if(fdi.Type.Equals("DEFAULT"))
                {
                    Console.WriteLine(fdi.Name);
                }
            }
            Console.WriteLine();
        }

        public List<FileDirectoryInfo> GetFilesAndDirectories(Client client)
        {
            try
            {
                
                Regex regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(\d{1,})\s+(\w+\s+\d{1,2}\s+(?:\d{4})?)(\d{1,2}:\d{2})?\s+(.+?)\s?$",
                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

               
                List<FileDirectoryInfo> list = new List<FileDirectoryInfo>();
                foreach (string s in client.ListDirectoryDetails())
                {
                    Match match = regex.Match(s);
                    if (match.Length > 5)
                    {
                     
                        string type = match.Groups[1].Value == "d" ? "DIR" : "FILE";

                       
                        string size = "";
                        if (type == "FILE")
                            size = (Int32.Parse(match.Groups[3].Value.Trim()) / 1024).ToString() + " кБ";

                        list.Add(new FileDirectoryInfo(size, type, match.Groups[6].Value, match.Groups[4].Value, client.URI));
                    }
                }

               
                list.Insert(0, new FileDirectoryInfo("", "DEFAULT", "...", "", client.URI));

             
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + ": \n" + ex.Message);
            }
            return new List<FileDirectoryInfo>();
        }
    }

    public class Client
    {
        private string password;
        private string userName;
        private string uri;
        private int bufferSize = 1024;

        public bool Passive = true;
        public bool Binary = true;
        public bool EnableSsl = false;
        public bool Hash = false;

        public string URI
        {
            get { return uri; }
        }

        public Client(string uri, string userName, string password)
        {
            this.uri = uri;
            this.userName = userName;
            this.password = password;
        }

        public string ChangeWorkingDirectory(string path)
        {
            uri = combine(uri, path);

            return PrintWorkingDirectory();
        }



        public string DeleteFile(string fileName)
        {
            FtpWebRequest request = createRequest(combine(uri, fileName), WebRequestMethods.Ftp.DeleteFile);

            return getStatusDescription(request);
        }

        public string DownloadFile(string source, string dest)
        {
            Console.WriteLine("path: " + combine(uri, source));
            FtpWebRequest request = createRequest(combine(uri, source), WebRequestMethods.Ftp.DownloadFile);

            byte[] buffer = new byte[bufferSize];

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream stream = response.GetResponseStream();

            FileStream fs = new FileStream(dest, FileMode.OpenOrCreate);
                    
                        int readCount = stream.Read(buffer, 0, bufferSize);

                        while (readCount > 0)
                        {
                            if (Hash)
                                Console.Write("#");

                            fs.Write(buffer, 0, readCount);
                            readCount = stream.Read(buffer, 0, bufferSize);
                        }
                    
                

                return response.StatusDescription;
            
        }

        public DateTime GetDateTimestamp(string fileName)
        {
            FtpWebRequest request = createRequest(combine(uri, fileName), WebRequestMethods.Ftp.GetDateTimestamp);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            
                return response.LastModified;
            
        }

        public long GetFileSize(string fileName)
        {
            FtpWebRequest request = createRequest(combine(uri, fileName), WebRequestMethods.Ftp.GetFileSize);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            
                return response.ContentLength;
            
        }

        public string[] ListDirectory()
        {
            List<string> list = new List<string>();

            FtpWebRequest request = createRequest(WebRequestMethods.Ftp.ListDirectory);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream stream = response.GetResponseStream();

            StreamReader reader = new StreamReader(stream, true);
                    
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    
                
            

            return list.ToArray();
        }

        public string[] ListDirectoryDetails()
        {
            List<string> list = new List<string>();

            FtpWebRequest request = createRequest(WebRequestMethods.Ftp.ListDirectoryDetails);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream stream = response.GetResponseStream();

            StreamReader reader = new StreamReader(stream, true);
                    
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    
                
            

            return list.ToArray();
        }

        public string MakeDirectory(string directoryName)
        {
            FtpWebRequest request = createRequest(combine(uri, directoryName), WebRequestMethods.Ftp.MakeDirectory);

            return getStatusDescription(request);
        }

        public string PrintWorkingDirectory()
        {
            FtpWebRequest request = createRequest(WebRequestMethods.Ftp.PrintWorkingDirectory);

            return getStatusDescription(request);
        }

        public string RemoveDirectory(string directoryName)
        {
            FtpWebRequest request = createRequest(combine(uri, directoryName), WebRequestMethods.Ftp.RemoveDirectory);

            return getStatusDescription(request);
        }

        public string Rename(string currentName, string newName)
        {
            FtpWebRequest request = createRequest(combine(uri, currentName), WebRequestMethods.Ftp.Rename);

            request.RenameTo = newName;

            return getStatusDescription(request);
        }

        public string UploadFile(string source, string destination)
        {
            FtpWebRequest request = createRequest(combine(uri, destination), WebRequestMethods.Ftp.UploadFile);

            Stream stream = request.GetRequestStream();
            
                FileStream fileStream = System.IO.File.Open(source, FileMode.Open);
               
                    int num;

                    byte[] buffer = new byte[bufferSize];

                    while ((num = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (Hash)
                            Console.Write("#");

                        stream.Write(buffer, 0, num);
                    }
                
            

            return getStatusDescription(request);
        }

        public string UploadFileWithUniqueName(string source)
        {
            FtpWebRequest request = createRequest(WebRequestMethods.Ftp.UploadFileWithUniqueName);

            Stream stream = request.GetRequestStream();
            
                FileStream fileStream = System.IO.File.Open(source, FileMode.Open);
                
                    int num;

                    byte[] buffer = new byte[bufferSize];

                    while ((num = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (Hash)
                            Console.Write("#");

                        stream.Write(buffer, 0, num);
                    }



            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            
                return Path.GetFileName(response.ResponseUri.ToString());
            
        }

        private FtpWebRequest createRequest(string method)
        {
            return createRequest(uri, method);
        }

        private FtpWebRequest createRequest(string uri, string method)
        {
            FtpWebRequest r = (FtpWebRequest)WebRequest.Create(uri);

            r.Credentials = new NetworkCredential(userName, password);
            r.Method = method;
            r.UseBinary = Binary;
            r.EnableSsl = EnableSsl;
            r.UsePassive = Passive;

            return r;
        }

        private string getStatusDescription(FtpWebRequest request)
        {
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            
                return response.StatusDescription;
            
        }

        private string combine(string path1, string path2)
        {
            return Path.Combine(path1, path2).Replace("\\", "/");
        }
    }

    public class FileDirectoryInfo
    {
        public string fileSize;
        public string type;
        public string name;
        public string date;
        public string adress;

        public string FileSize
        {
            get { return fileSize; }
            set { fileSize = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Date
        {
            get { return date; }
            set { date = value; }
        }

        public FileDirectoryInfo() { }

        public FileDirectoryInfo(string fileSize, string type, string name, string date, string adress)
        {
            FileSize = fileSize;
            Type = type;
            Name = name;
            Date = date;
            this.adress = adress;
        }

    }
}
