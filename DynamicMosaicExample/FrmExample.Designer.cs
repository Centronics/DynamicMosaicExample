namespace DynamicMosaicExample
{
    partial class FrmExample
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
            this.lstWords = new System.Windows.Forms.ListBox();
            this.btnWordAdd = new System.Windows.Forms.Button();
            this.btnWordRemove = new System.Windows.Forms.Button();
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
            this.btnResetLearn = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblElapsedTime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.grpSourceImage = new System.Windows.Forms.GroupBox();
            this.btnLoadImage = new System.Windows.Forms.Button();
            this.btnSaveImage = new System.Windows.Forms.Button();
            this.grpWords = new System.Windows.Forms.GroupBox();
            this.tmrImagesCount = new System.Windows.Forms.Timer(this.components);
            this.dlgSaveImage = new System.Windows.Forms.SaveFileDialog();
            this.dlgOpenImage = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.grpImages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.grpResults.SuspendLayout();
            this.grpSourceImage.SuspendLayout();
            this.grpWords.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbDraw
            // 
            this.pbDraw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDraw.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbDraw.Location = new System.Drawing.Point(5, 32);
            this.pbDraw.Name = "pbDraw";
            this.pbDraw.Size = new System.Drawing.Size(258, 50);
            this.pbDraw.TabIndex = 0;
            this.pbDraw.TabStop = false;
            this.pbDraw.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseDown);
            this.pbDraw.MouseLeave += new System.EventHandler(this.pbDraw_MouseLeave);
            this.pbDraw.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseMove);
            this.pbDraw.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseUp);
            // 
            // btnRecognizeImage
            // 
            this.btnRecognizeImage.Location = new System.Drawing.Point(5, 85);
            this.btnRecognizeImage.Name = "btnRecognizeImage";
            this.btnRecognizeImage.Size = new System.Drawing.Size(114, 23);
            this.btnRecognizeImage.TabIndex = 0;
            this.btnRecognizeImage.Text = "Распознать";
            this.btnRecognizeImage.UseVisualStyleBackColor = true;
            this.btnRecognizeImage.Click += new System.EventHandler(this.btnRecognizeImage_Click);
            // 
            // btnClearImage
            // 
            this.btnClearImage.Location = new System.Drawing.Point(149, 85);
            this.btnClearImage.Name = "btnClearImage";
            this.btnClearImage.Size = new System.Drawing.Size(114, 23);
            this.btnClearImage.TabIndex = 1;
            this.btnClearImage.Text = "Очистить";
            this.btnClearImage.UseVisualStyleBackColor = true;
            this.btnClearImage.Click += new System.EventHandler(this.btnClearImage_Click);
            // 
            // lstResults
            // 
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Location = new System.Drawing.Point(8, 16);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(131, 69);
            this.lstResults.TabIndex = 7;
            // 
            // lstWords
            // 
            this.lstWords.FormattingEnabled = true;
            this.lstWords.Location = new System.Drawing.Point(9, 39);
            this.lstWords.Name = "lstWords";
            this.lstWords.Size = new System.Drawing.Size(130, 95);
            this.lstWords.TabIndex = 5;
            this.lstWords.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lstWords_KeyUp);
            // 
            // btnWordAdd
            // 
            this.btnWordAdd.Location = new System.Drawing.Point(145, 39);
            this.btnWordAdd.Name = "btnWordAdd";
            this.btnWordAdd.Size = new System.Drawing.Size(114, 69);
            this.btnWordAdd.TabIndex = 3;
            this.btnWordAdd.Text = "Добавить слово";
            this.btnWordAdd.UseVisualStyleBackColor = true;
            this.btnWordAdd.Click += new System.EventHandler(this.btnWordAdd_Click);
            // 
            // btnWordRemove
            // 
            this.btnWordRemove.Location = new System.Drawing.Point(145, 111);
            this.btnWordRemove.Name = "btnWordRemove";
            this.btnWordRemove.Size = new System.Drawing.Size(114, 23);
            this.btnWordRemove.TabIndex = 4;
            this.btnWordRemove.Text = "Удалить слово";
            this.btnWordRemove.UseVisualStyleBackColor = true;
            this.btnWordRemove.Click += new System.EventHandler(this.btnWordRemove_Click);
            // 
            // txtWord
            // 
            this.txtWord.Location = new System.Drawing.Point(145, 13);
            this.txtWord.MaxLength = 6;
            this.txtWord.Name = "txtWord";
            this.txtWord.Size = new System.Drawing.Size(114, 20);
            this.txtWord.TabIndex = 2;
            this.txtWord.Tag = "";
            this.txtWord.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtWord_KeyPress);
            this.txtWord.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtWord_KeyUp);
            // 
            // btnImageCreate
            // 
            this.btnImageCreate.Location = new System.Drawing.Point(149, 35);
            this.btnImageCreate.Name = "btnImageCreate";
            this.btnImageCreate.Size = new System.Drawing.Size(114, 23);
            this.btnImageCreate.TabIndex = 8;
            this.btnImageCreate.Text = "Создать образ";
            this.btnImageCreate.UseVisualStyleBackColor = true;
            this.btnImageCreate.Click += new System.EventHandler(this.btnImageCreate_Click);
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
            this.txtImagesCount.Size = new System.Drawing.Size(76, 20);
            this.txtImagesCount.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(147, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Всего:";
            // 
            // lblSymbolName
            // 
            this.lblSymbolName.AutoSize = true;
            this.lblSymbolName.Location = new System.Drawing.Point(63, 19);
            this.lblSymbolName.Name = "lblSymbolName";
            this.lblSymbolName.Size = new System.Drawing.Size(80, 13);
            this.lblSymbolName.TabIndex = 4;
            this.lblSymbolName.Text = "<Неизвестно>";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 19);
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
            this.btnImageDelete.TabIndex = 9;
            this.btnImageDelete.Text = "Удалить";
            this.btnImageDelete.UseVisualStyleBackColor = true;
            this.btnImageDelete.Click += new System.EventHandler(this.btnImageDelete_Click);
            // 
            // btnImagePrev
            // 
            this.btnImagePrev.Location = new System.Drawing.Point(55, 62);
            this.btnImagePrev.Name = "btnImagePrev";
            this.btnImagePrev.Size = new System.Drawing.Size(88, 23);
            this.btnImagePrev.TabIndex = 11;
            this.btnImagePrev.Text = "Предыдущий";
            this.btnImagePrev.UseVisualStyleBackColor = true;
            this.btnImagePrev.Click += new System.EventHandler(this.btnImagePrev_Click);
            // 
            // btnImageNext
            // 
            this.btnImageNext.Location = new System.Drawing.Point(55, 35);
            this.btnImageNext.Name = "btnImageNext";
            this.btnImageNext.Size = new System.Drawing.Size(88, 23);
            this.btnImageNext.TabIndex = 10;
            this.btnImageNext.Text = "Следующий";
            this.btnImageNext.UseVisualStyleBackColor = true;
            this.btnImageNext.Click += new System.EventHandler(this.btnImageNext_Click);
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
            this.grpResults.Controls.Add(this.btnResetLearn);
            this.grpResults.Controls.Add(this.label5);
            this.grpResults.Controls.Add(this.lstResults);
            this.grpResults.Location = new System.Drawing.Point(281, 156);
            this.grpResults.Name = "grpResults";
            this.grpResults.Size = new System.Drawing.Size(265, 90);
            this.grpResults.TabIndex = 6;
            this.grpResults.TabStop = false;
            this.grpResults.Text = "Результаты";
            // 
            // btnResetLearn
            // 
            this.btnResetLearn.Location = new System.Drawing.Point(145, 62);
            this.btnResetLearn.Name = "btnResetLearn";
            this.btnResetLearn.Size = new System.Drawing.Size(114, 23);
            this.btnResetLearn.TabIndex = 9;
            this.btnResetLearn.Text = "Сброс обучения";
            this.btnResetLearn.UseVisualStyleBackColor = true;
            this.btnResetLearn.Click += new System.EventHandler(this.btnResetLearn_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(144, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 39);
            this.label5.TabIndex = 8;
            this.label5.Text = "Результаты отоб-\r\nражаются только\r\nдля имеющихся слов.";
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
            this.label3.Size = new System.Drawing.Size(131, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Новое слово (<= 6 букв):";
            // 
            // grpSourceImage
            // 
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
            // btnLoadImage
            // 
            this.btnLoadImage.Location = new System.Drawing.Point(149, 114);
            this.btnLoadImage.Name = "btnLoadImage";
            this.btnLoadImage.Size = new System.Drawing.Size(114, 23);
            this.btnLoadImage.TabIndex = 17;
            this.btnLoadImage.Text = "Загрузить";
            this.btnLoadImage.UseVisualStyleBackColor = true;
            this.btnLoadImage.Click += new System.EventHandler(this.btnLoadImage_Click);
            // 
            // btnSaveImage
            // 
            this.btnSaveImage.Location = new System.Drawing.Point(5, 114);
            this.btnSaveImage.Name = "btnSaveImage";
            this.btnSaveImage.Size = new System.Drawing.Size(114, 23);
            this.btnSaveImage.TabIndex = 16;
            this.btnSaveImage.Text = "Сохранить";
            this.btnSaveImage.UseVisualStyleBackColor = true;
            this.btnSaveImage.Click += new System.EventHandler(this.btnSaveImage_Click);
            // 
            // grpWords
            // 
            this.grpWords.Controls.Add(this.label3);
            this.grpWords.Controls.Add(this.lstWords);
            this.grpWords.Controls.Add(this.txtWord);
            this.grpWords.Controls.Add(this.btnWordRemove);
            this.grpWords.Controls.Add(this.btnWordAdd);
            this.grpWords.Location = new System.Drawing.Point(281, 7);
            this.grpWords.Name = "grpWords";
            this.grpWords.Size = new System.Drawing.Size(265, 143);
            this.grpWords.TabIndex = 18;
            this.grpWords.TabStop = false;
            this.grpWords.Text = "Искомые слова";
            // 
            // tmrImagesCount
            // 
            this.tmrImagesCount.Enabled = true;
            this.tmrImagesCount.Interval = 1000;
            this.tmrImagesCount.Tick += new System.EventHandler(this.tmrImagesCount_Tick);
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
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Пример применения библиотеки DynamicParser";
            this.Shown += new System.EventHandler(this.FrmExample_Shown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FrmExample_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.grpImages.ResumeLayout(false);
            this.grpImages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
            this.grpResults.ResumeLayout(false);
            this.grpResults.PerformLayout();
            this.grpSourceImage.ResumeLayout(false);
            this.grpSourceImage.PerformLayout();
            this.grpWords.ResumeLayout(false);
            this.grpWords.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDraw;
        private System.Windows.Forms.Button btnRecognizeImage;
        private System.Windows.Forms.Button btnClearImage;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.ListBox lstWords;
        private System.Windows.Forms.Button btnWordAdd;
        private System.Windows.Forms.Button btnWordRemove;
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Timer tmrImagesCount;
        private System.Windows.Forms.Button btnLoadImage;
        private System.Windows.Forms.Button btnSaveImage;
        private System.Windows.Forms.SaveFileDialog dlgSaveImage;
        private System.Windows.Forms.OpenFileDialog dlgOpenImage;
        private System.Windows.Forms.Button btnResetLearn;
    }
}

