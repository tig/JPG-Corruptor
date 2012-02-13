//===================================================================
// JPG Corruptor http://tig.github.com/JPG-Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the MIT License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace JPGCorrupt
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new JPGCorruptForm());
        }
    }
}
