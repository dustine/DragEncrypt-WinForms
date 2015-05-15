using System.ComponentModel;
using System.Windows.Forms;

namespace DragEncrypt
{
    partial class MainProcess
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.fileSelectPanel = new System.Windows.Forms.Panel();
            this.changeFileButton = new System.Windows.Forms.Button();
            this.optionsGroupBox = new System.Windows.Forms.GroupBox();
            this.deleteFileCheckBox = new System.Windows.Forms.CheckBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.fileLabel = new System.Windows.Forms.Label();
            this.submitPanel = new System.Windows.Forms.Panel();
            this.submitButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.passwordPanel = new System.Windows.Forms.Panel();
            this.showPasswordHoldButton = new System.Windows.Forms.Button();
            this.passwordBox = new System.Windows.Forms.TextBox();
            this.aboutLinkLabel = new System.Windows.Forms.LinkLabel();
            this.fileLocationLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.fileSelectPanel.SuspendLayout();
            this.optionsGroupBox.SuspendLayout();
            this.submitPanel.SuspendLayout();
            this.passwordPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.fileSelectPanel, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.optionsGroupBox, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.passwordLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.fileLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.submitPanel, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.passwordPanel, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.aboutLinkLabel, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 129);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // fileSelectPanel
            // 
            this.fileSelectPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileSelectPanel.Controls.Add(this.fileLocationLabel);
            this.fileSelectPanel.Controls.Add(this.changeFileButton);
            this.fileSelectPanel.Location = new System.Drawing.Point(58, 0);
            this.fileSelectPanel.Margin = new System.Windows.Forms.Padding(0);
            this.fileSelectPanel.Name = "fileSelectPanel";
            this.fileSelectPanel.Size = new System.Drawing.Size(328, 25);
            this.fileSelectPanel.TabIndex = 2;
            // 
            // changeFileButton
            // 
            this.changeFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.changeFileButton.BackColor = System.Drawing.SystemColors.Control;
            this.changeFileButton.FlatAppearance.BorderSize = 0;
            this.changeFileButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.changeFileButton.ForeColor = System.Drawing.SystemColors.Control;
            this.changeFileButton.Image = global::DragEncrypt.Properties.Resources.folder_16;
            this.changeFileButton.Location = new System.Drawing.Point(304, 1);
            this.changeFileButton.Margin = new System.Windows.Forms.Padding(0);
            this.changeFileButton.Name = "changeFileButton";
            this.changeFileButton.Size = new System.Drawing.Size(21, 22);
            this.changeFileButton.TabIndex = 1;
            this.changeFileButton.TabStop = false;
            this.changeFileButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.changeFileButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            // 
            // optionsGroupBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.optionsGroupBox, 2);
            this.optionsGroupBox.Controls.Add(this.deleteFileCheckBox);
            this.optionsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionsGroupBox.Location = new System.Drawing.Point(3, 59);
            this.optionsGroupBox.Name = "optionsGroupBox";
            this.optionsGroupBox.Size = new System.Drawing.Size(380, 38);
            this.optionsGroupBox.TabIndex = 0;
            this.optionsGroupBox.TabStop = false;
            this.optionsGroupBox.Text = "Options";
            // 
            // deleteFileCheckBox
            // 
            this.deleteFileCheckBox.AutoSize = true;
            this.deleteFileCheckBox.Location = new System.Drawing.Point(9, 19);
            this.deleteFileCheckBox.Name = "deleteFileCheckBox";
            this.deleteFileCheckBox.Size = new System.Drawing.Size(166, 17);
            this.deleteFileCheckBox.TabIndex = 0;
            this.deleteFileCheckBox.Text = "Delete the targetted file safely";
            this.deleteFileCheckBox.UseVisualStyleBackColor = true;
            // 
            // passwordLabel
            // 
            this.passwordLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordLabel.Location = new System.Drawing.Point(2, 25);
            this.passwordLabel.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(56, 31);
            this.passwordLabel.TabIndex = 1000;
            this.passwordLabel.Text = "Password:";
            this.passwordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // fileLabel
            // 
            this.fileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileLabel.Location = new System.Drawing.Point(2, 0);
            this.fileLabel.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(56, 25);
            this.fileLabel.TabIndex = 1001;
            this.fileLabel.Text = "File:";
            this.fileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // submitPanel
            // 
            this.submitPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.submitPanel.Controls.Add(this.submitButton);
            this.submitPanel.Controls.Add(this.progressBar);
            this.submitPanel.Location = new System.Drawing.Point(61, 103);
            this.submitPanel.Name = "submitPanel";
            this.submitPanel.Size = new System.Drawing.Size(322, 23);
            this.submitPanel.TabIndex = 2;
            // 
            // submitButton
            // 
            this.submitButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.submitButton.Location = new System.Drawing.Point(247, 0);
            this.submitButton.Margin = new System.Windows.Forms.Padding(0);
            this.submitButton.MaximumSize = new System.Drawing.Size(75, 23);
            this.submitButton.MinimumSize = new System.Drawing.Size(75, 23);
            this.submitButton.Name = "submitButton";
            this.submitButton.Size = new System.Drawing.Size(75, 23);
            this.submitButton.TabIndex = 0;
            this.submitButton.Text = "Do";
            this.submitButton.UseVisualStyleBackColor = true;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(247, 0);
            this.progressBar.MaximumSize = new System.Drawing.Size(75, 23);
            this.progressBar.MinimumSize = new System.Drawing.Size(75, 23);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(75, 23);
            this.progressBar.TabIndex = 1000;
            this.progressBar.Visible = false;
            // 
            // passwordPanel
            // 
            this.passwordPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordPanel.Controls.Add(this.showPasswordHoldButton);
            this.passwordPanel.Controls.Add(this.passwordBox);
            this.passwordPanel.Location = new System.Drawing.Point(58, 25);
            this.passwordPanel.Margin = new System.Windows.Forms.Padding(0);
            this.passwordPanel.Name = "passwordPanel";
            this.passwordPanel.Size = new System.Drawing.Size(328, 31);
            this.passwordPanel.TabIndex = 0;
            // 
            // showPasswordHoldButton
            // 
            this.showPasswordHoldButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.showPasswordHoldButton.BackColor = System.Drawing.SystemColors.Control;
            this.showPasswordHoldButton.FlatAppearance.BorderSize = 0;
            this.showPasswordHoldButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.WindowText;
            this.showPasswordHoldButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.showPasswordHoldButton.ForeColor = System.Drawing.SystemColors.Control;
            this.showPasswordHoldButton.Image = global::DragEncrypt.Properties.Resources.black_eye16;
            this.showPasswordHoldButton.Location = new System.Drawing.Point(304, 6);
            this.showPasswordHoldButton.Margin = new System.Windows.Forms.Padding(0);
            this.showPasswordHoldButton.Name = "showPasswordHoldButton";
            this.showPasswordHoldButton.Size = new System.Drawing.Size(21, 20);
            this.showPasswordHoldButton.TabIndex = 1;
            this.showPasswordHoldButton.TabStop = false;
            this.showPasswordHoldButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.showPasswordHoldButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.showPasswordHoldButton.UseVisualStyleBackColor = false;
            this.showPasswordHoldButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.showPasswordHoldButton_MouseDown);
            this.showPasswordHoldButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.showPasswordHoldButton_MouseUp);
            // 
            // passwordBox
            // 
            this.passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.passwordBox.Location = new System.Drawing.Point(0, 6);
            this.passwordBox.Margin = new System.Windows.Forms.Padding(0);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.Size = new System.Drawing.Size(304, 20);
            this.passwordBox.TabIndex = 0;
            this.passwordBox.UseSystemPasswordChar = true;
            // 
            // aboutLinkLabel
            // 
            this.aboutLinkLabel.Location = new System.Drawing.Point(3, 100);
            this.aboutLinkLabel.Name = "aboutLinkLabel";
            this.aboutLinkLabel.Size = new System.Drawing.Size(52, 26);
            this.aboutLinkLabel.TabIndex = 1002;
            this.aboutLinkLabel.TabStop = true;
            this.aboutLinkLabel.Text = "About...";
            this.aboutLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.aboutLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutLinkLabel_LinkClicked);
            // 
            // fileLocationLabel
            // 
            this.fileLocationLabel.Location = new System.Drawing.Point(0, 1);
            this.fileLocationLabel.Name = "fileLocationLabel";
            this.fileLocationLabel.Size = new System.Drawing.Size(304, 24);
            this.fileLocationLabel.TabIndex = 0;
            this.fileLocationLabel.Text = "label1";
            this.fileLocationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainProcess
            // 
            this.AcceptButton = this.submitButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(384, 129);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 168);
            this.Name = "MainProcess";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "DragEncrypt";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.fileSelectPanel.ResumeLayout(false);
            this.optionsGroupBox.ResumeLayout(false);
            this.optionsGroupBox.PerformLayout();
            this.submitPanel.ResumeLayout(false);
            this.passwordPanel.ResumeLayout(false);
            this.passwordPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Label passwordLabel;
        private Button showPasswordHoldButton;
        private TextBox passwordBox;
        private GroupBox optionsGroupBox;
        private CheckBox deleteFileCheckBox;
        private Panel passwordPanel;
        private Panel submitPanel;
        private Button submitButton;
        private ProgressBar progressBar;
        private Label fileLabel;
        private Panel fileSelectPanel;
        private Button changeFileButton;
        private Label fileLocationLabel;
        private LinkLabel aboutLinkLabel;
    }
}