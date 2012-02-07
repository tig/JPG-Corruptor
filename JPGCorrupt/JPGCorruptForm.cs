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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;

namespace JPGCorrupt
{
    public partial class JPGCorruptForm : Form
    {
        // ==========================================================
        // Class variables 
        // ==========================================================
        private Random RandomNum = new Random(42);
        private String SelectedImage;
        private String CurrentImage;
        private List<String> WordList;
        private String OutputDir = "";

        public JPGCorruptForm()
        {
            InitializeComponent();

            toolStripLabelText.Text = "";
            SelectedImage = toolStripLabelImage.Text = "";

            this.DoubleBuffered = true;
            this.Invalidate();
            this.Update();
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
                if (!value)
                {
                    toolStripLabelCurrent.Text = "Not running";
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
                    //if (false)
                    //{
                    //    bytes = new byte[len + corruptText.Length];
                    //    stream.Read(bytes, 0, len);

                    //    // Skip the first 256 bytes 
                    //    // TODO: Test whether this is really needed or not. Or if it can be a smaller
                    //    // number.
                    //    int rnd = RandomNum.Next(256, len);

                    //    // Shift original data
                    //    System.Buffer.BlockCopy(bytes, rnd, bytes, rnd + corruptTextBytes.Length, corruptTextBytes.Length);

                    //    // Copy in corrupt text
                    //    System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, rnd, corruptTextBytes.Length);
                    //}
                    //else
                    {
                        bytes = new byte[len];
                        stream.Read(bytes, 0, len);

                        int rnd = RandomNum.Next(256, len - corruptTextBytes.Length);

                        // Copy in corrupt text
                        System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, rnd, corruptTextBytes.Length);
                    }

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

        /// <summary>
        /// Corrupts a Bitmap in memory by randomly inserting corruptText
        /// into the RGB data
        /// </summary>
        /// <param name="image"></param>
        /// <param name="corruptText"></param>
        private void CorruptImage(Bitmap image, String corruptText)
        {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData =
                image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                image.PixelFormat);

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * image.Height;
            byte[] rgbValues = new byte[bytes];

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);

            int i = 0;
            int rnd = RandomNum.Next(bytes - corruptTextBytes.Length);
            foreach (byte b in corruptTextBytes)
            {
                rgbValues[rnd + i] = b;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            image.UnlockBits(bmpData);
        }

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
            public String Image { get; set;}
            public String Word { get; set; }
        }

        /// <summary>
        /// Called by the background worker each time a corrupted image is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileCorruptBackgroundWorkerUserState state = e.UserState as FileCorruptBackgroundWorkerUserState;

            toolStripLabelCurrent.Text = state.Word;
            toolStripProgressBar.Value = e.ProgressPercentage;

