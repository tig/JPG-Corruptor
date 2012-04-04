//===================================================================
// JPG Corruptor http://tig.github.com/JPG-Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the MIT License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace JPGCorrupt
{
    public partial class JPGCorruptForm : Form
    {
        // ==========================================================
        // Constants
        // ==========================================================
        private static int _JPGHEADERSIZE = 256;     // Number of bytes to skip at head of file.
        private static int _RANDOMSEED = 42;        // Hard coded so results can be reproduced.
        private static int _SEQUENTIALGAP = 1024*2;
        private static Color _BACKGROUNDCOLOR = Color.Black;

        // ==========================================================
        // Types
        // ==========================================================
        // BUGBUG: Cannot get sequential to work reliably without completely corrupting image.
        //         Leaving code in just in case I figure something out.
        private enum CorruptionMode
        {
            Sequential,   
            Random
        }

        // ==========================================================
        // Class variables 
        // ==========================================================
        private CorruptionMode _corruptionMode = CorruptionMode.Random;
        private Random _randomNum = new Random(_RANDOMSEED);
        private int _location = 0;
        private List<String> _wordList;
        private Image _offscreenBitmap = null;
        private static Mutex _bytesMutex = new Mutex();
        private byte[] _bytes = null;

        private Settings _settings = null;
        private String _settingsFileName = null;

        /// <summary>
        /// Holds a queue of text & image filename pairs. Used by Go() to iterate
        /// through the files found in the app settings.
        /// </summary>
        Queue<TextImagePair> _queue;

        public JPGCorruptForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Invalidate();

            _settingsFileName = Program.ExecutablePath() + "\\" + Program.AppName() + ".settings";

            toolStripLabelCurrent.Text = "";

            // Retrieve settings
            try
            {
                _settings = Settings.DeserializeFromXML(_settingsFileName);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failure reading settings file: " + ex.ToString(), "ERROR");
                MessageBox.Show("Failure reading settings file: " + ex.Message);
            }

            Loop = _settings.Loop;
            Trace.WriteLine(String.Format("Loop = {0}", Loop.ToString()));

            try
            {
                // These two properties actually try to load the file data
                Trace.WriteLine(String.Format("Trying _settings.TextFile: {0}", _settings.TextFile));
                CurrentTextFile = _settings.TextFile;
                Trace.WriteLine(String.Format("Trying _settings.ImageFile: {0}", _settings.ImageFile));
                CurrentImageFile = _settings.ImageFile;
            }
            catch (FileNotFoundException)
            {
                Trace.WriteLine("File not found. Ignoring exception");
                // Ignore if the files aren't found during app load.
            }

            if (_settings.AutoStart)
            {
                FullScreen = _settings.FullScreen;
                Trace.WriteLine(String.Format("FullScreen = {0}", FullScreen.ToString()));
                Go();
            }
        }

        private bool _running = false;
        /// <summary>
        /// Tracks whether the corruption process is running.
        /// </summary>
        private bool Running
        {
            get
            {
                return _running;
            }

            set
            {
                _running = value;
                Trace.WriteLine(String.Format("Running = {0}", _running.ToString()));
                toolStripButtonStop.Enabled = value;
                toolStripButtonGoFullscreen.Enabled = !value;
                toolStripButtonGo.Enabled = !value;
                toolStripButtonChooseImage.Enabled = !value;
                toolStripButtonChooseText.Enabled = !value;
                if (!value)
                {
                    toolStripLabelCurrent.Text = "Not running";
                    if (!toolStripProgressBar.IsDisposed)
                        toolStripProgressBar.Value = 0;
                }
            }
        }

        /// <summary>
        /// Tracks whether we are running full screen or not
        /// </summary>
        private bool FullScreen
        {
            get
            {
                return (this.WindowState == FormWindowState.Maximized);
            }

            set
            {
                Trace.WriteLine(String.Format("FullScreen = {0}", value.ToString()));
                if (value)
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    //and then to exit:
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    if (this.WindowState == FormWindowState.Maximized)
                        this.WindowState = FormWindowState.Normal;
                }
                toolStrip.Visible = statusStrip.Visible = !value; 
            }
        }

        /// <summary>
        /// Loop mode
        /// </summary>
        private bool Loop 
        { 
            get
            {
                return this.toolStripButtonLoop.Checked;
            }
            set
            {
                this.toolStripButtonLoop.Checked = value;
            }
        }

        private String _currentTextFile;
        public String CurrentTextFile
        {
            get { return _currentTextFile; }
            set 
            { 
                // GetWords will throw an exception if file not found
                Trace.WriteLine(String.Format("CurrentTextFile = {0}", value.ToString()));
                _wordList = GetWords(value);
                _currentTextFile = value;
                toolStripLabelText.Text = "Text: " + Path.GetFileName(_currentTextFile);
            }
        }

        private String _currentImageFile;
        public String CurrentImageFile
        {
            get { return _currentImageFile; }
            set 
            {
                Trace.WriteLine(String.Format("CurrentImageFile = {0}", value.ToString()));
                _currentImageFile = value;

                _bytesMutex.WaitOne();
                _bytes = GetFileBytes(_currentImageFile);
                _bytesMutex.ReleaseMutex();

                _offscreenBitmap = PaintBitmap(_bytes);

                toolStripLabelImage.Text = "Image: " + Path.GetFileName(_currentImageFile);
                this.Invalidate();
            }
        }

        /// <summary>
        /// Set when the stop button is pushed, ESC is pressed, or the app is closing.
        /// Reset when the background worker completes.
        /// </summary>
        public bool StopPushed { get; set; }

        /// <summary>
        /// Starts a corruption session
        /// </summary>
        private void Go()
        {
            if (!Running)
            {
                Trace.WriteLine("Go()");
                // First time through, setup the Queue. Note that there should ALWAYS be
                // at least one element in _settings.list.
                if (_queue == null || _queue.Count == 0)
                {
                    Trace.WriteLine("Creating _queue");
                    _queue = new Queue<TextImagePair>();
                    foreach (TextImagePair p in _settings.list)
                    {
                        Trace.WriteLine(String.Format("  Enqueue {0}, {1}", p.ImageFile, p.TextFile));
                        _queue.Enqueue(p);
                    }
                }

                TextImagePair pair = _queue.Dequeue();
                Trace.WriteLine(String.Format("Dequeued pair: {0}, {1}", pair.ImageFile, pair.TextFile));
                try
                {
                    Running = true;

                    // Read the files
                    CurrentTextFile = pair.TextFile;
                    CurrentImageFile = pair.ImageFile;

                    FileCorruptBackgroundWorker.RunWorkerAsync();
                }
                catch (FileNotFoundException ex)
                {
                    Trace.WriteLine(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
            }
            else 
                Trace.WriteLine("Go called while already running. Doing nothing.");

        }

        /// <summary>
        /// Stop a corruption session
        /// </summary>
        private void Stop()
        {
            Trace.WriteLine("Stop()");
            StopPushed = true;
            FileCorruptBackgroundWorker.CancelAsync();
        }

        // ==========================================================
        // Image Corruption Code
        // ==========================================================
        /// <summary>
        /// Returns an array of bytes with the contents of a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private byte[] GetFileBytes(String fileName)
        {
            Trace.WriteLine("GetFileBytes(): " + fileName);
            if (String.IsNullOrEmpty(fileName))
                return null;

            FileInfo file = new FileInfo(fileName);
            byte[] bytes = null;

            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
                {
                    int len = (int)stream.Length;
                    Trace.WriteLine(String.Format("File opened: {0} bytes length", len));
                    bytes = new byte[len];
                    stream.Read(bytes, 0, len);
                    stream.Close();
                    Trace.WriteLine("File read");
                }
            }
            catch (FileNotFoundException ex)
            {
                Trace.WriteLine(String.Format("File not found exception: {0} {1}", file, ex), "ERROR");
                MessageBox.Show("File not found: " + file);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("File exception: {0} {1}", file, ex), "ERROR");
                MessageBox.Show(ex.Message + file);
            }
            return bytes;
        }

        /// <summary>
        /// Corrupts an image in memory by overwriting data with corruptText at a random place
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="corruptText"></param>
        private void CorruptImageBytesRandom(byte[] bytes, String corruptText)
        {
            //Trace.WriteLine("CorruptImageBytesRandom()");
            byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);
            int location = 0;

            // Avoid overwriting anything in the first _JPGHEADERSIZE bytes of the file
            // in order to reduce the chance of making the JPG completely unreadable. 
            location = _randomNum.Next(_JPGHEADERSIZE, bytes.Length - corruptTextBytes.Length);

            // Copy in corrupt text
            _bytesMutex.WaitOne();
            System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, location, corruptTextBytes.Length);
            _bytesMutex.ReleaseMutex();
        }

        /// <summary>
        /// Corrupts an image in memory by overwriting data with corruptText at a random place
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="corruptText"></param>
        private void CorruptImageBytesSequential(byte[] bytes, String corruptText)
        {
            Trace.WriteLine("CorruptImageBytesSequential()");
            byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);

            if (_location > bytes.Length - corruptTextBytes.Length)
                return;

            // Copy in corrupt text
            _bytesMutex.WaitOne();
            System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, _location, corruptTextBytes.Length);
            _bytesMutex.ReleaseMutex();
            _location += corruptTextBytes.Length + _SEQUENTIALGAP;
        }


