//===================================================================
// JPG Corruptor http://tig.github.com/JPG-Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the MIT License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
namespace JPGCorrupt
{
    partial class JPGCorruptForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JPGCorruptForm));
            this.openFile = new System.Windows.Forms.OpenFileDialog();
            this.FileCorruptBackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripLabelText = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripLabelImage = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripLabelCurrent = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonGo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonGoFullscreen = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLoop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonChooseText = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonChooseImage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonAbout = new System.Windows.Forms.ToolStripButton();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.statusStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFile
            // 
            this.openFile.FileName = "openFile";
            this.openFile.Title = "Open File";
            // 
            // FileCorruptBackgroundWorker
            // 
            this.FileCorruptBackgroundWorker.WorkerReportsProgress = true;
            this.FileCorruptBackgroundWorker.WorkerSupportsCancellation = true;
            this.FileCorruptBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.FileCorruptBackgroundWorker_DoWork);
            this.FileCorruptBackgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.FileCorruptBackgroundWorker_ProgressChanged);
            this.FileCorruptBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FileCorruptBackgroundWorker_RunWorkerCompleted);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabelText,
            this.toolStripLabelImage,
            this.toolStripProgressBar,
            this.toolStripLabelCurrent});
            this.statusStrip.Location = new System.Drawing.Point(0, 463);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(721, 24);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip";
            // 
            // toolStripLabelText
            // 
            this.toolStripLabelText.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripLabelText.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.toolStripLabelText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripLabelText.Name = "toolStripLabelText";
            this.toolStripLabelText.Size = new System.Drawing.Size(30, 19);
            this.toolStripLabelText.Text = "text";
            // 
            // toolStripLabelImage
            // 
            this.toolStripLabelImage.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripLabelImage.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.toolStripLabelImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripLabelImage.Name = "toolStripLabelImage";
            this.toolStripLabelImage.Size = new System.Drawing.Size(44, 19);
            this.toolStripLabelImage.Text = "image";
            // 
            // toolStripProgressBar
            // 
            this.toolStripProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(100, 18);
            this.toolStripProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // toolStripLabelCurrent
            // 
            this.toolStripLabelCurrent.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripLabelCurrent.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.toolStripLabelCurrent.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripLabelCurrent.Name = "toolStripLabelCurrent";
            this.toolStripLabelCurrent.Size = new System.Drawing.Size(49, 19);
            this.toolStripLabelCurrent.Text = "current";
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonGo,
            this.toolStripButtonGoFullscreen,
            this.toolStripButtonStop,
            this.toolStripButtonLoop,
            this.toolStripSeparator1,
            this.toolStripButtonChooseText,
            this.toolStripButtonChooseImage,
            this.toolStripSeparator2,
            this.toolStripButtonSave,
            this.toolStripSeparator3,
            this.toolStripButtonAbout});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(721, 25);
            this.toolStrip.TabIndex = 3;
            this.toolStrip.Text = "toolStrip";
            // 
            // toolStripButtonGo
            // 
            this.toolStripButtonGo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonGo.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonGo.Image")));
            this.toolStripButtonGo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonGo.Name = "toolStripButtonGo";
            this.toolStripButtonGo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonGo.Text = "Corrupt";
            this.toolStripButtonGo.ToolTipText = "Start corrupt image process";
            this.toolStripButtonGo.Click += new System.EventHandler(this.toolStripButtonGo_Click);
            // 
            // toolStripButtonGoFullscreen
            // 
            this.toolStripButtonGoFullscreen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonGoFullscreen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonGoFullscreen.Image")));
            this.toolStripButtonGoFullscreen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonGoFullscreen.Name = "toolStripButtonGoFullscreen";
            this.toolStripButtonGoFullscreen.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonGoFullscreen.Text = "Corrupt in full screen mode";
            this.toolStripButtonGoFullscreen.ToolTipText = "Start corrupt image process full screen (ESC to stop)";
            this.toolStripButtonGoFullscreen.Click += new System.EventHandler(this.toolStripButtonGoFullscreen_Click);
            // 
            // toolStripButtonStop
            // 
            this.toolStripButtonStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonStop.Enabled = false;
            this.toolStripButtonStop.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonStop.Image")));
            this.toolStripButtonStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonStop.Name = "toolStripButtonStop";
            this.toolStripButtonStop.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonStop.Text = "Stop";
            this.toolStripButtonStop.Click += new System.EventHandler(this.toolStripButtonStop_Click);
            // 
            // toolStripButtonLoop
            // 
            this.toolStripButtonLoop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonLoop.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoop.Image")));
            this.toolStripButtonLoop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLoop.Name = "toolStripButtonLoop";
            this.toolStripButtonLoop.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonLoop.Text = "Loop Mode";
            this.toolStripButtonLoop.Click += new System.EventHandler(this.toolStripButtonLoop_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonChooseText
            // 
            this.toolStripButtonChooseText.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonChooseText.Image")));
            this.toolStripButtonChooseText.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonChooseText.Name = "toolStripButtonChooseText";
            this.toolStripButtonChooseText.Size = new System.Drawing.Size(101, 22);
            this.toolStripButtonChooseText.Text = "&Choose Text...";
            this.toolStripButtonChooseText.ToolTipText = "Choose text that will corrupt the image";
            this.toolStripButtonChooseText.Click += new System.EventHandler(this.toolStripButtonChooseText_Click);
            // 
            // toolStripButtonChooseImage
            // 
            this.toolStripButtonChooseImage.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonChooseImage.Image")));
            this.toolStripButtonChooseImage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonChooseImage.Name = "toolStripButtonChooseImage";
            this.toolStripButtonChooseImage.Size = new System.Drawing.Size(112, 22);
            this.toolStripButtonChooseImage.Text = "C&hoose Image...";
            this.toolStripButtonChooseImage.ToolTipText = "Choose image that will be corrupted";
            this.toolStripButtonChooseImage.Click += new System.EventHandler(this.toolStripButtonChooseImage_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSave.Image")));
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(103, 22);
            this.toolStripButtonSave.Text = "&Save Current...";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripButtonAbout
            // 
            this.toolStripButtonAbout.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAbout.Image")));
            this.toolStripButtonAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAbout.Name = "toolStripButtonAbout";
            this.toolStripButtonAbout.Size = new System.Drawing.Size(137, 22);
            this.toolStripButtonAbout.Text = "About JPG Corruptor";
            this.toolStripButtonAbout.Click += new System.EventHandler(this.toolStripButtonAbout_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.Title = "Save Image";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // JPGCorruptForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 487);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "JPGCorruptForm";
            this.Text = "JPG Corruptor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.JPGCorruptForm_FormClosing);
            this.SizeChanged += new System.EventHandler(this.JPGCorruptForm_SizeChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.JPGCorruptForm_Paint);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JPGCorruptForm_KeyUp);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFile;
        private System.ComponentModel.BackgroundWorker FileCorruptBackgroundWorker;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLabelImage;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLabelText;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLabelCurrent;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonGo;
        private System.Windows.Forms.ToolStripButton toolStripButtonStop;
        private System.Windows.Forms.ToolStripButton toolStripButtonChooseText;
        private System.Windows.Forms.ToolStripButton toolStripButtonGoFullscreen;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonChooseImage;
        private System.Windows.Forms.ToolStripButton toolStripButtonAbout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonSave;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripButton toolStripButtonLoop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
    }
}

