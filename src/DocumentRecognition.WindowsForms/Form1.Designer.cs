namespace DocumentRecognition.WindowsForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            button1 = new Button();
            tabControl1 = new TabControl();
            tabPageTitleBlock = new TabPage();
            dgvSignatures = new DataGridView();
            colSignatory = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colDate = new DataGridViewTextBoxColumn();
            lblSignatures = new Label();
            dgvTitleBlock = new DataGridView();
            colField = new DataGridViewTextBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            tabPageRequirements = new TabPage();
            txtTechnicalRequirements = new TextBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabControl1.SuspendLayout();
            tabPageTitleBlock.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSignatures).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTitleBlock).BeginInit();
            tabPageRequirements.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(620, 536);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            button1.Location = new Point(644, 519);
            button1.Name = "button1";
            button1.Size = new Size(458, 29);
            button1.TabIndex = 1;
            button1.Text = "Выбрать файл";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPageTitleBlock);
            tabControl1.Controls.Add(tabPageRequirements);
            tabControl1.Location = new Point(644, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(458, 500);
            tabControl1.TabIndex = 2;
            // 
            // tabPageTitleBlock
            // 
            tabPageTitleBlock.Controls.Add(dgvSignatures);
            tabPageTitleBlock.Controls.Add(lblSignatures);
            tabPageTitleBlock.Controls.Add(dgvTitleBlock);
            tabPageTitleBlock.Location = new Point(4, 24);
            tabPageTitleBlock.Name = "tabPageTitleBlock";
            tabPageTitleBlock.Padding = new Padding(6);
            tabPageTitleBlock.Size = new Size(450, 472);
            tabPageTitleBlock.TabIndex = 0;
            tabPageTitleBlock.Text = "Основная надпись";
            tabPageTitleBlock.UseVisualStyleBackColor = true;
            // 
            // dgvSignatures
            // 
            dgvSignatures.AllowUserToAddRows = false;
            dgvSignatures.AllowUserToDeleteRows = false;
            dgvSignatures.AllowUserToResizeRows = false;
            dgvSignatures.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSignatures.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSignatures.BackgroundColor = SystemColors.Window;
            dgvSignatures.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSignatures.Columns.AddRange(new DataGridViewColumn[] { colSignatory, colName, colDate });
            dgvSignatures.Location = new Point(6, 251);
            dgvSignatures.MultiSelect = false;
            dgvSignatures.Name = "dgvSignatures";
            dgvSignatures.ReadOnly = true;
            dgvSignatures.RowHeadersVisible = false;
            dgvSignatures.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSignatures.Size = new Size(438, 215);
            dgvSignatures.TabIndex = 2;
            // 
            // colSignatory
            // 
            colSignatory.FillWeight = 40F;
            colSignatory.HeaderText = "Должность";
            colSignatory.Name = "colSignatory";
            colSignatory.ReadOnly = true;
            colSignatory.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colName
            // 
            colName.FillWeight = 40F;
            colName.HeaderText = "ФИО";
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colDate
            // 
            colDate.FillWeight = 20F;
            colDate.HeaderText = "Дата";
            colDate.Name = "colDate";
            colDate.ReadOnly = true;
            colDate.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // lblSignatures
            // 
            lblSignatures.AutoSize = true;
            lblSignatures.Location = new Point(6, 232);
            lblSignatures.Name = "lblSignatures";
            lblSignatures.Size = new Size(59, 15);
            lblSignatures.TabIndex = 1;
            lblSignatures.Text = "Подписи:";
            // 
            // dgvTitleBlock
            // 
            dgvTitleBlock.AllowUserToAddRows = false;
            dgvTitleBlock.AllowUserToDeleteRows = false;
            dgvTitleBlock.AllowUserToResizeRows = false;
            dgvTitleBlock.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvTitleBlock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTitleBlock.BackgroundColor = SystemColors.Window;
            dgvTitleBlock.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTitleBlock.Columns.AddRange(new DataGridViewColumn[] { colField, colValue });
            dgvTitleBlock.Location = new Point(6, 6);
            dgvTitleBlock.MultiSelect = false;
            dgvTitleBlock.Name = "dgvTitleBlock";
            dgvTitleBlock.ReadOnly = true;
            dgvTitleBlock.RowHeadersVisible = false;
            dgvTitleBlock.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTitleBlock.Size = new Size(438, 220);
            dgvTitleBlock.TabIndex = 0;
            // 
            // colField
            // 
            colField.FillWeight = 45F;
            colField.HeaderText = "Поле";
            colField.Name = "colField";
            colField.ReadOnly = true;
            colField.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colValue
            // 
            colValue.FillWeight = 55F;
            colValue.HeaderText = "Значение";
            colValue.Name = "colValue";
            colValue.ReadOnly = true;
            colValue.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // tabPageRequirements
            // 
            tabPageRequirements.Controls.Add(txtTechnicalRequirements);
            tabPageRequirements.Location = new Point(4, 24);
            tabPageRequirements.Name = "tabPageRequirements";
            tabPageRequirements.Padding = new Padding(6);
            tabPageRequirements.Size = new Size(450, 472);
            tabPageRequirements.TabIndex = 1;
            tabPageRequirements.Text = "Технические требования";
            tabPageRequirements.UseVisualStyleBackColor = true;
            // 
            // txtTechnicalRequirements
            // 
            txtTechnicalRequirements.BackColor = SystemColors.Window;
            txtTechnicalRequirements.Dock = DockStyle.Fill;
            txtTechnicalRequirements.Location = new Point(6, 6);
            txtTechnicalRequirements.Multiline = true;
            txtTechnicalRequirements.Name = "txtTechnicalRequirements";
            txtTechnicalRequirements.ReadOnly = true;
            txtTechnicalRequirements.ScrollBars = ScrollBars.Vertical;
            txtTechnicalRequirements.Size = new Size(438, 460);
            txtTechnicalRequirements.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1114, 560);
            Controls.Add(tabControl1);
            Controls.Add(button1);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Document Recognition";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabControl1.ResumeLayout(false);
            tabPageTitleBlock.ResumeLayout(false);
            tabPageTitleBlock.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSignatures).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTitleBlock).EndInit();
            tabPageRequirements.ResumeLayout(false);
            tabPageRequirements.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private Button button1;
        private TabControl tabControl1;
        private TabPage tabPageTitleBlock;
        private DataGridView dgvTitleBlock;
        private DataGridViewTextBoxColumn colField;
        private DataGridViewTextBoxColumn colValue;
        private Label lblSignatures;
        private DataGridView dgvSignatures;
        private DataGridViewTextBoxColumn colSignatory;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colDate;
        private TabPage tabPageRequirements;
        private TextBox txtTechnicalRequirements;
    }
}
