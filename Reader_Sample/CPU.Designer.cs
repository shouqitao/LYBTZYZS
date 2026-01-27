namespace Reader_Sample
{
    partial class CPU
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
            this.button1 = new System.Windows.Forms.Button();
            this.SlotBox = new System.Windows.Forms.ComboBox();
            this.apduBox = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.showBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.status = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(623, 61);
            this.button1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(138, 40);
            this.button1.TabIndex = 0;
            this.button1.Text = "上电";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SlotBox
            // 
            this.SlotBox.FormattingEnabled = true;
            this.SlotBox.Items.AddRange(new object[] {
            "大卡座",
            "副卡座",
            "SAM1",
            "SAM2",
            "SAM3",
            "SAM4",
            "非接CPU"});
            this.SlotBox.Location = new System.Drawing.Point(336, 63);
            this.SlotBox.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.SlotBox.Name = "SlotBox";
            this.SlotBox.Size = new System.Drawing.Size(224, 29);
            this.SlotBox.TabIndex = 1;
            // 
            // apduBox
            // 
            this.apduBox.Location = new System.Drawing.Point(18, 140);
            this.apduBox.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.apduBox.Name = "apduBox";
            this.apduBox.Size = new System.Drawing.Size(541, 31);
            this.apduBox.TabIndex = 2;
            this.apduBox.Text = "0084000008";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(623, 140);
            this.button2.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(138, 40);
            this.button2.TabIndex = 3;
            this.button2.Text = "执行APDU";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // showBox
            // 
            this.showBox.Location = new System.Drawing.Point(18, 228);
            this.showBox.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.showBox.Multiline = true;
            this.showBox.Name = "showBox";
            this.showBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.showBox.Size = new System.Drawing.Size(739, 260);
            this.showBox.TabIndex = 4;
            this.showBox.WordWrap = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 201);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 21);
            this.label1.TabIndex = 5;
            this.label1.Text = "显示:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 114);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 21);
            this.label2.TabIndex = 6;
            this.label2.Text = "APDU:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(233, 68);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 21);
            this.label3.TabIndex = 7;
            this.label3.Text = "卡座:";
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Location = new System.Drawing.Point(22, 495);
            this.status.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(0, 21);
            this.status.TabIndex = 8;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(36, 56);
            this.button3.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(138, 40);
            this.button3.TabIndex = 9;
            this.button3.Text = "上电";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // CPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(803, 525);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.status);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.showBox);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.apduBox);
            this.Controls.Add(this.SlotBox);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.Name = "CPU";
            this.Text = "CPU";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox SlotBox;
        private System.Windows.Forms.TextBox apduBox;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox showBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button button3;
    }
}