#if CORRUPTFILE
        /// <summary>
        /// Corrupts a image file by inserting corruptText at a random place
        /// within the image data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CorruptImageFile(String fileName, String corruptText)
        {
            FileInfo file = new FileInfo(fileName);
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);
                    byte[] bytes = null;

                    int len = (int)stream.Length;
                    //  bytes = new byte[len + corruptText.Length];
                    //  stream.Read(bytes, 0, len);
                    //  int rnd = _randomNum.Next(256, len);
                    //  // Shift original data
                    //  System.Buffer.BlockCopy(bytes, rnd, bytes, rnd + corruptTextBytes.Length, corruptTextBytes.Length);
                    //  // Copy in corrupt text
                    //  System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, rnd, corruptTextBytes.Length);

                    bytes = new byte[len];
                    stream.Read(bytes, 0, len);

                    int rnd = _randomNum.Next(_JPGHEADERSIZE, len - corruptTextBytes.Length);

                    // Copy in corrupt text
                    System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, rnd, corruptTextBytes.Length);

                    stream.Seek(0, 0);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }
            }
            catch (IOException)
            {
                if (FileCorruptBackgroundWorker.CancellationPending)
                    return;

                //System.Diagnostics.Trace.WriteLine("CorruptImageFile: IOException. Sleeping..."); 
                Thread.Sleep(250);
                //System.Diagnostics.Trace.WriteLine("CorruptImageFile: IOException. Awake...");
                CorruptImageFile(fileName, corruptText);
            }
        }
