//===================================================================
// JPG Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the BSD License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
using System;
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
        private String _selectedImage;
        private List<String> _wordList;
        private Image _offscreenBitmap = null;
        private static Mutex _bytesMutex = new Mutex();
        private byte[] _bytes = null;

        public JPGCorruptForm()
        {
            InitializeComponent();

            toolStripLabelText.Text = "";
            _selectedImage = toolStripLabelImage.Text = "";

            this.DoubleBuffered = true;
            this.Invalidate();
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
                return (this.FormBorderStyle == FormBorderStyle.None);
            }

            set
            {
                if (value)
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    //and then to exit:
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;
                }
                toolStrip.Visible = statusStrip.Visible = !value; 
            }
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
            FileInfo file = new FileInfo(fileName);
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
            {
                int len = (int)stream.Length;
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, len);
                stream.Close();
                return bytes;
            }
        }

        /// <summary>
        /// Corrupts an image in memory by overwriting data with corruptText at a random place
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="corruptText"></param>
        private void CorruptImageBytesRandom(byte[] bytes, String corruptText)
        {
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

                //System.Diagnostics.Debug.WriteLine("CorruptImageFile: IOException. Sleeping..."); 
                Thread.Sleep(250);
                //System.Diagnostics.Debug.WriteLine("CorruptImageFile: IOException. Awake...");
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
        /// Given a text file, return a list of words.
        /// </summary>
        /// <param name="file"></param>
        private List<String> GetWords(String file)
        {
            List<String> list = null;
            FileInfo txtFile = new FileInfo(file);
            
            using (TextReader rdr = txtFile.OpenText())
            {
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
            int n = 1;

            FileCorruptBackgroundWorkerUserState UserState = new FileCorruptBackgroundWorkerUserState();
            foreach (string word in _wordList)
            {
                if (FileCorruptBackgroundWorker.CancellationPending)
                    return;

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
                UserState.Bitmap = PaintBitmap(_bytes);
                UserState.Word = word;
                FileCorruptBackgroundWorker.ReportProgress((int)((double)n * 100 / _wordList.Count), UserState);

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
            try
            {
                _bytesMutex.WaitOne();
                using (Image newImage = Image.FromStream(new MemoryStream(_bytes)))
                {
                    // Figure out actual drawing area
                    Rectangle DrawArea = GetDrawRectangle();

                    // Scale to fill height
                    Rectangle ImageArea = GetImageRectangle(newImage);

                    Image bitmapImage = new Bitmap(ImageArea.Width, ImageArea.Height);
                    using (Graphics g = Graphics.FromImage(bitmapImage))
                    {
                        g.DrawImage(newImage, ImageArea);
                    }
                    return bitmapImage;
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("paint: exception");
            }
            finally
            {
                _bytesMutex.ReleaseMutex();
            }
            return null;
        }

        /// <summary>
        /// Called by the background worker each time a corrupted image is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileCorruptBackgroundWorkerUserState state = e.UserState as FileCorruptBackgroundWorkerUserState;

            _offscreenBitmap = state.Bitmap;
            toolStripLabelCurrent.Text = state.Word;
            if (!toolStripProgressBar.IsDisposed)
                toolStripProgressBar.Value = e.ProgressPercentage;

            this.Invalidate();
        }


        private void FileCorruptBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                FullScreen = false;

            Running = false;
        }

        // ==========================================================
        // Drawing
        // ==========================================================
        private void JPGCorruptForm_Paint(object sender, PaintEventArgs e)
        {
            if (!String.IsNullOrEmpty(_selectedImage))
            {
                // Figure out actual drawing area
                Rectangle DrawArea = GetDrawRectangle();
                // Scale to fill height
                Rectangle ImageArea = GetImageRectangle(_offscreenBitmap);
                // Offset to account for client area
                ImageArea.Offset(DrawArea.X + ((ClientRectangle.Width - ImageArea.Width) / 2), DrawArea.Y);
                try
                {
                    e.Graphics.DrawImage(_offscreenBitmap, ImageArea);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                Font drawFont = new Font("Arial", 16);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                PointF drawPoint = new PointF(10F, 30F);
                // Center text
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(toolStripLabelCurrent.Text, drawFont, drawBrush, DrawArea, stringFormat);
            }
        }

        /// <summary>
        /// Get the rectangle representing the client area.
        /// </summary>
        /// <returns></returns>
        private Rectangle GetDrawRectangle()
        {
            return new Rectangle(0,
                toolStrip.Visible ? toolStrip.Height : 0,
                ClientRectangle.Width - 1,
                ClientRectangle.Height -
                    ((toolStrip.Visible ? toolStrip.Height : 0) + (statusStrip.Visible ? statusStrip.Height : 0)));
        }

        /// <summary>
        /// Scale the Image area to the window client.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private Rectangle GetImageRectangle(Image image)
        {
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
            if (!Running)
            {
                if (!String.IsNullOrEmpty(_selectedImage) && (_wordList != null))
                {
                    try
                    {
                        Running = true;
                        FileCorruptBackgroundWorker.RunWorkerAsync();
                    }
                    catch (FileNotFoundException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void toolStripButtonGoFullscreen_Click(object sender, EventArgs e)
        {
            FullScreen = true;
            toolStripButtonGo_Click(sender, e);
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            if (Running)
            {
                FileCorruptBackgroundWorker.CancelAsync();
            }
        }

        private void toolStripButtonChooseText_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = ".";
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileCorruptBackgroundWorker.CancelAsync();

                try
                {
                    if ((myStream = openFileDialog.OpenFile()) != null)
                    {
                        _wordList = GetWords(openFileDialog.FileName);
                        toolStripLabelText.Text = "Text: " + openFileDialog.FileName;
                    }
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
                FileCorruptBackgroundWorker.CancelAsync();

                try
                {
                    if ((myStream = openFileDialog.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            _selectedImage = openFileDialog.FileName;
                        }
                        _bytesMutex.WaitOne();
                        _bytes = GetFileBytes(_selectedImage);
                        _bytesMutex.ReleaseMutex();

                        _offscreenBitmap = PaintBitmap(_bytes);
                        this.Invalidate();

                        toolStripLabelImage.Text = "Image: " + _selectedImage;
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
            SaveFileDialog openFileDialog = new SaveFileDialog();

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
            FileCorruptBackgroundWorker.CancelAsync();
        }

        private void JPGCorruptForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                if (Running)
                    FileCorruptBackgroundWorker.CancelAsync();
                else
                    FullScreen = false;
            }
        }

        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Copyright © 2012 Charlie Kindel.\r\nSource on http://github.com/tig/JPG-Corruptor", "JPG Corruptor");
        }

        private void JPGCorruptForm_SizeChanged(object sender, EventArgs e)
        {
            if (!Running)
                _offscreenBitmap = PaintBitmap(_bytes);

            Invalidate();
        }

    } // class
} // namespace
