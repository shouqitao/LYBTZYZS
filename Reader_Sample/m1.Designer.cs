namespace Reader_Sample
{
    partial class m1
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.addrBox = new System.Windows.Forms.ComboBox();
            this.secnrBox = new System.Windows.Forms.ComboBox();
            this.KeyABox = new System.Windows.Forms.TextBox();
            this.KeyBBox = new System.Windows.Forms.TextBox();
            this.btn_FindCard = new System.Windows.Forms.Button();
            this.btn_VerifyKey = new System.Windows.Forms.Button();
            this.btn_readcard = new System.Windows.Forms.Button();
            this.btn_writecard = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.status = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(29, 157);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "KeyA:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 199);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "KeyB:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(26, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "扇区:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(26, 67);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "块号:";
            // 
            // addrBox
            // 
            this.addrBox.FormattingEnabled = true;
            this.addrBox.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3"});
            this.addrBox.Location = new System.Drawing.Point(92, 64);
            this.addrBox.Name = "addrBox";
            this.addrBox.Size = new System.Drawing.Size(70, 20);
            this.addrBox.TabIndex = 4;
            // 
            // secnrBox
            // 
            this.secnrBox.FormattingEnabled = true;
            this.secnrBox.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15"});
            this.secnrBox.Location = new System.Drawing.Point(92, 23);
            this.secnrBox.Name = "secnrBox";
            this.secnrBox.Size = new System.Drawing.Size(70, 20);
            this.secnrBox.TabIndex = 5;
            // 
            // KeyABox
            // 
            this.KeyABox.Location = new System.Drawing.Point(70, 153);
            this.KeyABox.Name = "KeyABox";
            this.KeyABox.Size = new System.Drawing.Size(92, 21);
            this.KeyABox.TabIndex = 6;
            this.KeyABox.Text = "ffffffffffff";
            // 
            // KeyBBox
            // 
            this.KeyBBox.Location = new System.Drawing.Point(70, 196);
            this.KeyBBox.Name = "KeyBBox";
            this.KeyBBox.Size = new System.Drawing.Size(92, 21);
            this.KeyBBox.TabIndex = 7;
            this.KeyBBox.Text = "ffffffffffff";
            // 
            // btn_FindCard
            // 
            this.btn_FindCard.Location = new System.Drawing.Point(327, 18);
            this.btn_FindCard.Name = "btn_FindCard";
            this.btn_FindCard.Size = new System.Drawing.Size(131, 36);
            this.btn_FindCard.TabIndex = 8;
            this.btn_FindCard.Text = "(1)寻卡";
            this.btn_FindCard.UseVisualStyleBackColor = true;
            this.btn_FindCard.Click += new System.EventHandler(this.btn_FindCard_Click);
            // 
            // btn_VerifyKey
            // 
            this.btn_VerifyKey.Location = new System.Drawing.Point(327, 73);
            this.btn_VerifyKey.Name = "btn_VerifyKey";
            this.btn_VerifyKey.Size = new System.Drawing.Size(131, 36);
            this.btn_VerifyKey.TabIndex = 9;
            this.btn_VerifyKey.Text = "(2)认证秘钥";
            this.btn_VerifyKey.UseVisualStyleBackColor = true;
            this.btn_VerifyKey.Click += new System.EventHandler(this.btn_VerifyKey_Click);
            // 
            // btn_readcard
            // 
            this.btn_readcard.Location = new System.Drawing.Point(327, 127);
            this.btn_readcard.Name = "btn_readcard";
            this.btn_readcard.Size = new System.Drawing.Size(131, 36);
            this.btn_readcard.TabIndex = 10;
            this.btn_readcard.Text = "(3)读卡";
            this.btn_readcard.UseVisualStyleBackColor = true;
            this.btn_readcard.Click += new System.EventHandler(this.btn_readcard_Click);
            // 
            // btn_writecard
            // 
            this.btn_writecard.Location = new System.Drawing.Point(327, 181);
            this.btn_writecard.Name = "btn_writecard";
            this.btn_writecard.Size = new System.Drawing.Size(131, 36);
            this.btn_writecard.TabIndex = 11;
            this.btn_writecard.Text = "(3)写卡";
            this.btn_writecard.UseVisualStyleBackColor = true;
            this.btn_writecard.Click += new System.EventHandler(this.btn_writecard_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(88, 247);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(370, 75);
            this.textBox1.TabIndex = 12;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(29, 250);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 12);
            this.label5.TabIndex = 13;
            this.label5.Text = "读卡显示:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(29, 361);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "写卡数据:";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(88, 352);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(370, 21);
            this.textBox2.TabIndex = 15;
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Location = new System.Drawing.Point(29, 407);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(41, 12);
            this.status.TabIndex = 16;
            this.status.Text = "label7";
            // 
            // m1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 437);
            this.Controls.Add(this.status);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btn_writecard);
            this.Controls.Add(this.btn_readcard);
            this.Controls.Add(this.btn_VerifyKey);
            this.Controls.Add(this.btn_FindCard);
            this.Controls.Add(this.KeyBBox);
            this.Controls.Add(this.KeyABox);
            this.Controls.Add(this.secnrBox);
            this.Controls.Add(this.addrBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "m1";
            this.Text = "m1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox addrBox;
        private System.Windows.Forms.ComboBox secnrBox;
        private System.Windows.Forms.TextBox KeyABox;
        private System.Windows.Forms.TextBox KeyBBox;
        private System.Windows.Forms.Button btn_FindCard;
        private System.Windows.Forms.Button btn_VerifyKey;
        private System.Windows.Forms.Button btn_readcard;
        private System.Windows.Forms.Button btn_writecard;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label status;
    }
}