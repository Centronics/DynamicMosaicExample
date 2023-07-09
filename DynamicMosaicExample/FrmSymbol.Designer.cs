namespace DynamicMosaicExample
{
    sealed partial class FrmSymbol
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
            this.txtSymbol = new System.Windows.Forms.TextBox();
            this.lblImageName = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.pbBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbBox)).BeginInit();
            this.SuspendLayout();
            // 
            // txtSymbol
            // 
            this.txtSymbol.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtSymbol.Location = new System.Drawing.Point(132, 5);
            this.txtSymbol.MaxLength = 1;
            this.txtSymbol.Name = "txtSymbol";
            this.txtSymbol.Size = new System.Drawing.Size(18, 22);
            this.txtSymbol.TabIndex = 0;
            this.txtSymbol.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtSymbol.TextChanged += new System.EventHandler(this.TxtSymbolTextCheck);
            this.txtSymbol.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtSymbol_KeyDown);
            this.txtSymbol.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtSymbol_KeyPress);
            // 
            // lblImageName
            // 
            this.lblImageName.AutoSize = true;
            this.lblImageName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblImageName.Location = new System.Drawing.Point(12, 6);
            this.lblImageName.Name = "lblImageName";
            this.lblImageName.Size = new System.Drawing.Size(88, 16);
            this.lblImageName.TabIndex = 1;
            this.lblImageName.Text = "Имя образа:";
            // 
            // btnOK
            // 
            this.btnOK.Image = global::DynamicMosaicExample.Resources.ButtonOk;
            this.btnOK.Location = new System.Drawing.Point(69, 38);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(40, 40);
            this.btnOK.TabIndex = 2;
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            this.btnOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BtnKeyDown);
            // 
            // btnClear
            // 
            this.btnClear.Enabled = false;
            this.btnClear.Image = global::DynamicMosaicExample.Resources.ClearImage;
            this.btnClear.Location = new System.Drawing.Point(115, 38);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(40, 40);
            this.btnClear.TabIndex = 4;
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            this.btnClear.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BtnKeyDown);
            // 
            // pbBox
            // 
            this.pbBox.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbBox.Location = new System.Drawing.Point(15, 33);
            this.pbBox.Name = "pbBox";
            this.pbBox.Size = new System.Drawing.Size(43, 50);
            this.pbBox.TabIndex = 3;
            this.pbBox.TabStop = false;
            this.pbBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PbBox_MouseDown);
            this.pbBox.MouseLeave += new System.EventHandler(this.PbBox_MouseLeave);
            this.pbBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PbBox_MouseMove);
            this.pbBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PbBox_MouseUp);
            // 
            // FrmSymbol
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(167, 95);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.pbBox);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblImageName);
            this.Controls.Add(this.txtSymbol);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSymbol";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Образ";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmSymbol_FormClosed);
            this.Shown += new System.EventHandler(this.FrmSymbol_Shown);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FrmSymbol_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.pbBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSymbol;
        private System.Windows.Forms.Label lblImageName;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pbBox;
        private System.Windows.Forms.Button btnClear;
    }
}