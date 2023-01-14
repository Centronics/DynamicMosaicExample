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
            this.label1 = new System.Windows.Forms.Label();
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(12, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Имя образа:";
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
            this.btnOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Btn_KeyDown);
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
            this.btnClear.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Btn_KeyDown);
            // 
            // pbBox
            // 
            this.pbBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
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
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSymbol);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSymbol";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Образ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmSymbol_FormClosing);
            this.Shown += new System.EventHandler(this.FrmSymbol_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSymbol;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pbBox;
        private System.Windows.Forms.Button btnClear;
    }
}