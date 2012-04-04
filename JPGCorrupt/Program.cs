//===================================================================
// JPG Corruptor http://tig.github.com/JPG-Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the MIT License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace JPGCorrupt
{
    static class Program
    {
        private static int _MAXLOGFILESIZE = 1024 * 500;

        public static String AppName()
        {
            return Application.ProductName;
            //return Path.GetFileNameWithoutExtension(Application.ExecutablePath);
        }

        public static String ExecutablePath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            String logfile = Program.ExecutablePath() + "\\" + Program.AppName() + ".log";
            FileStreamWithBackup fs = new FileStreamWithBackup(logfile, _MAXLOGFILESIZE, 10, FileMode.Append);
            fs.CanSplitData = false;
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(fs);

            Trace.Listeners.Add(listener);
            Trace.AutoFlush = true;
            Trace.WriteLine(String.Format("JPG Corruptor V{0} - Started", Application.ProductVersion), "START");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new JPGCorruptForm());

            Trace.WriteLine("Stopped {0}", "STOPPED");
            Trace.Flush();
            listener.Close();
        }
    }
}
