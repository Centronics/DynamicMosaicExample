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
            this.grpResults = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblElapsedTime = new System.Windows.Forms.Label();
            this.grpSourceImage = new System.Windows.Forms.GroupBox();
            this.grpWords = new System.Windows.Forms.GroupBox();
            this.grpContains = new System.Windows.Forms.GroupBox();
            this.txtConSymbol = new System.Windows.Forms.TextBox();
            this.dlgOpenImage = new System.Windows.Forms.OpenFileDialog();
            this.btnConSaveAllImages = new System.Windows.Forms.Button();
            this.btnConSaveImage = new System.Windows.Forms.Button();
            this.btnConPrevious = new System.Windows.Forms.Button();
            this.btnConNext = new System.Windows.Forms.Button();
            this.pbConSymbol = new System.Windows.Forms.PictureBox();
            this.pbSuccess = new System.Windows.Forms.PictureBox();
            this.btnPrevRecogImage = new System.Windows.Forms.Button();
            this.btnNextRecogImage = new System.Windows.Forms.Button();
            this.btnDeleteRecognizeImage = new System.Windows.Forms.Button();
            this.btnWide = new System.Windows.Forms.Button();
            this.btnNarrow = new System.Windows.Forms.Button();
            this.btnLoadRecognizeImage = new System.Windows.Forms.Button();
            this.btnSaveRecognizeImage = new System.Windows.Forms.Button();
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.btnClearImage = new System.Windows.Forms.Button();
            this.btnImageUpToQueries = new System.Windows.Forms.Button();
            this.btnImageDelete = new System.Windows.Forms.Button();
            this.btnImagePrev = new System.Windows.Forms.Button();
            this.btnImageCreate = new System.Windows.Forms.Button();
            this.btnImageNext = new System.Windows.Forms.Button();
            this.pbBrowse = new System.Windows.Forms.PictureBox();
            this.grpImages.SuspendLayout();
            this.grpResults.SuspendLayout();
            this.grpSourceImage.SuspendLayout();
            this.grpWords.SuspendLayout();
            this.grpContains.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.SuspendLayout();
            // 
            // btnRecognizeImage
            // 
            this.btnRecognizeImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnRecognizeImage.Location = new System.Drawing.Point(1, 77);
            this.btnRecognizeImage.Name = "btnRecognizeImage";
            this.btnRecognizeImage.Size = new System.Drawing.Size(98, 23);
            this.btnRecognizeImage.TabIndex = 0;
            this.btnRecognizeImage.Text = "Найти";
            this.btnRecognizeImage.UseVisualStyleBackColor = true;
            this.btnRecognizeImage.Click += new System.EventHandler(this.BtnRecognizeImage_Click);
            // 
            // lstResults
            // 
            this.lstResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstResults.ColumnWidth = 100;
            this.lstResults.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Location = new System.Drawing.Point(2, 16);
            this.lstResults.MultiColumn = true;
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(124, 67);
            this.lstResults.TabIndex = 17;
            this.lstResults.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.LstResults_DrawItem);
            this.lstResults.SelectedIndexChanged += new System.EventHandler(this.LstResults_SelectedIndexChanged);
            // 
            // txtWord
            // 
            this.txtWord.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtWord.Location = new System.Drawing.Point(2, 13);
            this.txtWord.MaxLength = 6;
            this.txtWord.Name = "txtWord";
            this.txtWord.Size = new System.Drawing.Size(95, 23);
            this.txtWord.TabIndex = 9;
            this.txtWord.Tag = "";
            this.txtWord.Text = "WWWWWW";
            this.txtWord.TextChanged += new System.EventHandler(this.TxtWord_TextChanged);
            this.txtWord.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtWord_KeyDown);
            this.txtWord.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtWord_KeyPress);
            this.txtWord.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtWord_KeyUp);
            // 
            // grpImages
            // 
            this.grpImages.Controls.Add(this.btnImageUpToQueries);
            this.grpImages.Controls.Add(this.txtSymbolPath);
            this.grpImages.Controls.Add(this.txtImagesCount);
            this.grpImages.Controls.Add(this.btnImageDelete);
            this.grpImages.Controls.Add(this.btnImagePrev);
            this.grpImages.Controls.Add(this.btnImageCreate);
            this.grpImages.Controls.Add(this.btnImageNext);
            this.grpImages.Controls.Add(this.pbBrowse);
            this.grpImages.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpImages.Location = new System.Drawing.Point(1, 125);
            this.grpImages.Name = "grpImages";
            this.grpImages.Size = new System.Drawing.Size(262, 90);
            this.grpImages.TabIndex = 26;
            this.grpImages.TabStop = false;
            this.grpImages.Text = "Образы искомых букв";
            // 
            // txtSymbolPath
            // 
            this.txtSymbolPath.Enabled = false;
            this.txtSymbolPath.Location = new System.Drawing.Point(2, 14);
            this.txtSymbolPath.Name = "txtSymbolPath";
            this.txtSymbolPath.ReadOnly = true;
            this.txtSymbolPath.Size = new System.Drawing.Size(193, 20);
            this.txtSymbolPath.TabIndex = 10;
            this.txtSymbolPath.TextChanged += new System.EventHandler(this.TxtSymbolPath_TextChanged);
            // 
            // txtImagesCount
            // 
            this.txtImagesCount.Enabled = false;
            this.txtImagesCount.Location = new System.Drawing.Point(195, 14);
            this.txtImagesCount.Name = "txtImagesCount";
            this.txtImagesCount.ReadOnly = true;
            this.txtImagesCount.Size = new System.Drawing.Size(65, 20);
            this.txtImagesCount.TabIndex = 11;
            // 
            // grpResults
            // 
            this.grpResults.Controls.Add(this.lstResults);
            this.grpResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpResults.Location = new System.Drawing.Point(267, 37);
            this.grpResults.Name = "grpResults";
            this.grpResults.Size = new System.Drawing.Size(129, 88);
            this.grpResults.TabIndex = 27;
            this.grpResults.TabStop = false;
            this.grpResults.Text = "История объекта";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(158, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Затраченное время на поиск:";
            // 
            // lblElapsedTime
            // 
            this.lblElapsedTime.AutoSize = true;
            this.lblElapsedTime.Location = new System.Drawing.Point(206, 12);
            this.lblElapsedTime.Name = "lblElapsedTime";
            this.lblElapsedTime.Size = new System.Drawing.Size(49, 13);
            this.lblElapsedTime.TabIndex = 15;
            this.lblElapsedTime.Text = "00:00:00";
            // 
            // grpSourceImage
            // 
            this.grpSourceImage.Controls.Add(this.btnPrevRecogImage);
            this.grpSourceImage.Controls.Add(this.btnNextRecogImage);
            this.grpSourceImage.Controls.Add(this.btnDeleteRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnWide);
            this.grpSourceImage.Controls.Add(this.btnNarrow);
            this.grpSourceImage.Controls.Add(this.btnLoadRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnSaveRecognizeImage);
            this.grpSourceImage.Controls.Add(this.pbDraw);
            this.grpSourceImage.Controls.Add(this.label2);
            this.grpSourceImage.Controls.Add(this.lblElapsedTime);
            this.grpSourceImage.Controls.Add(this.btnRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnClearImage);
            this.grpSourceImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpSourceImage.Location = new System.Drawing.Point(1, -1);
            this.grpSourceImage.Name = "grpSourceImage";
            this.grpSourceImage.Size = new System.Drawing.Size(262, 128);
            this.grpSourceImage.TabIndex = 24;
            this.grpSourceImage.TabStop = false;
            this.grpSourceImage.Text = "Рисование (ЛКМ / ПКМ)";
            // 
            // grpWords
            // 
            this.grpWords.Controls.Add(this.pbSuccess);
            this.grpWords.Controls.Add(this.txtWord);
            this.grpWords.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpWords.Location = new System.Drawing.Point(267, -1);
            this.grpWords.Name = "grpWords";
            this.grpWords.Size = new System.Drawing.Size(129, 40);
            this.grpWords.TabIndex = 25;
            this.grpWords.TabStop = false;
            this.grpWords.Text = "Искомое слово";
            // 
            // grpContains
            // 
            this.grpContains.Controls.Add(this.btnConSaveAllImages);
            this.grpContains.Controls.Add(this.btnConSaveImage);
            this.grpContains.Controls.Add(this.txtConSymbol);
            this.grpContains.Controls.Add(this.btnConPrevious);
            this.grpContains.Controls.Add(this.btnConNext);
            this.grpContains.Controls.Add(this.pbConSymbol);
            this.grpContains.Location = new System.Drawing.Point(267, 124);
            this.grpContains.Name = "grpContains";
            this.grpContains.Size = new System.Drawing.Size(129, 91);
            this.grpContains.TabIndex = 23;
            this.grpContains.TabStop = false;
            this.grpContains.Text = "Содержимое";
            // 
            // txtConSymbol
            // 
            this.txtConSymbol.Enabled = false;
            this.txtConSymbol.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold);
            this.txtConSymbol.Location = new System.Drawing.Point(2, 14);
            this.txtConSymbol.Name = "txtConSymbol";
            this.txtConSymbol.ReadOnly = true;
            this.txtConSymbol.Size = new System.Drawing.Size(124, 23);
            this.txtConSymbol.TabIndex = 23;
            this.txtConSymbol.Text = "W";
            // 
            // dlgOpenImage
            // 
            this.dlgOpenImage.Filter = "BMP|*.bmp";
            // 
            // btnConSaveAllImages
            // 
            this.btnConSaveAllImages.Enabled = false;
            this.btnConSaveAllImages.Image = global::DynamicMosaicExample.Resources.SaveAllImages1;
            this.btnConSaveAllImages.Location = new System.Drawing.Point(43, 39);
            this.btnConSaveAllImages.Name = "btnConSaveAllImages";
            this.btnConSaveAllImages.Size = new System.Drawing.Size(38, 24);
            this.btnConSaveAllImages.TabIndex = 20;
            this.btnConSaveAllImages.UseVisualStyleBackColor = true;
            this.btnConSaveAllImages.Click += new System.EventHandler(this.BtnConSaveAllImages_Click);
            // 
            // btnConSaveImage
            // 
            this.btnConSaveImage.Enabled = false;
            this.btnConSaveImage.Image = global::DynamicMosaicExample.Resources.SaveImage1;
            this.btnConSaveImage.Location = new System.Drawing.Point(4, 39);
            this.btnConSaveImage.Name = "btnConSaveImage";
            this.btnConSaveImage.Size = new System.Drawing.Size(38, 24);
            this.btnConSaveImage.TabIndex = 19;
            this.btnConSaveImage.UseVisualStyleBackColor = true;
            this.btnConSaveImage.Click += new System.EventHandler(this.BtnConSaveImage_Click);
            // 
            // btnConPrevious
            // 
            this.btnConPrevious.Enabled = false;
            this.btnConPrevious.Image = global::DynamicMosaicExample.Resources.PreviousImage1;
            this.btnConPrevious.Location = new System.Drawing.Point(4, 63);
            this.btnConPrevious.Name = "btnConPrevious";
            this.btnConPrevious.Size = new System.Drawing.Size(38, 24);
            this.btnConPrevious.TabIndex = 21;
            this.btnConPrevious.UseVisualStyleBackColor = true;
            this.btnConPrevious.Click += new System.EventHandler(this.BtnConPrevious_Click);
            // 
            // btnConNext
            // 
            this.btnConNext.Enabled = false;
            this.btnConNext.Image = global::DynamicMosaicExample.Resources.NextImage1;
            this.btnConNext.Location = new System.Drawing.Point(43, 63);
            this.btnConNext.Name = "btnConNext";
            this.btnConNext.Size = new System.Drawing.Size(38, 24);
            this.btnConNext.TabIndex = 22;
            this.btnConNext.UseVisualStyleBackColor = true;
            this.btnConNext.Click += new System.EventHandler(this.BtnConNext_Click);
            // 
            // pbConSymbol
            // 
            this.pbConSymbol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbConSymbol.Enabled = false;
            this.pbConSymbol.Location = new System.Drawing.Point(83, 38);
            this.pbConSymbol.Name = "pbConSymbol";
            this.pbConSymbol.Size = new System.Drawing.Size(43, 50);
            this.pbConSymbol.TabIndex = 19;
            this.pbConSymbol.TabStop = false;
            // 
            // pbSuccess
            // 
            this.pbSuccess.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbSuccess.Image = global::DynamicMosaicExample.Resources.Result_Unknown;
            this.pbSuccess.Location = new System.Drawing.Point(97, 13);
            this.pbSuccess.Name = "pbSuccess";
            this.pbSuccess.Size = new System.Drawing.Size(29, 23);
            this.pbSuccess.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSuccess.TabIndex = 24;
            this.pbSuccess.TabStop = false;
            // 
            // btnPrevRecogImage
            // 
            this.btnPrevRecogImage.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnPrevRecogImage.Location = new System.Drawing.Point(182, 77);
            this.btnPrevRecogImage.Name = "btnPrevRecogImage";
            this.btnPrevRecogImage.Size = new System.Drawing.Size(40, 49);
            this.btnPrevRecogImage.TabIndex = 7;
            this.btnPrevRecogImage.UseVisualStyleBackColor = true;
            this.btnPrevRecogImage.Click += new System.EventHandler(this.BtnPrevRecogImage_Click);
            // 
            // btnNextRecogImage
            // 
            this.btnNextRecogImage.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnNextRecogImage.Location = new System.Drawing.Point(221, 77);
            this.btnNextRecogImage.Name = "btnNextRecogImage";
            this.btnNextRecogImage.Size = new System.Drawing.Size(40, 49);
            this.btnNextRecogImage.TabIndex = 8;
            this.btnNextRecogImage.UseVisualStyleBackColor = true;
            this.btnNextRecogImage.Click += new System.EventHandler(this.BtnNextRecogImage_Click);
            // 
            // btnDeleteRecognizeImage
            // 
            this.btnDeleteRecognizeImage.Enabled = false;
            this.btnDeleteRecognizeImage.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.btnDeleteRecognizeImage.Location = new System.Drawing.Point(49, 103);
            this.btnDeleteRecognizeImage.Name = "btnDeleteRecognizeImage";
            this.btnDeleteRecognizeImage.Size = new System.Drawing.Size(50, 23);
            this.btnDeleteRecognizeImage.TabIndex = 2;
            this.btnDeleteRecognizeImage.UseVisualStyleBackColor = true;
            this.btnDeleteRecognizeImage.Click += new System.EventHandler(this.BtnDeleteRecognizeImage_Click);
            // 
            // btnWide
            // 
            this.btnWide.Enabled = false;
            this.btnWide.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnWide.Image = global::DynamicMosaicExample.Resources.ExpandRight;
            this.btnWide.Location = new System.Drawing.Point(141, 77);
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
            this.btnNarrow.Location = new System.Drawing.Point(103, 77);
            this.btnNarrow.Name = "btnNarrow";
            this.btnNarrow.Size = new System.Drawing.Size(36, 23);
            this.btnNarrow.TabIndex = 3;
            this.btnNarrow.UseVisualStyleBackColor = true;
            this.btnNarrow.Click += new System.EventHandler(this.BtnNarrow_Click);
            // 
            // btnLoadRecognizeImage
            // 
            this.btnLoadRecognizeImage.Image = global::DynamicMosaicExample.Resources.OpenFile;
            this.btnLoadRecognizeImage.Location = new System.Drawing.Point(141, 103);
            this.btnLoadRecognizeImage.Name = "btnLoadRecognizeImage";
            this.btnLoadRecognizeImage.Size = new System.Drawing.Size(36, 23);
            this.btnLoadRecognizeImage.TabIndex = 6;
            this.btnLoadRecognizeImage.UseVisualStyleBackColor = true;
            this.btnLoadRecognizeImage.Click += new System.EventHandler(this.BtnLoadRecognizeImage_Click);
            // 
            // btnSaveRecognizeImage
            // 
            this.btnSaveRecognizeImage.Enabled = false;
            this.btnSaveRecognizeImage.Image = global::DynamicMosaicExample.Resources.SaveFile;
            this.btnSaveRecognizeImage.Location = new System.Drawing.Point(103, 103);
            this.btnSaveRecognizeImage.Name = "btnSaveRecognizeImage";
            this.btnSaveRecognizeImage.Size = new System.Drawing.Size(36, 23);
            this.btnSaveRecognizeImage.TabIndex = 5;
            this.btnSaveRecognizeImage.UseVisualStyleBackColor = true;
            this.btnSaveRecognizeImage.Click += new System.EventHandler(this.BtnSaveRecognizeImage_Click);
            // 
            // pbDraw
            // 
            this.pbDraw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDraw.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbDraw.Location = new System.Drawing.Point(2, 27);
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
            this.btnClearImage.Location = new System.Drawing.Point(1, 103);
            this.btnClearImage.Name = "btnClearImage";
            this.btnClearImage.Size = new System.Drawing.Size(46, 23);
            this.btnClearImage.TabIndex = 1;
            this.btnClearImage.UseVisualStyleBackColor = true;
            this.btnClearImage.Click += new System.EventHandler(this.BtnClearImage_Click);
            // 
            // btnImageUpToQueries
            // 
            this.btnImageUpToQueries.Enabled = false;
            this.btnImageUpToQueries.Image = global::DynamicMosaicExample.Resources.UpToQueries;
            this.btnImageUpToQueries.Location = new System.Drawing.Point(128, 35);
            this.btnImageUpToQueries.Name = "btnImageUpToQueries";
            this.btnImageUpToQueries.Size = new System.Drawing.Size(50, 51);
            this.btnImageUpToQueries.TabIndex = 14;
            this.btnImageUpToQueries.UseVisualStyleBackColor = true;
            this.btnImageUpToQueries.Click += new System.EventHandler(this.BtnImageUpToQueries_Click);
            // 
            // btnImageDelete
            // 
            this.btnImageDelete.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.btnImageDelete.Location = new System.Drawing.Point(221, 35);
            this.btnImageDelete.Name = "btnImageDelete";
            this.btnImageDelete.Size = new System.Drawing.Size(40, 52);
            this.btnImageDelete.TabIndex = 16;
            this.btnImageDelete.UseVisualStyleBackColor = true;
            this.btnImageDelete.Click += new System.EventHandler(this.BtnImageDelete_Click);
            // 
            // btnImagePrev
            // 
            this.btnImagePrev.Enabled = false;
            this.btnImagePrev.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnImagePrev.Location = new System.Drawing.Point(45, 35);
            this.btnImagePrev.Name = "btnImagePrev";
            this.btnImagePrev.Size = new System.Drawing.Size(40, 52);
            this.btnImagePrev.TabIndex = 12;
            this.btnImagePrev.UseVisualStyleBackColor = true;
            this.btnImagePrev.Click += new System.EventHandler(this.BtnImagePrev_Click);
            // 
            // btnImageCreate
            // 
            this.btnImageCreate.Image = global::DynamicMosaicExample.Resources.CreateImage;
            this.btnImageCreate.Location = new System.Drawing.Point(182, 35);
            this.btnImageCreate.Name = "btnImageCreate";
            this.btnImageCreate.Size = new System.Drawing.Size(40, 52);
            this.btnImageCreate.TabIndex = 15;
            this.btnImageCreate.UseVisualStyleBackColor = true;
            this.btnImageCreate.Click += new System.EventHandler(this.BtnImageCreate_Click);
            // 
            // btnImageNext
            // 
            this.btnImageNext.Enabled = false;
            this.btnImageNext.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnImageNext.Location = new System.Drawing.Point(84, 35);
            this.btnImageNext.Name = "btnImageNext";
            this.btnImageNext.Size = new System.Drawing.Size(40, 52);
            this.btnImageNext.TabIndex = 13;
            this.btnImageNext.UseVisualStyleBackColor = true;
            this.btnImageNext.Click += new System.EventHandler(this.BtnImageNext_Click);
            // 
            // pbBrowse
            // 
            this.pbBrowse.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbBrowse.Enabled = false;
            this.pbBrowse.Location = new System.Drawing.Point(2, 36);
            this.pbBrowse.Name = "pbBrowse";
            this.pbBrowse.Size = new System.Drawing.Size(43, 50);
            this.pbBrowse.TabIndex = 0;
            this.pbBrowse.TabStop = false;
            // 
            // FrmExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 215);
            this.Controls.Add(this.grpContains);
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
            this.Text = "Демонстрационный стенд";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmExample_FormClosing);
            this.Shown += new System.EventHandler(this.FrmExample_Shown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FrmExample_KeyUp);
            this.grpImages.ResumeLayout(false);
            this.grpImages.PerformLayout();
            this.grpResults.ResumeLayout(false);
            this.grpSourceImage.ResumeLayout(false);
            this.grpSourceImage.PerformLayout();
            this.grpWords.ResumeLayout(false);
            this.grpWords.PerformLayout();
            this.grpContains.ResumeLayout(false);
            this.grpContains.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
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
        private System.Windows.Forms.GroupBox grpSourceImage;
        private System.Windows.Forms.GroupBox grpWords;
        private System.Windows.Forms.TextBox txtImagesCount;
        private System.Windows.Forms.Button btnLoadRecognizeImage;
        private System.Windows.Forms.Button btnSaveRecognizeImage;
        private System.Windows.Forms.OpenFileDialog dlgOpenImage;
        private System.Windows.Forms.Button btnWide;
        private System.Windows.Forms.Button btnNarrow;
        private System.Windows.Forms.PictureBox pbConSymbol;
        private System.Windows.Forms.GroupBox grpContains;
        private System.Windows.Forms.Button btnConPrevious;
        private System.Windows.Forms.Button btnConNext;
        private System.Windows.Forms.PictureBox pbSuccess;
        private System.Windows.Forms.TextBox txtSymbolPath;
        private System.Windows.Forms.TextBox txtConSymbol;
        private System.Windows.Forms.Button btnConSaveImage;
        private System.Windows.Forms.Button btnConSaveAllImages;
        private System.Windows.Forms.Button btnDeleteRecognizeImage;
        private System.Windows.Forms.Button btnPrevRecogImage;
        private System.Windows.Forms.Button btnNextRecogImage;
        private System.Windows.Forms.Button btnImageUpToQueries;
    }
}

