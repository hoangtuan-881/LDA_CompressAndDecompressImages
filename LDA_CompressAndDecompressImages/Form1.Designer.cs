namespace ldaimage
{
    partial class Form1
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
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnProcess = new System.Windows.Forms.Button();
            this.pictureBoxOriginal = new System.Windows.Forms.PictureBox();
            this.pictureBoxReconstructed = new System.Windows.Forms.PictureBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.numBlockSize = new System.Windows.Forms.NumericUpDown();
            this.numClustersK = new System.Windows.Forms.NumericUpDown();
            this.numLdaComponents = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOriginal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReconstructed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBlockSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClustersK)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLdaComponents)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(12, 11);
            this.btnLoad.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 0;
            this.btnLoad.Text = "Tải ảnh";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(93, 11);
            this.btnProcess.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(273, 23);
            this.btnProcess.TabIndex = 1;
            this.btnProcess.Text = "Xử lý (Phân cụm+LDA+Tái tạo)";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // pictureBoxOriginal
            // 
            this.pictureBoxOriginal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxOriginal.Location = new System.Drawing.Point(12, 135);
            this.pictureBoxOriginal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBoxOriginal.Name = "pictureBoxOriginal";
            this.pictureBoxOriginal.Size = new System.Drawing.Size(301, 300);
            this.pictureBoxOriginal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxOriginal.TabIndex = 2;
            this.pictureBoxOriginal.TabStop = false;
            // 
            // pictureBoxReconstructed
            // 
            this.pictureBoxReconstructed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxReconstructed.Location = new System.Drawing.Point(369, 135);
            this.pictureBoxReconstructed.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBoxReconstructed.Name = "pictureBoxReconstructed";
            this.pictureBoxReconstructed.Size = new System.Drawing.Size(301, 300);
            this.pictureBoxReconstructed.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxReconstructed.TabIndex = 3;
            this.pictureBoxReconstructed.TabStop = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(77, 491);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(533, 55);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "lblStatus";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numBlockSize
            // 
            this.numBlockSize.Location = new System.Drawing.Point(217, 41);
            this.numBlockSize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numBlockSize.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numBlockSize.Name = "numBlockSize";
            this.numBlockSize.Size = new System.Drawing.Size(120, 22);
            this.numBlockSize.TabIndex = 5;
            this.numBlockSize.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // numClustersK
            // 
            this.numClustersK.Location = new System.Drawing.Point(217, 71);
            this.numClustersK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numClustersK.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numClustersK.Name = "numClustersK";
            this.numClustersK.Size = new System.Drawing.Size(120, 22);
            this.numClustersK.TabIndex = 6;
            this.numClustersK.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            // 
            // numLdaComponents
            // 
            this.numLdaComponents.Location = new System.Drawing.Point(217, 103);
            this.numLdaComponents.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numLdaComponents.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLdaComponents.Name = "numLdaComponents";
            this.numLdaComponents.Size = new System.Drawing.Size(120, 22);
            this.numLdaComponents.TabIndex = 7;
            this.numLdaComponents.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Kích thước khối:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 16);
            this.label2.TabIndex = 9;
            this.label2.Text = "Số lượng cụm (K):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 105);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(168, 16);
            this.label3.TabIndex = 10;
            this.label3.Text = "Thành phần LDA (d <= K-1):";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 441);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(657, 23);
            this.progressBar.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(685, 569);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numLdaComponents);
            this.Controls.Add(this.numClustersK);
            this.Controls.Add(this.numBlockSize);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pictureBoxReconstructed);
            this.Controls.Add(this.pictureBoxOriginal);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.btnLoad);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Nén và giải nén ảnh bằng LDA";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOriginal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReconstructed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBlockSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClustersK)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLdaComponents)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.PictureBox pictureBoxOriginal;
        private System.Windows.Forms.PictureBox pictureBoxReconstructed;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.NumericUpDown numBlockSize;
        private System.Windows.Forms.NumericUpDown numClustersK;
        private System.Windows.Forms.NumericUpDown numLdaComponents;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}