            this.Invalidate();
        }


        /// <summary>
        /// Worker method for the background worker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            String file = OutputDir + "Corrupt 0000.jpg";
            System.IO.File.Copy(SelectedImage, file, true);

            ProcessWords(file);
            //ProcessWordsKeepFiles(file);
        }

        /// <summary>
        /// Process words without creating a file for each
        /// </summary>
        /// <param name="StartFileName"></param>
        private void ProcessWords(String StartFileName)
        {
            int n = 1;

            FileCorruptBackgroundWorkerUserState UserState = new FileCorruptBackgroundWorkerUserState();
            foreach (string word in WordList)
            {
                if (FileCorruptBackgroundWorker.CancellationPending)
                    return;
                // This reads then writes to the file
                CorruptImageFile(StartFileName, word);
                //System.Threading.Thread.Sleep(500);

                UserState.Image = StartFileName;
                UserState.Word = word;
                CurrentImage = StartFileName;
                FileCorruptBackgroundWorker.ReportProgress((int)((double)n * 100 / WordList.Count), UserState);

                // Block until 
                while (!String.IsNullOrEmpty(CurrentImage))
                {
                    if (FileCorruptBackgroundWorker.CancellationPending)
                        return;
                    //System.Diagnostics.Debug.WriteLine("ProcessWords: waiting");
                }
                n++;
            }

        }

        /// <summary>
        /// Process words creating a new file for each word
        /// </summary>
        /// <param name="StartFileName"></param>
        private void ProcessWordsKeepFiles(String StartFileName)
        {
            String FileName = StartFileName;
            String nextFileName;
            int fileNum = 0;

            FileCorruptBackgroundWorkerUserState UserState = new FileCorruptBackgroundWorkerUserState();

            FileName = String.Format(OutputDir + "Corrupt {0:D4}.jpg", fileNum++);
            foreach (string word in WordList)
            {
                if (FileCorruptBackgroundWorker.CancellationPending)
                {
                    return;
                }

                nextFileName = String.Format(OutputDir + "Corrupt {0:D4}.jpg", fileNum);
                System.IO.File.Copy(FileName, nextFileName, true);
                FileName = nextFileName;

                // This reads then writes to the file
                CorruptImageFile(FileName, word);
                //System.Threading.Thread.Sleep(500);

                UserState.Image = FileName;
                UserState.Word = word;
                CurrentImage = FileName;
                FileCorruptBackgroundWorker.ReportProgress((int)((double)fileNum / (double)WordList.Count) * 100, UserState);

                fileNum++;
            }
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
            //// paint the current image
            //// Create image.
            System.Diagnostics.Debug.WriteLine(String.Format("Paint {0}", CurrentImage));

            // Figure out actual drawing area
            Rectangle DrawArea = new Rectangle(0,
                                toolStrip.Visible ? toolStrip.Height : 0, 
                                ClientRectangle.Width - 1,
                                ClientRectangle.Height - 
                                ((toolStrip.Visible ? toolStrip.Height : 0) + (statusStrip.Visible ? statusStrip.Height : 0)) - 1);

            if (!String.IsNullOrEmpty(CurrentImage))
            {
                try
                {
                    using (Image newImage = Image.FromFile(CurrentImage))
                    {
                        //// Create point for upper-left corner of image.
                        Point ulCorner = new Point(0, 20);

                        //// Draw image to screen.
                        //e.Graphics.DrawImageUnscaled(newImage, ulCorner);

                        // Scale to fill height
                        Rectangle ImageArea;
                        if (newImage.Height > ClientRectangle.Height)
                        {
                            // what percentage is this.Height of newImage.Height?
                            double percent = (double)ClientRectangle.Height / newImage.Height;
                            ImageArea = new Rectangle(DrawArea.X, DrawArea.Y, (int)((double)newImage.Width * percent), ClientRectangle.Height);
                        }
                        else
                            ImageArea = new Rectangle(DrawArea.X, DrawArea.Y, newImage.Width, newImage.Height);

                        ImageArea.Offset((ClientRectangle.Width - ImageArea.Width) / 2, 0);
                        e.Graphics.DrawImage(newImage, ImageArea);

                        // Force image to be disposed so the file closes
                        newImage.Dispose();
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.WriteLine("paint: exception");
                }

                CurrentImage = null;

                //// Create font and brush.
                Font drawFont = new Font("Arial", 16);
                SolidBrush drawBrush = new SolidBrush(Color.Black);

                //// Create point for upper-left corner of drawing.
                PointF drawPoint = new PointF(10F, 30F);
                e.Graphics.DrawString(toolStripLabelCurrent.Text, drawFont, drawBrush, drawPoint);
            }
        }

        // ==========================================================
        // Menu & Toolbar Event Handlers
        // ==========================================================
        private void toolStripButtonGo_Click(object sender, EventArgs e)
        {
            if (!Running)
            {
                if (!String.IsNullOrEmpty(SelectedImage))
                {
                    try
                    {
                        WordList = GetWords(toolStripLabelText.Text);

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
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = ".";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileCorruptBackgroundWorker.CancelAsync();

                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        WordList = GetWords(openFileDialog1.FileName);
                        toolStripLabelText.Text = openFileDialog1.FileName;
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
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = ".";
            openFileDialog1.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Cancel the background worker
                FileCorruptBackgroundWorker.CancelAsync();

                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            //pictureBox.Image = (Image)new Bitmap(myStream);

                            SelectedImage = openFileDialog1.FileName;
                            CurrentImage = SelectedImage;
                            toolStripLabelImage.Text = SelectedImage;
                            this.Invalidate();
                            this.Update();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
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
    }

}
