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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmExample));
            this.btnRecognizeImage = new System.Windows.Forms.Button();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.txtWord = new System.Windows.Forms.TextBox();
            this.grpImages = new System.Windows.Forms.GroupBox();
            this.txtSymbolPath = new System.Windows.Forms.TextBox();
            this.txtImagesCount = new System.Windows.Forms.TextBox();
            this.btnImageDelete = new System.Windows.Forms.Button();
            this.btnImagePrev = new System.Windows.Forms.Button();
            this.btnImageCreate = new System.Windows.Forms.Button();
            this.btnImageNext = new System.Windows.Forms.Button();
            this.pbBrowse = new System.Windows.Forms.PictureBox();
            this.grpResults = new System.Windows.Forms.GroupBox();
            this.btnReflexRemove = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lblElapsedTime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.grpSourceImage = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnWide = new System.Windows.Forms.Button();
            this.btnNarrow = new System.Windows.Forms.Button();
            this.btnLoadImage = new System.Windows.Forms.Button();
            this.btnSaveImage = new System.Windows.Forms.Button();
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.btnClearImage = new System.Windows.Forms.Button();
            this.grpWords = new System.Windows.Forms.GroupBox();
            this.pbSuccess = new System.Windows.Forms.PictureBox();
            this.grpContains = new System.Windows.Forms.GroupBox();
            this.btnConSaveAllImages = new System.Windows.Forms.Button();
            this.btnConSaveImage = new System.Windows.Forms.Button();
            this.txtConSymbol = new System.Windows.Forms.TextBox();
            this.btnConPrevious = new System.Windows.Forms.Button();
            this.btnConNext = new System.Windows.Forms.Button();
            this.pbConSymbol = new System.Windows.Forms.PictureBox();
            this.dlgOpenImage = new System.Windows.Forms.OpenFileDialog();
            this.fswImageChanged = new System.IO.FileSystemWatcher();
            this.btnNextRecogImage = new System.Windows.Forms.Button();
            this.btnPrevRecogImage = new System.Windows.Forms.Button();
            this.grpImages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.grpResults.SuspendLayout();
            this.grpSourceImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.grpWords.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).BeginInit();
            this.grpContains.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fswImageChanged)).BeginInit();
            this.SuspendLayout();
            // 
            // btnRecognizeImage
            // 
            this.btnRecognizeImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnRecognizeImage.Location = new System.Drawing.Point(4, 87);
            this.btnRecognizeImage.Name = "btnRecognizeImage";
            this.btnRecognizeImage.Size = new System.Drawing.Size(98, 23);
            this.btnRecognizeImage.TabIndex = 0;
            this.btnRecognizeImage.Text = "Распознать";
            this.btnRecognizeImage.UseVisualStyleBackColor = true;
            this.btnRecognizeImage.Click += new System.EventHandler(this.BtnRecognizeImage_Click);
            // 
            // lstResults
            // 
            this.lstResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstResults.ColumnWidth = 100;
            this.lstResults.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Items.AddRange(new object[] {
            "<Создать Reflex>"});
            this.lstResults.Location = new System.Drawing.Point(6, 16);
            this.lstResults.MultiColumn = true;
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(217, 67);
            this.lstResults.TabIndex = 12;
            this.lstResults.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.LstResults_DrawItem);
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
            this.txtWord.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtWord_KeyDown);
            this.txtWord.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtWord_KeyPress);
            this.txtWord.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtWord_KeyUp);
            // 
            // grpImages
            // 
            this.grpImages.Controls.Add(this.txtSymbolPath);
            this.grpImages.Controls.Add(this.txtImagesCount);
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
            // txtSymbolPath
            // 
            this.txtSymbolPath.Enabled = false;
            this.txtSymbolPath.Location = new System.Drawing.Point(6, 14);
            this.txtSymbolPath.Name = "txtSymbolPath";
            this.txtSymbolPath.ReadOnly = true;
            this.txtSymbolPath.Size = new System.Drawing.Size(193, 20);
            this.txtSymbolPath.TabIndex = 19;
            this.txtSymbolPath.TextChanged += new System.EventHandler(this.TxtSymbolPath_TextChanged);
            // 
            // txtImagesCount
            // 
            this.txtImagesCount.Enabled = false;
            this.txtImagesCount.Location = new System.Drawing.Point(199, 14);
            this.txtImagesCount.Name = "txtImagesCount";
            this.txtImagesCount.ReadOnly = true;
            this.txtImagesCount.Size = new System.Drawing.Size(64, 20);
            this.txtImagesCount.TabIndex = 18;
            // 
            // btnImageDelete
            // 
            this.btnImageDelete.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.btnImageDelete.Location = new System.Drawing.Point(221, 40);
            this.btnImageDelete.Name = "btnImageDelete";
            this.btnImageDelete.Size = new System.Drawing.Size(40, 40);
            this.btnImageDelete.TabIndex = 15;
            this.btnImageDelete.UseVisualStyleBackColor = true;
            this.btnImageDelete.Click += new System.EventHandler(this.BtnImageDelete_Click);
            // 
            // btnImagePrev
            // 
            this.btnImagePrev.Enabled = false;
            this.btnImagePrev.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnImagePrev.Location = new System.Drawing.Point(55, 40);
            this.btnImagePrev.Name = "btnImagePrev";
            this.btnImagePrev.Size = new System.Drawing.Size(40, 40);
            this.btnImagePrev.TabIndex = 17;
            this.btnImagePrev.UseVisualStyleBackColor = true;
            this.btnImagePrev.Click += new System.EventHandler(this.BtnImagePrev_Click);
            // 
            // btnImageCreate
            // 
            this.btnImageCreate.Image = global::DynamicMosaicExample.Resources.CreateImage;
            this.btnImageCreate.Location = new System.Drawing.Point(159, 40);
            this.btnImageCreate.Name = "btnImageCreate";
            this.btnImageCreate.Size = new System.Drawing.Size(40, 40);
            this.btnImageCreate.TabIndex = 14;
            this.btnImageCreate.UseVisualStyleBackColor = true;
            this.btnImageCreate.Click += new System.EventHandler(this.BtnImageCreate_Click);
            // 
            // btnImageNext
            // 
            this.btnImageNext.Enabled = false;
            this.btnImageNext.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnImageNext.Location = new System.Drawing.Point(101, 40);
            this.btnImageNext.Name = "btnImageNext";
            this.btnImageNext.Size = new System.Drawing.Size(40, 40);
            this.btnImageNext.TabIndex = 16;
            this.btnImageNext.UseVisualStyleBackColor = true;
            this.btnImageNext.Click += new System.EventHandler(this.BtnImageNext_Click);
            // 
            // pbBrowse
            // 
            this.pbBrowse.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbBrowse.Enabled = false;
            this.pbBrowse.Location = new System.Drawing.Point(6, 35);
            this.pbBrowse.Name = "pbBrowse";
            this.pbBrowse.Size = new System.Drawing.Size(43, 50);
            this.pbBrowse.TabIndex = 0;
            this.pbBrowse.TabStop = false;
            // 
            // grpResults
            // 
            this.grpResults.Controls.Add(this.btnReflexRemove);
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
            this.btnReflexRemove.Enabled = false;
            this.btnReflexRemove.Image = global::DynamicMosaicExample.Resources.RemoveSelected;
            this.btnReflexRemove.Location = new System.Drawing.Point(225, 31);
            this.btnReflexRemove.Name = "btnReflexRemove";
            this.btnReflexRemove.Size = new System.Drawing.Size(37, 37);
            this.btnReflexRemove.TabIndex = 14;
            this.btnReflexRemove.UseVisualStyleBackColor = true;
            this.btnReflexRemove.Click += new System.EventHandler(this.BtnReflexRemove_Click);
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
            this.label3.Size = new System.Drawing.Size(91, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Слово (< 7 букв):";
            // 
            // grpSourceImage
            // 
            this.grpSourceImage.Controls.Add(this.btnPrevRecogImage);
            this.grpSourceImage.Controls.Add(this.btnNextRecogImage);
            this.grpSourceImage.Controls.Add(this.button1);
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
            // button1
            // 
            this.button1.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.button1.Location = new System.Drawing.Point(52, 113);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(50, 23);
            this.button1.TabIndex = 17;
            this.button1.UseVisualStyleBackColor = true;
            // 
            // btnWide
            // 
            this.btnWide.Enabled = false;
            this.btnWide.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnWide.Image = global::DynamicMosaicExample.Resources.ExpandRight;
            this.btnWide.Location = new System.Drawing.Point(144, 87);
            this.btnWide.Name = "btnWide";
            this.btnWide.Size = new System.Drawing.Size(36, 23);
            this.btnWide.TabIndex = 4;
            this.btnWide.UseVisualStyleBackColor = true;
            this.btnWide.Click += new System.EventHandler(this.BtnWide_Click);
            // 
            // btnNarrow
            // 
            this.btnNarrow.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnNarrow.Image = global::DynamicMosaicExample.Resources.ExpandLeft;
            this.btnNarrow.Location = new System.Drawing.Point(106, 87);
            this.btnNarrow.Name = "btnNarrow";
            this.btnNarrow.Size = new System.Drawing.Size(36, 23);
            this.btnNarrow.TabIndex = 2;
            this.btnNarrow.UseVisualStyleBackColor = true;
            this.btnNarrow.Click += new System.EventHandler(this.BtnNarrow_Click);
            // 
            // btnLoadImage
            // 
            this.btnLoadImage.Image = global::DynamicMosaicExample.Resources.OpenFile;
            this.btnLoadImage.Location = new System.Drawing.Point(144, 113);
            this.btnLoadImage.Name = "btnLoadImage";
            this.btnLoadImage.Size = new System.Drawing.Size(36, 23);
            this.btnLoadImage.TabIndex = 5;
            this.btnLoadImage.UseVisualStyleBackColor = true;
            this.btnLoadImage.Click += new System.EventHandler(this.BtnLoadImage_Click);
            // 
            // btnSaveImage
            // 
            this.btnSaveImage.Image = global::DynamicMosaicExample.Resources.SaveFile;
            this.btnSaveImage.Location = new System.Drawing.Point(106, 113);
            this.btnSaveImage.Name = "btnSaveImage";
            this.btnSaveImage.Size = new System.Drawing.Size(36, 23);
            this.btnSaveImage.TabIndex = 3;
            this.btnSaveImage.UseVisualStyleBackColor = true;
            this.btnSaveImage.Click += new System.EventHandler(this.BtnSaveImage_Click);
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
            // btnClearImage
            // 
            this.btnClearImage.Enabled = false;
            this.btnClearImage.Image = global::DynamicMosaicExample.Resources.ClearImage;
            this.btnClearImage.Location = new System.Drawing.Point(4, 113);
            this.btnClearImage.Name = "btnClearImage";
            this.btnClearImage.Size = new System.Drawing.Size(46, 23);
            this.btnClearImage.TabIndex = 1;
            this.btnClearImage.UseVisualStyleBackColor = true;
            this.btnClearImage.Click += new System.EventHandler(this.BtnClearImage_Click);
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
            this.pbSuccess.Image = ((System.Drawing.Image)(resources.GetObject("pbSuccess.Image")));
            this.pbSuccess.Location = new System.Drawing.Point(7, 37);
            this.pbSuccess.Name = "pbSuccess";
            this.pbSuccess.Size = new System.Drawing.Size(92, 100);
            this.pbSuccess.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSuccess.TabIndex = 24;
            this.pbSuccess.TabStop = false;
            // 
            // grpContains
            // 
            this.grpContains.Controls.Add(this.btnConSaveAllImages);
            this.grpContains.Controls.Add(this.btnConSaveImage);
            this.grpContains.Controls.Add(this.txtConSymbol);
            this.grpContains.Controls.Add(this.btnConPrevious);
            this.grpContains.Controls.Add(this.btnConNext);
            this.grpContains.Controls.Add(this.pbConSymbol);
            this.grpContains.Location = new System.Drawing.Point(107, 37);
            this.grpContains.Name = "grpContains";
            this.grpContains.Size = new System.Drawing.Size(152, 100);
            this.grpContains.TabIndex = 23;
            this.grpContains.TabStop = false;
            this.grpContains.Text = "Содержимое Reflex";
            // 
            // btnConSaveAllImages
            // 
            this.btnConSaveAllImages.Enabled = false;
            this.btnConSaveAllImages.Image = global::DynamicMosaicExample.Resources.SaveAllImages;
            this.btnConSaveAllImages.Location = new System.Drawing.Point(59, 14);
            this.btnConSaveAllImages.Name = "btnConSaveAllImages";
            this.btnConSaveAllImages.Size = new System.Drawing.Size(40, 40);
            this.btnConSaveAllImages.TabIndex = 28;
            this.btnConSaveAllImages.UseVisualStyleBackColor = true;
            this.btnConSaveAllImages.Click += new System.EventHandler(this.BtnConSaveAllImages_Click);
            // 
            // btnConSaveImage
            // 
            this.btnConSaveImage.Enabled = false;
            this.btnConSaveImage.Image = global::DynamicMosaicExample.Resources.SaveImage;
            this.btnConSaveImage.Location = new System.Drawing.Point(7, 14);
            this.btnConSaveImage.Name = "btnConSaveImage";
            this.btnConSaveImage.Size = new System.Drawing.Size(40, 40);
            this.btnConSaveImage.TabIndex = 27;
            this.btnConSaveImage.UseVisualStyleBackColor = true;
            this.btnConSaveImage.Click += new System.EventHandler(this.BtnConSaveImage_Click);
            // 
            // txtConSymbol
            // 
            this.txtConSymbol.Enabled = false;
            this.txtConSymbol.Location = new System.Drawing.Point(103, 15);
            this.txtConSymbol.Name = "txtConSymbol";
            this.txtConSymbol.ReadOnly = true;
            this.txtConSymbol.Size = new System.Drawing.Size(43, 20);
            this.txtConSymbol.TabIndex = 20;
            // 
            // btnConPrevious
            // 
            this.btnConPrevious.Enabled = false;
            this.btnConPrevious.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnConPrevious.Location = new System.Drawing.Point(7, 56);
            this.btnConPrevious.Name = "btnConPrevious";
            this.btnConPrevious.Size = new System.Drawing.Size(40, 40);
            this.btnConPrevious.TabIndex = 26;
            this.btnConPrevious.UseVisualStyleBackColor = true;
            this.btnConPrevious.Click += new System.EventHandler(this.BtnConPrevious_Click);
            // 
            // btnConNext
            // 
            this.btnConNext.Enabled = false;
            this.btnConNext.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnConNext.Location = new System.Drawing.Point(59, 56);
            this.btnConNext.Name = "btnConNext";
            this.btnConNext.Size = new System.Drawing.Size(40, 40);
            this.btnConNext.TabIndex = 25;
            this.btnConNext.UseVisualStyleBackColor = true;
            this.btnConNext.Click += new System.EventHandler(this.BtnConNext_Click);
            // 
            // pbConSymbol
            // 
            this.pbConSymbol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbConSymbol.Enabled = false;
            this.pbConSymbol.Location = new System.Drawing.Point(103, 45);
            this.pbConSymbol.Name = "pbConSymbol";
            this.pbConSymbol.Size = new System.Drawing.Size(43, 50);
            this.pbConSymbol.TabIndex = 19;
            this.pbConSymbol.TabStop = false;
            // 
            // dlgOpenImage
            // 
            this.dlgOpenImage.Filter = "BMP|*.bmp";
            // 
            // fswImageChanged
            // 
            this.fswImageChanged.EnableRaisingEvents = true;
            this.fswImageChanged.IncludeSubdirectories = true;
            this.fswImageChanged.NotifyFilter = System.IO.NotifyFilters.FileName;
            this.fswImageChanged.SynchronizingObject = this;
            // 
            // btnNextRecogImage
            // 
            this.btnNextRecogImage.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnNextRecogImage.Location = new System.Drawing.Point(224, 87);
            this.btnNextRecogImage.Name = "btnNextRecogImage";
            this.btnNextRecogImage.Size = new System.Drawing.Size(40, 49);
            this.btnNextRecogImage.TabIndex = 18;
            this.btnNextRecogImage.UseVisualStyleBackColor = true;
            // 
            // btnPrevRecogImage
            // 
            this.btnPrevRecogImage.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnPrevRecogImage.Location = new System.Drawing.Point(184, 87);
            this.btnPrevRecogImage.Name = "btnPrevRecogImage";
            this.btnPrevRecogImage.Size = new System.Drawing.Size(40, 49);
            this.btnPrevRecogImage.TabIndex = 19;
            this.btnPrevRecogImage.UseVisualStyleBackColor = true;
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
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Пример применения библиотеки DynamicMosaic";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmExample_FormClosing);
            this.Load += new System.EventHandler(this.FrmExample_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FrmExample_KeyUp);
            this.grpImages.ResumeLayout(false);
            this.grpImages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
            this.grpResults.ResumeLayout(false);
            this.grpSourceImage.ResumeLayout(false);
            this.grpSourceImage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.grpWords.ResumeLayout(false);
            this.grpWords.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).EndInit();
            this.grpContains.ResumeLayout(false);
            this.grpContains.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fswImageChanged)).EndInit();
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
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblElapsedTime;
        private System.Windows.Forms.Button btnImageDelete;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox grpSourceImage;
        private System.Windows.Forms.GroupBox grpWords;
        private System.Windows.Forms.TextBox txtImagesCount;
        private System.Windows.Forms.Button btnLoadImage;
        private System.Windows.Forms.Button btnSaveImage;
        private System.Windows.Forms.OpenFileDialog dlgOpenImage;
        private System.Windows.Forms.Button btnWide;
        private System.Windows.Forms.Button btnNarrow;
        private System.Windows.Forms.PictureBox pbConSymbol;
        private System.Windows.Forms.GroupBox grpContains;
        private System.Windows.Forms.Button btnConPrevious;
        private System.Windows.Forms.Button btnConNext;
        private System.Windows.Forms.PictureBox pbSuccess;
        private System.Windows.Forms.Button btnReflexRemove;
        private System.Windows.Forms.TextBox txtSymbolPath;
        private System.Windows.Forms.TextBox txtConSymbol;
        private System.Windows.Forms.Button btnConSaveImage;
        private System.Windows.Forms.Button btnConSaveAllImages;
        private System.IO.FileSystemWatcher fswImageChanged;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnPrevRecogImage;
        private System.Windows.Forms.Button btnNextRecogImage;
    }
}

