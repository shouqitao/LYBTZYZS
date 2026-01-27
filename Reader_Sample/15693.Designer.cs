namespace Reader_Sample
{
    partial class _15693
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
            this.label_15693addr = new System.Windows.Forms.Label();
            this.textBox_15693addr = new System.Windows.Forms.TextBox();
            this.btn15693read = new System.Windows.Forms.Button();
            this.btn15693write = new System.Windows.Forms.Button();
            this.btnsearch = new System.Windows.Forms.Button();
            this.btn15693wafi = new System.Windows.Forms.Button();
            this.btn15693wdsfid = new System.Windows.Forms.Button();
            this.btn15693ldsfid = new System.Windows.Forms.Button();
            this.btn15693laddr = new System.Windows.Forms.Button();
            this.btn15693cinfo = new System.Windows.Forms.Button();
            this.btn15693lafi = new System.Windows.Forms.Button();
            this.label_15693data = new System.Windows.Forms.Label();
            this.textBox15693data = new System.Windows.Forms.TextBox();
            this.textBox15693show = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label_15693addr
            // 
            this.label_15693addr.AutoSize = true;
            this.label_15693addr.Location = new System.Drawing.Point(307, 22);
            this.label_15693addr.Name = "label_15693addr";
            this.label_15693addr.Size = new System.Drawing.Size(35, 12);
            this.label_15693addr.TabIndex = 0;
            this.label_15693addr.Text = "地址:";
            // 
            // textBox_15693addr
            // 
            this.textBox_15693addr.Location = new System.Drawing.Point(356, 19);
            this.textBox_15693addr.Name = "textBox_15693addr";
            this.textBox_15693addr.Size = new System.Drawing.Size(71, 21);
            this.textBox_15693addr.TabIndex = 1;
            this.textBox_15693addr.Text = "0";
            // 
            // btn15693read
            // 
            this.btn15693read.Location = new System.Drawing.Point(467, 88);
            this.btn15693read.Name = "btn15693read";
            this.btn15693read.Size = new System.Drawing.Size(85, 45);
            this.btn15693read.TabIndex = 2;
            this.btn15693read.Text = "读卡";
            this.btn15693read.UseVisualStyleBackColor = true;
            this.btn15693read.Click += new System.EventHandler(this.btn15693read_Click);
            // 
            // btn15693write
            // 
            this.btn15693write.Location = new System.Drawing.Point(309, 154);
            this.btn15693write.Name = "btn15693write";
            this.btn15693write.Size = new System.Drawing.Size(85, 45);
            this.btn15693write.TabIndex = 3;
            this.btn15693write.Text = "写卡";
            this.btn15693write.UseVisualStyleBackColor = true;
            this.btn15693write.Click += new System.EventHandler(this.btn15693write_Click);
            // 
            // btnsearch
            // 
            this.btnsearch.Location = new System.Drawing.Point(467, 22);
            this.btnsearch.Name = "btnsearch";
            this.btnsearch.Size = new System.Drawing.Size(85, 45);
            this.btnsearch.TabIndex = 4;
            this.btnsearch.Text = "寻卡";
            this.btnsearch.UseVisualStyleBackColor = true;
            this.btnsearch.Click += new System.EventHandler(this.btnsearch_Click);
            // 
            // btn15693wafi
            // 
            this.btn15693wafi.Location = new System.Drawing.Point(309, 233);
            this.btn15693wafi.Name = "btn15693wafi";
            this.btn15693wafi.Size = new System.Drawing.Size(85, 45);
            this.btn15693wafi.TabIndex = 5;
            this.btn15693wafi.Text = "写AFI";
            this.btn15693wafi.UseVisualStyleBackColor = true;
            this.btn15693wafi.Click += new System.EventHandler(this.btn15693wafi_Click);
            // 
            // btn15693wdsfid
            // 
            this.btn15693wdsfid.Location = new System.Drawing.Point(309, 295);
            this.btn15693wdsfid.Name = "btn15693wdsfid";
            this.btn15693wdsfid.Size = new System.Drawing.Size(85, 45);
            this.btn15693wdsfid.TabIndex = 6;
            this.btn15693wdsfid.Text = "写DSFID";
            this.btn15693wdsfid.UseVisualStyleBackColor = true;
            this.btn15693wdsfid.Click += new System.EventHandler(this.btn15693wdsfid_Click);
            // 
            // btn15693ldsfid
            // 
            this.btn15693ldsfid.Location = new System.Drawing.Point(467, 295);
            this.btn15693ldsfid.Name = "btn15693ldsfid";
            this.btn15693ldsfid.Size = new System.Drawing.Size(85, 45);
            this.btn15693ldsfid.TabIndex = 7;
            this.btn15693ldsfid.Text = "锁DSFID";
            this.btn15693ldsfid.UseVisualStyleBackColor = true;
            this.btn15693ldsfid.Click += new System.EventHandler(this.btn15693ldsfid_Click);
            // 
            // btn15693laddr
            // 
            this.btn15693laddr.Location = new System.Drawing.Point(467, 154);
            this.btn15693laddr.Name = "btn15693laddr";
            this.btn15693laddr.Size = new System.Drawing.Size(85, 45);
            this.btn15693laddr.TabIndex = 8;
            this.btn15693laddr.Text = "锁块";
            this.btn15693laddr.UseVisualStyleBackColor = true;
            this.btn15693laddr.Click += new System.EventHandler(this.btn15693laddr_Click);
            // 
            // btn15693cinfo
            // 
            this.btn15693cinfo.Location = new System.Drawing.Point(309, 88);
            this.btn15693cinfo.Name = "btn15693cinfo";
            this.btn15693cinfo.Size = new System.Drawing.Size(85, 45);
            this.btn15693cinfo.TabIndex = 9;
            this.btn15693cinfo.Text = "卡片信息";
            this.btn15693cinfo.UseVisualStyleBackColor = true;
            this.btn15693cinfo.Click += new System.EventHandler(this.btn15693cinfo_Click);
            // 
            // btn15693lafi
            // 
            this.btn15693lafi.Location = new System.Drawing.Point(467, 233);
            this.btn15693lafi.Name = "btn15693lafi";
            this.btn15693lafi.Size = new System.Drawing.Size(85, 45);
            this.btn15693lafi.TabIndex = 10;
            this.btn15693lafi.Text = "锁AFI";
            this.btn15693lafi.UseVisualStyleBackColor = true;
            this.btn15693lafi.Click += new System.EventHandler(this.btn15693lafi_Click);
            // 
            // label_15693data
            // 
            this.label_15693data.AutoSize = true;
            this.label_15693data.Location = new System.Drawing.Point(307, 50);
            this.label_15693data.Name = "label_15693data";
            this.label_15693data.Size = new System.Drawing.Size(35, 12);
            this.label_15693data.TabIndex = 11;
            this.label_15693data.Text = "数据:";
            // 
            // textBox15693data
            // 
            this.textBox15693data.Location = new System.Drawing.Point(356, 50);
            this.textBox15693data.Name = "textBox15693data";
            this.textBox15693data.Size = new System.Drawing.Size(101, 21);
            this.textBox15693data.TabIndex = 12;
            this.textBox15693data.Text = "01020304";
            // 
            // textBox15693show
            // 
            this.textBox15693show.Location = new System.Drawing.Point(25, 19);
            this.textBox15693show.Multiline = true;
            this.textBox15693show.Name = "textBox15693show";
            this.textBox15693show.Size = new System.Drawing.Size(259, 321);
            this.textBox15693show.TabIndex = 13;
            // 
            // _15693
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 362);
            this.Controls.Add(this.textBox15693show);
            this.Controls.Add(this.textBox15693data);
            this.Controls.Add(this.label_15693data);
            this.Controls.Add(this.btn15693lafi);
            this.Controls.Add(this.btn15693cinfo);
            this.Controls.Add(this.btn15693laddr);
            this.Controls.Add(this.btn15693ldsfid);
            this.Controls.Add(this.btn15693wdsfid);
            this.Controls.Add(this.btn15693wafi);
            this.Controls.Add(this.btnsearch);
            this.Controls.Add(this.btn15693write);
            this.Controls.Add(this.btn15693read);
            this.Controls.Add(this.textBox_15693addr);
            this.Controls.Add(this.label_15693addr);
            this.Name = "_15693";
            this.Text = "_15693";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_15693addr;
        private System.Windows.Forms.TextBox textBox_15693addr;
        private System.Windows.Forms.Button btn15693read;
        private System.Windows.Forms.Button btn15693write;
        private System.Windows.Forms.Button btnsearch;
        private System.Windows.Forms.Button btn15693wafi;
        private System.Windows.Forms.Button btn15693wdsfid;
        private System.Windows.Forms.Button btn15693ldsfid;
        private System.Windows.Forms.Button btn15693laddr;
        private System.Windows.Forms.Button btn15693cinfo;
        private System.Windows.Forms.Button btn15693lafi;
        private System.Windows.Forms.Label label_15693data;
        private System.Windows.Forms.TextBox textBox15693data;
        private System.Windows.Forms.TextBox textBox15693show;
    }
}