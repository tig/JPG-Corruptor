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

namespace JPGCorrupt
{
    public partial class JPGCorruptForm : Form
    {
        private Random RandomNum = new Random(42);
        private String SelectedImage;
        private String CurrentImage;
        private List<String> WordList;
        private String OutputDir = "";

        public JPGCorruptForm()
        {
            InitializeComponent();

            toolStripLabelText.Text = "C:\\Code\\JPGCorrupt\\Chapter1.txt";
            WordList = GetWords(toolStripLabelText.Text);

            this.DoubleBuffered = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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


        private void chooseTextMenu_Click(object sender, EventArgs e)
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

        /// <summary>
        /// Background worker context
        /// </summary>
        class CorruptWorker
        {
            public String StartFileName;
        }

        private void corruptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (corruptToolStripMenuItem.Text == "&Stop")
            {
                FileCorruptBackgroundWorker.CancelAsync();
                toolStripLabelImage.Text = "Cancelled";
                toolStripProgressBar1.Value = 0;
            }
            else
            {
                if (!String.IsNullOrEmpty(SelectedImage))
                {
                    corruptToolStripMenuItem.Text = "&Stop";
                    CorruptWorker worker = new CorruptWorker
                    {
                        StartFileName = OutputDir + "Corrupt 0000.jpg"
                    };

                    System.IO.File.Copy(SelectedImage, worker.StartFileName, true);
                    FileCorruptBackgroundWorker.RunWorkerAsync(worker);
                }
            }
        }

        /// <summary>
        /// Corrupts a image file by inserting corruptText at a random place
        /// within the image data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CorruptImageFile(String fileName, String corruptText)
        {
            FileInfo file = new FileInfo(fileName);
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] corruptTextBytes = Encoding.ASCII.GetBytes(corruptText);
                byte[] bytes = null;

                int len = (int)stream.Length;
                if (false)
                {
                    bytes = new byte[len + corruptText.Length];
                    stream.Read(bytes, 0, len);

                    // Skip the first 256 bytes 
                    // TODO: Test whether this is really needed or not. Or if it can be a smaller
                    // number.
                    int rnd = RandomNum.Next(256, len);

                    // Shift original data
                    System.Buffer.BlockCopy(bytes, rnd, bytes, rnd + corruptTextBytes.Length, corruptTextBytes.Length);

                    // Copy in corrupt text
                    System.Buffer.BlockCopy(corruptTextBytes, 0, bytes, rnd, corruptTextBytes.Length);
                }
                else
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
            // UserState contains the newly corrupted filename
            //String file = e.UserState as String;
            //pictureBox.Load(file);

            FileCorruptBackgroundWorkerUserState state = e.UserState as FileCorruptBackgroundWorkerUserState;
            this.Invalidate();
            this.Update();

            toolStripLabelCurrent.Text = state.Image + ", " + state.Word;

            toolStripProgressBar1.Value = e.ProgressPercentage;
                  
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
                    var words = line.Split(' ');
                    foreach (string word in words)
                    {
                        list.Add(word);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Worker method for the background worker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCorruptBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            CorruptWorker worker = e.Argument as CorruptWorker;

            ProcessWordsKeepFiles(worker.StartFileName);
        }

        /// <summary>
        /// Process words without creating a file for each
        /// </summary>
        /// <param name="StartFileName"></param>
        private void ProcessWords(String StartFileName)
        {

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
                FileCorruptBackgroundWorker.ReportProgress((int)((double)fileNum / (double)WordList.Count) * 100, UserState);
                fileNum++;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileCorruptBackgroundWorker.CancelAsync();
            Application.Exit();
        }

        private void FileCorruptBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            corruptToolStripMenuItem.Text = "&Corrupt";
            toolStripLabelCurrent.Text = "Not running";
        }

        private void JPGCorruptForm_Paint(object sender, PaintEventArgs e)
        {
            //// paint the current image
            //// Create image.
            System.Diagnostics.Debug.WriteLine(String.Format("Paint {0}", CurrentImage));
            if (!String.IsNullOrEmpty(CurrentImage))
            {
                Image newImage = Image.FromFile(CurrentImage);

                //// Create point for upper-left corner of image.
                Point ulCorner = new Point(0,20);

                //// Draw image to screen.
                e.Graphics.DrawImageUnscaled(newImage, ulCorner);

                //// Create font and brush.
                Font drawFont = new Font("Arial", 16);
                SolidBrush drawBrush = new SolidBrush(Color.Red);

                //// Create point for upper-left corner of drawing.
                PointF drawPoint = new PointF(10F, 30F);

                e.Graphics.DrawString(toolStripLabelCurrent.Text, drawFont, drawBrush, drawPoint);
            }
        }

    }

}
