namespace DynamicMosaicExample
{
    sealed partial class FrmExample
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.btnRecognizeImage = new System.Windows.Forms.Button();
            this.btnClearImage = new System.Windows.Forms.Button();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.txtWord = new System.Windows.Forms.TextBox();
            this.btnImageCreate = new System.Windows.Forms.Button();
            this.grpImages = new System.Windows.Forms.GroupBox();
            this.txtImagesCount = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lblSymbolName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnImageDelete = new System.Windows.Forms.Button();
            this.btnImagePrev = new System.Windows.Forms.Button();
            this.btnImageNext = new System.Windows.Forms.Button();
            this.pbBrowse = new System.Windows.Forms.PictureBox();
            this.grpResults = new System.Windows.Forms.GroupBox();
            this.btnReflexRemove = new System.Windows.Forms.Button();
            this.btnReflexClear = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lblElapsedTime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.grpSourceImage = new System.Windows.Forms.GroupBox();
            this.btnWide = new System.Windows.Forms.Button();
            this.btnNarrow = new System.Windows.Forms.Button();
            this.btnLoadImage = new System.Windows.Forms.Button();
            this.btnSaveImage = new System.Windows.Forms.Button();
            this.grpWords = new System.Windows.Forms.GroupBox();
            this.pbSuccess = new System.Windows.Forms.PictureBox();
            this.grpContains = new System.Windows.Forms.GroupBox();
            this.btnConPrevious = new System.Windows.Forms.Button();
            this.btnConNext = new System.Windows.Forms.Button();
            this.lblConSymbol = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.pbConSymbol = new System.Windows.Forms.PictureBox();
            this.tmrImagesCount = new System.Windows.Forms.Timer(this.components);
            this.dlgSaveImage = new System.Windows.Forms.SaveFileDialog();
            this.dlgOpenImage = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.grpImages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.grpResults.SuspendLayout();
            this.grpSourceImage.SuspendLayout();
            this.grpWords.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).BeginInit();
            this.grpContains.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).BeginInit();
            this.SuspendLayout();
            // 
            // pbDraw
            // 
            this.pbDraw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDraw.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbDraw.Location = new System.Drawing.Point(5, 32);
            this.pbDraw.MaximumSize = new System.Drawing.Size(258, 50);
            this.pbDraw.MinimumSize = new System.Drawing.Size(43, 50);
            this.pbDraw.Name = "pbDraw";
            this.pbDraw.Size = new System.Drawing.Size(258, 50);
            this.pbDraw.TabIndex = 0;
            this.pbDraw.TabStop = false;
            this.pbDraw.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseDown);
            this.pbDraw.MouseLeave += new System.EventHandler(this.PbDraw_MouseLeave);
            this.pbDraw.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseMove);
            this.pbDraw.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseUp);
            // 
            // btnRecognizeImage
            // 
            this.btnRecognizeImage.Location = new System.Drawing.Point(5, 85);
            this.btnRecognizeImage.Name = "btnRecognizeImage";
            this.btnRecognizeImage.Size = new System.Drawing.Size(83, 23);
            this.btnRecognizeImage.TabIndex = 0;
            this.btnRecognizeImage.Text = "Распознать";
            this.btnRecognizeImage.UseVisualStyleBackColor = true;
            this.btnRecognizeImage.Click += new System.EventHandler(this.BtnRecognizeImage_Click);
            // 
            // btnClearImage
            // 
            this.btnClearImage.Enabled = false;
            this.btnClearImage.Location = new System.Drawing.Point(5, 114);
            this.btnClearImage.Name = "btnClearImage";
            this.btnClearImage.Size = new System.Drawing.Size(83, 23);
            this.btnClearImage.TabIndex = 1;
            this.btnClearImage.Text = "Очистить";
            this.btnClearImage.UseVisualStyleBackColor = true;
            this.btnClearImage.Click += new System.EventHandler(this.BtnClearImage_Click);
            // 
            // lstResults
            // 
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Items.AddRange(new object[] {
            "<Создать Reflex>"});
            this.lstResults.Location = new System.Drawing.Point(8, 16);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(131, 69);
            this.lstResults.TabIndex = 12;
            this.lstResults.SelectedIndexChanged += new System.EventHandler(this.LstResults_SelectedIndexChanged);
            // 
            // txtWord
            // 
            this.txtWord.Location = new System.Drawing.Point(107, 13);
            this.txtWord.MaxLength = 6;
            this.txtWord.Name = "txtWord";
            this.txtWord.Size = new System.Drawing.Size(152, 20);
            this.txtWord.TabIndex = 6;
            this.txtWord.Tag = "";
            this.txtWord.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtWord_KeyPress);
            this.txtWord.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtWord_KeyUp);
            // 
            // btnImageCreate
            // 
            this.btnImageCreate.Location = new System.Drawing.Point(149, 38);
            this.btnImageCreate.Name = "btnImageCreate";
            this.btnImageCreate.Size = new System.Drawing.Size(114, 23);
            this.btnImageCreate.TabIndex = 14;
            this.btnImageCreate.Text = "Создать образ";
            this.btnImageCreate.UseVisualStyleBackColor = true;
            this.btnImageCreate.Click += new System.EventHandler(this.BtnImageCreate_Click);
            // 
            // grpImages
            // 
            this.grpImages.Controls.Add(this.txtImagesCount);
            this.grpImages.Controls.Add(this.label4);
            this.grpImages.Controls.Add(this.lblSymbolName);
            this.grpImages.Controls.Add(this.label1);
            this.grpImages.Controls.Add(this.btnImageDelete);
            this.grpImages.Controls.Add(this.btnImagePrev);
            this.grpImages.Controls.Add(this.btnImageCreate);
            this.grpImages.Controls.Add(this.btnImageNext);
            this.grpImages.Controls.Add(this.pbBrowse);
            this.grpImages.Location = new System.Drawing.Point(8, 156);
            this.grpImages.Name = "grpImages";
            this.grpImages.Size = new System.Drawing.Size(267, 90);
            this.grpImages.TabIndex = 12;
            this.grpImages.TabStop = false;
            this.grpImages.Text = "Образы искомых букв";
            // 
            // txtImagesCount
            // 
            this.txtImagesCount.Location = new System.Drawing.Point(185, 15);
            this.txtImagesCount.Name = "txtImagesCount";
            this.txtImagesCount.ReadOnly = true;
            this.txtImagesCount.Size = new System.Drawing.Size(77, 20);
            this.txtImagesCount.TabIndex = 18;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(147, 17);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Всего:";
            // 
            // lblSymbolName
            // 
            this.lblSymbolName.AutoSize = true;
            this.lblSymbolName.Location = new System.Drawing.Point(63, 17);
            this.lblSymbolName.Name = "lblSymbolName";
            this.lblSymbolName.Size = new System.Drawing.Size(80, 13);
            this.lblSymbolName.TabIndex = 4;
            this.lblSymbolName.Text = "<Неизвестно>";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Название:";
            // 
            // btnImageDelete
            // 
            this.btnImageDelete.Location = new System.Drawing.Point(149, 62);
            this.btnImageDelete.Name = "btnImageDelete";
            this.btnImageDelete.Size = new System.Drawing.Size(114, 23);
            this.btnImageDelete.TabIndex = 15;
            this.btnImageDelete.Text = "Удалить";
            this.btnImageDelete.UseVisualStyleBackColor = true;
            this.btnImageDelete.Click += new System.EventHandler(this.BtnImageDelete_Click);
            // 
            // btnImagePrev
            // 
            this.btnImagePrev.Location = new System.Drawing.Point(55, 62);
            this.btnImagePrev.Name = "btnImagePrev";
            this.btnImagePrev.Size = new System.Drawing.Size(88, 23);
            this.btnImagePrev.TabIndex = 17;
            this.btnImagePrev.Text = "Предыдущий";
            this.btnImagePrev.UseVisualStyleBackColor = true;
            this.btnImagePrev.Click += new System.EventHandler(this.BtnImagePrev_Click);
            // 
            // btnImageNext
            // 
            this.btnImageNext.Location = new System.Drawing.Point(55, 38);
            this.btnImageNext.Name = "btnImageNext";
            this.btnImageNext.Size = new System.Drawing.Size(88, 23);
            this.btnImageNext.TabIndex = 16;
            this.btnImageNext.Text = "Следующий";
            this.btnImageNext.UseVisualStyleBackColor = true;
            this.btnImageNext.Click += new System.EventHandler(this.BtnImageNext_Click);
            // 
            // pbBrowse
            // 
            this.pbBrowse.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbBrowse.Location = new System.Drawing.Point(6, 35);
            this.pbBrowse.Name = "pbBrowse";
            this.pbBrowse.Size = new System.Drawing.Size(43, 50);
            this.pbBrowse.TabIndex = 0;
            this.pbBrowse.TabStop = false;
            // 
            // grpResults
            // 
            this.grpResults.Controls.Add(this.btnReflexRemove);
            this.grpResults.Controls.Add(this.btnReflexClear);
            this.grpResults.Controls.Add(this.lstResults);
            this.grpResults.Location = new System.Drawing.Point(281, 156);
            this.grpResults.Name = "grpResults";
            this.grpResults.Size = new System.Drawing.Size(265, 90);
            this.grpResults.TabIndex = 6;
            this.grpResults.TabStop = false;
            this.grpResults.Text = "Созданные объекты Reflex";
            // 
            // btnReflexRemove
            // 
            this.btnReflexRemove.Location = new System.Drawing.Point(145, 17);
            this.btnReflexRemove.Name = "btnReflexRemove";
            this.btnReflexRemove.Size = new System.Drawing.Size(114, 23);
            this.btnReflexRemove.TabIndex = 14;
            this.btnReflexRemove.Text = "Удалить";
            this.btnReflexRemove.UseVisualStyleBackColor = true;
            this.btnReflexRemove.Click += new System.EventHandler(this.BtnReflexRemove_Click);
            // 
            // btnReflexClear
            // 
            this.btnReflexClear.Enabled = false;
            this.btnReflexClear.Location = new System.Drawing.Point(145, 62);
            this.btnReflexClear.Name = "btnReflexClear";
            this.btnReflexClear.Size = new System.Drawing.Size(114, 23);
            this.btnReflexClear.TabIndex = 13;
            this.btnReflexClear.Text = "Очистить";
            this.btnReflexClear.UseVisualStyleBackColor = true;
            this.btnReflexClear.Click += new System.EventHandler(this.BtnReflexClear_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(206, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Затраченное время на распознавание:";
            // 
            // lblElapsedTime
            // 
            this.lblElapsedTime.AutoSize = true;
            this.lblElapsedTime.Location = new System.Drawing.Point(214, 16);
            this.lblElapsedTime.Name = "lblElapsedTime";
            this.lblElapsedTime.Size = new System.Drawing.Size(49, 13);
            this.lblElapsedTime.TabIndex = 15;
            this.lblElapsedTime.Text = "00:00:00";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Слово (<= 6 букв):";
            // 
            // grpSourceImage
            // 
            this.grpSourceImage.Controls.Add(this.btnWide);
            this.grpSourceImage.Controls.Add(this.btnNarrow);
            this.grpSourceImage.Controls.Add(this.btnLoadImage);
            this.grpSourceImage.Controls.Add(this.btnSaveImage);
            this.grpSourceImage.Controls.Add(this.pbDraw);
            this.grpSourceImage.Controls.Add(this.label2);
            this.grpSourceImage.Controls.Add(this.lblElapsedTime);
            this.grpSourceImage.Controls.Add(this.btnRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnClearImage);
            this.grpSourceImage.Location = new System.Drawing.Point(8, 7);
            this.grpSourceImage.Name = "grpSourceImage";
            this.grpSourceImage.Size = new System.Drawing.Size(267, 143);
            this.grpSourceImage.TabIndex = 17;
            this.grpSourceImage.TabStop = false;
            this.grpSourceImage.Text = "Изображение (ЛКМ - рисовать / ПКМ - стереть)";
            // 
            // btnWide
            // 
            this.btnWide.Enabled = false;
            this.btnWide.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnWide.Location = new System.Drawing.Point(189, 85);
            this.btnWide.Name = "btnWide";
            this.btnWide.Size = new System.Drawing.Size(74, 23);
            this.btnWide.TabIndex = 4;
            this.btnWide.Text = "===>";
            this.btnWide.UseVisualStyleBackColor = true;
            this.btnWide.Click += new System.EventHandler(this.BtnWide_Click);
            // 
            // btnNarrow
            // 
            this.btnNarrow.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnNarrow.Location = new System.Drawing.Point(109, 85);
            this.btnNarrow.Name = "btnNarrow";
            this.btnNarrow.Size = new System.Drawing.Size(74, 23);
            this.btnNarrow.TabIndex = 2;
            this.btnNarrow.Text = "<===";
            this.btnNarrow.UseVisualStyleBackColor = true;
            this.btnNarrow.Click += new System.EventHandler(this.BtnNarrow_Click);
            // 
            // btnLoadImage
            // 
            this.btnLoadImage.Location = new System.Drawing.Point(189, 114);
            this.btnLoadImage.Name = "btnLoadImage";
            this.btnLoadImage.Size = new System.Drawing.Size(74, 23);
            this.btnLoadImage.TabIndex = 5;
            this.btnLoadImage.Text = "Загрузить";
            this.btnLoadImage.UseVisualStyleBackColor = true;
            this.btnLoadImage.Click += new System.EventHandler(this.BtnLoadImage_Click);
            // 
            // btnSaveImage
            // 
            this.btnSaveImage.Location = new System.Drawing.Point(109, 114);
            this.btnSaveImage.Name = "btnSaveImage";
            this.btnSaveImage.Size = new System.Drawing.Size(74, 23);
            this.btnSaveImage.TabIndex = 3;
            this.btnSaveImage.Text = "Сохранить";
            this.btnSaveImage.UseVisualStyleBackColor = true;
            this.btnSaveImage.Click += new System.EventHandler(this.BtnSaveImage_Click);
            // 
            // grpWords
            // 
            this.grpWords.Controls.Add(this.pbSuccess);
            this.grpWords.Controls.Add(this.grpContains);
            this.grpWords.Controls.Add(this.label3);
            this.grpWords.Controls.Add(this.txtWord);
            this.grpWords.Location = new System.Drawing.Point(281, 7);
            this.grpWords.Name = "grpWords";
            this.grpWords.Size = new System.Drawing.Size(265, 143);
            this.grpWords.TabIndex = 18;
            this.grpWords.TabStop = false;
            this.grpWords.Text = "Искомое слово";
            // 
            // pbSuccess
            // 
            this.pbSuccess.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbSuccess.Location = new System.Drawing.Point(9, 37);
            this.pbSuccess.Name = "pbSuccess";
            this.pbSuccess.Size = new System.Drawing.Size(92, 100);
            this.pbSuccess.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSuccess.TabIndex = 24;
            this.pbSuccess.TabStop = false;
            // 
            // grpContains
            // 
            this.grpContains.Controls.Add(this.btnConPrevious);
            this.grpContains.Controls.Add(this.btnConNext);
            this.grpContains.Controls.Add(this.lblConSymbol);
            this.grpContains.Controls.Add(this.label6);
            this.grpContains.Controls.Add(this.pbConSymbol);
            this.grpContains.Location = new System.Drawing.Point(107, 37);
            this.grpContains.Name = "grpContains";
            this.grpContains.Size = new System.Drawing.Size(152, 100);
            this.grpContains.TabIndex = 23;
            this.grpContains.TabStop = false;
            this.grpContains.Text = "Содержимое Reflex";
            // 
            // btnConPrevious
            // 
            this.btnConPrevious.Enabled = false;
            this.btnConPrevious.Location = new System.Drawing.Point(7, 68);
            this.btnConPrevious.Name = "btnConPrevious";
            this.btnConPrevious.Size = new System.Drawing.Size(88, 23);
            this.btnConPrevious.TabIndex = 26;
            this.btnConPrevious.Text = "Предыдущий";
            this.btnConPrevious.UseVisualStyleBackColor = true;
            this.btnConPrevious.Click += new System.EventHandler(this.BtnConPrevious_Click);
            // 
            // btnConNext
            // 
            this.btnConNext.Enabled = false;
            this.btnConNext.Location = new System.Drawing.Point(7, 40);
            this.btnConNext.Name = "btnConNext";
            this.btnConNext.Size = new System.Drawing.Size(88, 23);
            this.btnConNext.TabIndex = 25;
            this.btnConNext.Text = "Следующий";
            this.btnConNext.UseVisualStyleBackColor = true;
            this.btnConNext.Click += new System.EventHandler(this.BtnConNext_Click);
            // 
            // lblConSymbol
            // 
            this.lblConSymbol.AutoSize = true;
            this.lblConSymbol.Location = new System.Drawing.Point(65, 18);
            this.lblConSymbol.Name = "lblConSymbol";
            this.lblConSymbol.Size = new System.Drawing.Size(80, 13);
            this.lblConSymbol.TabIndex = 24;
            this.lblConSymbol.Text = "<Неизвестно>";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 18);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(60, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Название:";
            // 
            // pbConSymbol
            // 
            this.pbConSymbol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbConSymbol.Location = new System.Drawing.Point(101, 41);
            this.pbConSymbol.Name = "pbConSymbol";
            this.pbConSymbol.Size = new System.Drawing.Size(43, 50);
            this.pbConSymbol.TabIndex = 19;
            this.pbConSymbol.TabStop = false;
            // 
            // tmrImagesCount
            // 
            this.tmrImagesCount.Enabled = true;
            this.tmrImagesCount.Interval = 1000;
            this.tmrImagesCount.Tick += new System.EventHandler(this.TmrImagesCount_Tick);
            // 
            // dlgSaveImage
            // 
            this.dlgSaveImage.Filter = "BMP|*.bmp";
            // 
            // dlgOpenImage
            // 
            this.dlgOpenImage.Filter = "BMP|*.bmp";
            // 
            // FrmExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 252);
            this.Controls.Add(this.grpWords);
            this.Controls.Add(this.grpSourceImage);
            this.Controls.Add(this.grpResults);
            this.Controls.Add(this.grpImages);
            this.Enabled = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Пример применения библиотеки DynamicMosaic";
            this.Shown += new System.EventHandler(this.FrmExample_Shown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FrmExample_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.grpImages.ResumeLayout(false);
            this.grpImages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
            this.grpResults.ResumeLayout(false);
            this.grpSourceImage.ResumeLayout(false);
            this.grpSourceImage.PerformLayout();
            this.grpWords.ResumeLayout(false);
            this.grpWords.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).EndInit();
            this.grpContains.ResumeLayout(false);
            this.grpContains.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDraw;
        private System.Windows.Forms.Button btnRecognizeImage;
        private System.Windows.Forms.Button btnClearImage;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.TextBox txtWord;
        private System.Windows.Forms.Button btnImageCreate;
        private System.Windows.Forms.GroupBox grpImages;
        private System.Windows.Forms.GroupBox grpResults;
        private System.Windows.Forms.PictureBox pbBrowse;
        private System.Windows.Forms.Button btnImagePrev;
        private System.Windows.Forms.Button btnImageNext;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblSymbolName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblElapsedTime;
        private System.Windows.Forms.Button btnImageDelete;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox grpSourceImage;
        private System.Windows.Forms.GroupBox grpWords;
        private System.Windows.Forms.TextBox txtImagesCount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer tmrImagesCount;
        private System.Windows.Forms.Button btnLoadImage;
        private System.Windows.Forms.Button btnSaveImage;
        private System.Windows.Forms.SaveFileDialog dlgSaveImage;
        private System.Windows.Forms.OpenFileDialog dlgOpenImage;
        private System.Windows.Forms.Button btnReflexClear;
        private System.Windows.Forms.Button btnWide;
        private System.Windows.Forms.Button btnNarrow;
        private System.Windows.Forms.PictureBox pbConSymbol;
        private System.Windows.Forms.GroupBox grpContains;
        private System.Windows.Forms.Button btnConPrevious;
        private System.Windows.Forms.Button btnConNext;
        private System.Windows.Forms.Label lblConSymbol;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox pbSuccess;
        private System.Windows.Forms.Button btnReflexRemove;
    }
}