#endif

#if CORRUPTBITMAP
        /// <summary>
        /// Corrupts a Bitmap in memory by randomly inserting corruptText
        /// into the RGB data
        /// </summary>
        /// <param name="image"></param>
        /// <param name="corruptText"></param>
        private void CorruptBitmap(Bitmap image, String corruptText)
        {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, image.PixelFormat);

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * image.Height;
            byte[] rgbValues = new byte[bytes];

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);

            int i = 0;
            int rnd = _randomNum.Next(bytes - corruptTextBytes.Length);
            foreach (byte b in corruptTextBytes)
            {
                rgbValues[rnd + i] = b;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            image.UnlockBits(bmpData);
        }
#endif

        /// <summary>
        /// Given a text file, return a list of words. Will throw an exception if 
        /// file is not found etc...
        /// </summary>
        /// <param name="file"></param>
        private List<String> GetWords(String file)
        {
            Trace.WriteLine("GetWords(): " + file);
            if (String.IsNullOrEmpty(file))
                return null;

            List<String> list = null;
            FileInfo txtFile = new FileInfo(file);

            try
            {
                using (TextReader rdr = txtFile.OpenText())
                {
                    Trace.WriteLine("Reading file...");
                    // TODO: Make this return only words, no punctuation
                    list = new List<string>();
                    String line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        foreach (string word in line.Split(' '))
                        {
                            list.Add(word);
                        }
                    }
                }
                Trace.WriteLine("File read.");
            }
            catch (FileNotFoundException ex)
            {
                Trace.WriteLine(String.Format("File not found exception: {0} {1}", file, ex), "ERROR");
                MessageBox.Show("File not found: " + file);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("File exception: {0} {1}", file, ex), "ERROR");
                MessageBox.Show(ex.Message + file);
            }

            return list;
        }

        // ==========================================================
        // Background Worker
        // ==========================================================
        private class FileCorruptBackgroundWorkerUserState
        {
            public Image Bitmap { get; set;}
            public String Word { get; set; }
        }

        /// <summary>
        /// Worker method for the background worker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Trace.WriteLine("FileCorruptBackgroundWorker_DoWork()");
            FileCorruptBackgroundWorkerUserState UserState = new FileCorruptBackgroundWorkerUserState();

            int n = 1;
            foreach (string word in _wordList)
            {
                if (FileCorruptBackgroundWorker.CancellationPending)
                {
                    Trace.WriteLine("FileCorruptBackgroundWorker.CancellationPending");
                    return;
                }

                //Trace.WriteLine("_corruptionMode: " + _corruptionMode.ToString());
                switch (_corruptionMode)
                {
                    case CorruptionMode.Random:
                        // This will block if needed
                        CorruptImageBytesRandom(_bytes, word);
                        break;
                    case CorruptionMode.Sequential:
                        // This will block if needed
                        CorruptImageBytesSequential(_bytes, word);
                        break;

                }

                // Render to a bitmap for fast blitting
                //Trace.WriteLine("Render to bitmap...");
                UserState.Bitmap = PaintBitmap(_bytes);
                UserState.Word = word;
                int percent = (int)((double)n * 100 / _wordList.Count);
                FileCorruptBackgroundWorker.ReportProgress(percent, UserState);

                n++;
            }
        }

        /// <summary>
        /// Paints our JPG image onto an (offscreen) bitmap sized for the current client area. 
        /// Since the scaling of a JPG can take a while, while running this is called on the
        /// worker thread. This way the UI stays responsive and most corrupt images get displayed.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private Image PaintBitmap(byte[] bytes)
        {
            //Trace.WriteLine("PaintBitmap()");
            Image bitmapImage = null;
            try
            {
                _bytesMutex.WaitOne();
                // If we are minimized or sized too small just ignore
                Rectangle r = GetDrawRectangle();
                if (r.Height > 0 && r.Width > 0)
                {
                    //Trace.WriteLine("calling Image.FromStream");
                    using (Image newImage = Image.FromStream(new MemoryStream(_bytes)))
                    {
                        // Scale to fill height
                        Rectangle ImageArea = GetImageRectangle(newImage);
                        bitmapImage = new Bitmap(ImageArea.Width, ImageArea.Height);
                        using (Graphics g = Graphics.FromImage(bitmapImage))
                        {
                            g.DrawImage(newImage, ImageArea);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("PaintBitmap() exception: " + ex.ToString(), "ERROR");
            }
            finally
            {
                _bytesMutex.ReleaseMutex();
            }
            return bitmapImage;
        }

        /// <summary>
        /// Called by the background worker each time a corrupted image is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Trace.WriteLine("FileCorruptBackgroundWorker_ProgressChanged()");
            FileCorruptBackgroundWorkerUserState state = e.UserState as FileCorruptBackgroundWorkerUserState;

            _offscreenBitmap = state.Bitmap;
            //Trace.Assert(_offscreenBitmap != null);
            toolStripLabelCurrent.Text = state.Word;
            if (!toolStripProgressBar.IsDisposed)
            {
                if (toolStripProgressBar.Value != e.ProgressPercentage)
                    Trace.WriteLine(e.ProgressPercentage + "% complete");

                toolStripProgressBar.Value = e.ProgressPercentage;
            }

            //Trace.WriteLine("Calling Invalidate");
            this.Invalidate();
        }

        /// <summary>
        /// Called by the background worker upon completion or cancellation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Trace.WriteLine("FileCorruptBackgroundWorker_RunWorkerCompleted()");
            Running = false;

            // if it wasn't cancelled and Loop mode is enabled (or there are items in the queue)
            // then go again
            if (!StopPushed && (this.toolStripButtonLoop.Checked || _queue.Count > 0))
            {
                Trace.WriteLine("Going again...");
                Go();
            }
            else
            {
                Trace.WriteLine("Done.");
                // Always come out of full sceen mode if cancelled
                FullScreen = false;
                StopPushed = false;
            }
        }

        // ==========================================================
        // Drawing
        // ==========================================================
        private void JPGCorruptForm_Paint(object sender, PaintEventArgs e)
        {
            //Trace.WriteLine("JPGCorruptForm_Paint()");
            if (_bytes != null && _offscreenBitmap != null)
            {
                // Figure out actual drawing area
                Rectangle DrawArea = GetDrawRectangle();

                SolidBrush bgBrush = new SolidBrush(_BACKGROUNDCOLOR);
                e.Graphics.FillRectangle(bgBrush, DrawArea);

                // Scale to fill height
                Trace.Assert(_offscreenBitmap != null);
                Rectangle ImageArea = GetImageRectangle(_offscreenBitmap);
                // Offset to account for client area
                ImageArea.Offset(DrawArea.X + ((ClientRectangle.Width - ImageArea.Width) / 2), DrawArea.Y);
                try
                {
                    e.Graphics.DrawImage(_offscreenBitmap, ImageArea);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }

                //Font drawFont = new Font("Arial", 16);
                //SolidBrush drawBrush = new SolidBrush(Color.Black);
                //PointF drawPoint = new PointF(10F, 30F);
                //// Center text
                //StringFormat stringFormat = new StringFormat();
                //stringFormat.Alignment = StringAlignment.Center;
                //stringFormat.LineAlignment = StringAlignment.Center;
                //e.Graphics.DrawString(toolStripLabelCurrent.Text, drawFont, drawBrush, DrawArea, stringFormat);
            }
        }

        /// <summary>
        /// Get the rectangle representing the client area.
        /// </summary>
        /// <returns></returns>
        private Rectangle GetDrawRectangle()
        {
            //Trace.WriteLine("GetDrawRectangle()");
            Rectangle r = new Rectangle(0,
                toolStrip.Visible ? toolStrip.Height : 0,
                ClientRectangle.Width,
                ClientRectangle.Height -
                    ((toolStrip.Visible ? toolStrip.Height : 0) + (statusStrip.Visible ? statusStrip.Height : 0)));
            return r;
        }

        /// <summary>
        /// Scale the Image area to the window client.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private Rectangle GetImageRectangle(Image image)
        {
            //Trace.WriteLine("GetImageRectangle()");
            Rectangle DrawRectangle = GetDrawRectangle();
            if (image.Height > DrawRectangle.Height)
            {
                // what percentage is this.Height of newImage.Height?
                double percent = (double)DrawRectangle.Height / image.Height;
                return new Rectangle(0, 0, (int)((double)image.Width * percent), DrawRectangle.Height);
            }
            else
                return new Rectangle(0, 0, image.Width, image.Height);
        }

        // ==========================================================
        // Menu & Toolbar Event Handlers
        // ==========================================================
        private void toolStripButtonGo_Click(object sender, EventArgs e)
        {
            Go();
        }

        private void toolStripButtonGoFullscreen_Click(object sender, EventArgs e)
        {
            FullScreen = true;
            Go();
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            if (Running)
                Stop();
        }

        private void toolStripButtonChooseText_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = ".";
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Stop();

                try
                {
                    // CurrentTextFile.Set will attempt to open the file and exception willl be thrown
                    // if not found.
                    CurrentTextFile = openFileDialog.FileName;
                    _settings.TextFile = openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void toolStripButtonChooseImage_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = ".";
            openFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Cancel the background worker
                Stop();

                try
                {
                    if ((myStream = openFileDialog.OpenFile()) != null)
                    {
                        String file;
                        using (myStream)
                        {
                            file = openFileDialog.FileName;
                        }

                        CurrentImageFile = file;
                        _settings.ImageFile = CurrentImageFile;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            saveFileDialog.InitialDirectory = ".";
            saveFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = false;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileInfo file = new FileInfo(saveFileDialog.FileName);
                try
                {
                    using (FileStream stream = file.Open(FileMode.Create, FileAccess.Write))
                    {
                        _bytesMutex.WaitOne();
                        stream.Write(_bytes, 0, _bytes.Length);
                        _bytesMutex.ReleaseMutex();
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not save file to disk. Original error: " + ex.Message);
                }
            }

        }

        private void JPGCorruptForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();

            Settings.SerializeToXML(_settingsFileName, _settings);
        }

        private void JPGCorruptForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                if (Running)
                    Stop();
                else
                    FullScreen = false;
            }
        }

        private void JPGCorruptForm_SizeChanged(object sender, EventArgs e)
        {
            Trace.WriteLine("JPGCorruptForm_SizeChanged");
            if (!Running)
                _offscreenBitmap = PaintBitmap(_bytes);

            Trace.WriteLine("Calling Invalidate");
            Invalidate();
        }

        private void toolStripButtonLoop_Click(object sender, EventArgs e)
        {
            this.toolStripButtonLoop.Checked = !this.toolStripButtonLoop.Checked;
            _settings.Loop = this.toolStripButtonLoop.Checked;
        }

        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version " + Application.ProductVersion + "\r\nCopyright © 2012 Charlie Kindel.\r\nSource on http://github.com/tig/JPG-Corruptor", "JPG Corruptor");
        }

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand,
            Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = m.WParam.ToInt32() & 0xfff0;
                    if (command == SC_MINIMIZE)
                    {
                        Stop();  
                    }
                    break;
            }
            base.WndProc(ref m);
        }

    } // class
} // namespace
