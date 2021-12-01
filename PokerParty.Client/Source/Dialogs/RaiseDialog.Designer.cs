namespace PokerParty.Client.Dialogs
{
    partial class RaiseDialog
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
            this.amountValue = new System.Windows.Forms.NumericUpDown();
            this.acceptButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.currentBetLabel = new System.Windows.Forms.Label();
            this.amountLabel = new System.Windows.Forms.Label();
            this.newBetLabel = new System.Windows.Forms.Label();
            this.currentBetValue = new System.Windows.Forms.Label();
            this.newBetValue = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.amountValue)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // amountValue
            // 
            this.amountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.amountValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.amountValue.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.amountValue.Location = new System.Drawing.Point(109, 93);
            this.amountValue.Name = "amountValue";
            this.amountValue.Size = new System.Drawing.Size(213, 50);
            this.amountValue.TabIndex = 0;
            this.amountValue.ValueChanged += new System.EventHandler(this.amountValue_ValueChanged);
            // 
            // acceptButton
            // 
            this.acceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptButton.Location = new System.Drawing.Point(262, 176);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(75, 23);
            this.acceptButton.TabIndex = 1;
            this.acceptButton.Text = "Accept";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.acceptButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(181, 176);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // currentBetLabel
            // 
            this.currentBetLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentBetLabel.Location = new System.Drawing.Point(3, 0);
            this.currentBetLabel.Name = "currentBetLabel";
            this.currentBetLabel.Size = new System.Drawing.Size(100, 45);
            this.currentBetLabel.TabIndex = 3;
            this.currentBetLabel.Text = "Current bet";
            this.currentBetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // amountLabel
            // 
            this.amountLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.amountLabel.Location = new System.Drawing.Point(3, 90);
            this.amountLabel.Name = "amountLabel";
            this.amountLabel.Size = new System.Drawing.Size(100, 56);
            this.amountLabel.TabIndex = 4;
            this.amountLabel.Text = "Amount";
            this.amountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // newBetLabel
            // 
            this.newBetLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.newBetLabel.Location = new System.Drawing.Point(3, 45);
            this.newBetLabel.Name = "newBetLabel";
            this.newBetLabel.Size = new System.Drawing.Size(100, 45);
            this.newBetLabel.TabIndex = 5;
            this.newBetLabel.Text = "New bet";
            this.newBetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // currentBetValue
            // 
            this.currentBetValue.AutoSize = true;
            this.currentBetValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.currentBetValue.Location = new System.Drawing.Point(109, 0);
            this.currentBetValue.Name = "currentBetValue";
            this.currentBetValue.Size = new System.Drawing.Size(38, 45);
            this.currentBetValue.TabIndex = 6;
            this.currentBetValue.Text = "0";
            // 
            // newBetValue
            // 
            this.newBetValue.AutoSize = true;
            this.newBetValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.newBetValue.ForeColor = System.Drawing.Color.Green;
            this.newBetValue.Location = new System.Drawing.Point(109, 45);
            this.newBetValue.Name = "newBetValue";
            this.newBetValue.Size = new System.Drawing.Size(38, 45);
            this.newBetValue.TabIndex = 7;
            this.newBetValue.Text = "0";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.currentBetLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.amountLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.newBetValue, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.amountValue, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.newBetLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.currentBetValue, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(325, 158);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // RaiseDialog
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(349, 211);
            this.ControlBox = false;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.acceptButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RaiseDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Raise bet";
            ((System.ComponentModel.ISupportInitialize)(this.amountValue)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NumericUpDown amountValue;
        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label currentBetLabel;
        private System.Windows.Forms.Label amountLabel;
        private System.Windows.Forms.Label newBetLabel;
        private System.Windows.Forms.Label currentBetValue;
        private System.Windows.Forms.Label newBetValue;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}