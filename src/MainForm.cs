using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace LoanTracker
{
    public sealed class MainForm : Form
    {
        private readonly DataStore store;
        private AppData data;
        private readonly Label principalValue;
        private readonly Label monthValue;
        private readonly DataGridView repaymentsGrid;

        private static readonly Color Canvas = Color.FromArgb(15, 23, 42);
        private static readonly Color Card = Color.FromArgb(24, 34, 55);
        private static readonly Color Border = Color.FromArgb(45, 59, 83);
        private static readonly Color TextMain = Color.FromArgb(241, 245, 249);
        private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
        private static readonly Color Accent = Color.FromArgb(99, 102, 241);

        public MainForm(DataStore dataStore, AppData initialData)
        {
            store = dataStore;
            data = AppCore.Normalize(initialData);
            Text = "Loan Tracker";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(850, 580);
            Size = new Size(1080, 720);
            BackColor = Canvas;
            ForeColor = TextMain;
            Font = new Font("Segoe UI", 9F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(28, 22, 28, 18),
                BackColor = Canvas
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
            root.Controls.Add(titlePanel, 0, 0);
            titlePanel.Controls.Add(new Label
            {
                Text = "Loan Tracker",
                Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            titlePanel.Controls.Add(new Label
            {
                Text = "Keep your loans and upcoming repayments in view.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(2, 42)
            });

            var summary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Canvas,
                Margin = new Padding(0, 8, 0, 12)
            };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.Controls.Add(summary, 0, 1);

            principalValue = AddSummaryCard(summary, 0, "TOTAL LOAN PRINCIPAL");
            monthValue = AddSummaryCard(summary, 1, "REPAYMENTS THIS MONTH");

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Canvas,
                Padding = new Padding(0, 6, 0, 0),
                Margin = Padding.Empty
            };
            root.Controls.Add(actions, 0, 2);

            Button addLoan = CreateButton("+  Add loan", Accent);
            addLoan.Click += delegate { ShowLoanDialog(); };
            actions.Controls.Add(addLoan);

            Button addRepayment = CreateButton("+  Schedule repayment", Color.FromArgb(30, 42, 66));
            addRepayment.Click += delegate { ShowRepaymentDialog(); };
            actions.Controls.Add(addRepayment);

            Button togglePaid = CreateButton("Mark paid / unpaid", Color.FromArgb(30, 42, 66));
            togglePaid.Click += delegate { ToggleSelectedPaidStatus(); };
            actions.Controls.Add(togglePaid);

            Button delete = CreateButton("Delete selected", Color.FromArgb(127, 29, 29));
            delete.Click += delegate { DeleteSelected(); };
            actions.Controls.Add(delete);

            repaymentsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Card,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                GridColor = Border,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            repaymentsGrid.ColumnHeadersDefaultCellStyle.BackColor = Card;
            repaymentsGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            repaymentsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            repaymentsGrid.ColumnHeadersHeight = 38;
            repaymentsGrid.DefaultCellStyle.BackColor = Card;
            repaymentsGrid.DefaultCellStyle.ForeColor = TextMain;
            repaymentsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 65, 95);
            repaymentsGrid.DefaultCellStyle.SelectionForeColor = TextMain;
            repaymentsGrid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            repaymentsGrid.RowTemplate.Height = 40;
            repaymentsGrid.Columns.Add(CreateColumn("DueDate", "DUE DATE", 17));
            repaymentsGrid.Columns.Add(CreateColumn("Lender", "LENDER", 20));
            repaymentsGrid.Columns.Add(CreateColumn("Description", "LOAN", 23));
            repaymentsGrid.Columns.Add(CreateColumn("Amount", "AMOUNT", 17));
            repaymentsGrid.Columns.Add(CreateColumn("Status", "STATUS", 15));
            repaymentsGrid.Columns.Add(CreateColumn("Urgency", "DUE", 8));
            root.Controls.Add(repaymentsGrid, 0, 3);

            Render();
        }

        private static Label AddSummaryCard(TableLayoutPanel parent, int column, string title)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Card,
                Margin = new Padding(0, 0, column == 0 ? 8 : 0, 0),
                Padding = new Padding(17, 13, 12, 10)
            };
            parent.Controls.Add(card, column, 0);
            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(17, 13)
            });
            var value = new Label
            {
                Text = "0.00",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(16, 40)
            };
            card.Controls.Add(value);
            return value;
        }

        private static DataGridViewTextBoxColumn CreateColumn(string name, string header, float fillWeight)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = name,
                FillWeight = fillWeight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private static Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                Height = 36,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = TextMain,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0, 0, 9, 0),
                Cursor = Cursors.Hand
            };
        }

        private void Render()
        {
            principalValue.Text = FormatMoney(AppCore.GetTotalPrincipal(data));
            monthValue.Text = FormatMoney(AppCore.GetRepaymentsDueThisMonth(data, DateTime.Today));

            var rows = data.Repayments
                .OrderBy(repayment => repayment.DueDate, StringComparer.Ordinal)
                .Select(repayment =>
                {
                    Loan loan = data.Loans.First(item => item.Id == repayment.LoanId);
                    DateTime dueDate = DateTime.ParseExact(
                        repayment.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    return new
                    {
                        Repayment = repayment,
                        DueDate = dueDate.ToString("MMM d, yyyy", CultureInfo.CurrentCulture),
                        Lender = loan.Lender,
                        Description = loan.Description,
                        Amount = FormatMoney(repayment.Amount),
                        Status = repayment.IsPaid ? "Paid" : "Unpaid",
                        Urgency = repayment.IsPaid ? "" : GetUrgencyText(dueDate)
                    };
                })
                .ToList();

            repaymentsGrid.Rows.Clear();
            foreach (var item in rows)
            {
                int index = repaymentsGrid.Rows.Add(
                    item.DueDate, item.Lender, item.Description, item.Amount, item.Status, item.Urgency);
                DataGridViewRow row = repaymentsGrid.Rows[index];
                row.Tag = item.Repayment;
                if (!item.Repayment.IsPaid)
                {
                    switch (AppCore.GetDueDateBand(
                        DateTime.ParseExact(item.Repayment.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                        DateTime.Today))
                    {
                        case DueDateBand.Red:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(75, 35, 44);
                            break;
                        case DueDateBand.Yellow:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(75, 62, 30);
                            break;
                        case DueDateBand.Green:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(27, 66, 58);
                            break;
                    }
                }
            }
        }

        private static string GetUrgencyText(DateTime dueDate)
        {
            int days = (dueDate.Date - DateTime.Today).Days;
            if (days < 0)
                return "Overdue";
            if (days == 0)
                return "Today";
            if (days == 1)
                return "1 day";
            return days + " days";
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N2", CultureInfo.CurrentCulture);
        }

        private void ShowLoanDialog()
        {
            using (var dialog = new LoanDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                data.Loans.Add(new Loan
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Lender = dialog.Lender,
                    Description = dialog.Description,
                    Principal = dialog.Principal,
                    CreatedOn = AppCore.DateKey(DateTime.Today)
                });
                SaveAndRender();
            }
        }

        private void ShowRepaymentDialog()
        {
            if (data.Loans.Count == 0)
            {
                MessageBox.Show(this, "Add a loan before scheduling a repayment.",
                    "No loans yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new RepaymentDialog(data.Loans))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                data.Repayments.Add(new Repayment
                {
                    Id = Guid.NewGuid().ToString("N"),
                    LoanId = dialog.SelectedLoan.Id,
                    DueDate = AppCore.DateKey(dialog.DueDate),
                    Amount = dialog.Amount
                });
                SaveAndRender();
            }
        }

        private Repayment GetSelectedRepayment()
        {
            if (repaymentsGrid.SelectedRows.Count == 0)
                return null;
            return repaymentsGrid.SelectedRows[0].Tag as Repayment;
        }

        private void ToggleSelectedPaidStatus()
        {
            Repayment repayment = GetSelectedRepayment();
            if (repayment == null)
            {
                MessageBox.Show(this, "Select a repayment first.",
                    "No repayment selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            repayment.IsPaid = !repayment.IsPaid;
            SaveAndRender();
        }

        private void DeleteSelected()
        {
            Repayment repayment = GetSelectedRepayment();
            if (repayment != null)
            {
                if (MessageBox.Show(this, "Delete this scheduled repayment?",
                    "Delete repayment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                data.Repayments.Remove(repayment);
                SaveAndRender();
                return;
            }

            if (data.Loans.Count == 0)
            {
                MessageBox.Show(this, "There are no loans to delete.",
                    "No loan selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new LoanSelectionDialog(data.Loans))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                Loan loan = dialog.SelectedLoan;
                if (MessageBox.Show(this,
                    "Delete " + loan.Lender + " and all of its scheduled repayments?",
                    "Delete loan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                data.Repayments.RemoveAll(item => item.LoanId == loan.Id);
                data.Loans.Remove(loan);
                SaveAndRender();
            }
        }

        private void SaveAndRender()
        {
            try
            {
                store.Save(data);
                Render();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Your changes could not be saved.\r\n\r\n" + ex.Message,
                    "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class LoanDialog : Form
        {
            private readonly TextBox lenderInput;
            private readonly TextBox descriptionInput;
            private readonly NumericUpDown principalInput;

            public string Lender { get { return lenderInput.Text.Trim(); } }
            public string Description { get { return descriptionInput.Text.Trim(); } }
            public decimal Principal { get { return principalInput.Value; } }

            public LoanDialog()
            {
                Text = "Add loan";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(390, 235);
                BackColor = Canvas;
                ForeColor = TextMain;
                Font = new Font("Segoe UI", 9F);

                AddFieldLabel(this, "Lender", 18, 18);
                lenderInput = AddTextBox(20, 40, 350);
                AddFieldLabel(this, "Loan description (optional)", 18, 82);
                descriptionInput = AddTextBox(20, 104, 350);
                AddFieldLabel(this, "Total loan principal", 18, 146);
                principalInput = new NumericUpDown
                {
                    Location = new Point(20, 168),
                    Width = 180,
                    DecimalPlaces = 2,
                    Maximum = 1000000000000m,
                    Minimum = 0.01m,
                    ThousandsSeparator = true,
                    BackColor = Card,
                    ForeColor = TextMain
                };
                Controls.Add(principalInput);

                var save = new Button { Text = "Save loan", Location = new Point(256, 184), Size = new Size(114, 32), DialogResult = DialogResult.OK };
                save.Click += delegate { if (ValidateInputs()) DialogResult = DialogResult.OK; else DialogResult = DialogResult.None; };
                Controls.Add(save);
                AcceptButton = save;
            }

            private bool ValidateInputs()
            {
                if (String.IsNullOrWhiteSpace(Lender) || Lender.Length > 120 || Description.Length > 200)
                {
                    MessageBox.Show(this, "Enter a lender name (up to 120 characters) and a description up to 200 characters.",
                        "Check loan details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
        }

        private sealed class RepaymentDialog : Form
        {
            private readonly ComboBox loanInput;
            private readonly NumericUpDown amountInput;
            private readonly DateTimePicker dateInput;

            public Loan SelectedLoan { get { return loanInput.SelectedItem as Loan; } }
            public decimal Amount { get { return amountInput.Value; } }
            public DateTime DueDate { get { return dateInput.Value.Date; } }

            public RepaymentDialog(System.Collections.Generic.IList<Loan> loans)
            {
                Text = "Schedule repayment";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(390, 225);
                BackColor = Canvas;
                ForeColor = TextMain;
                Font = new Font("Segoe UI", 9F);

                AddFieldLabel(this, "Loan", 18, 18);
                loanInput = new ComboBox
                {
                    Location = new Point(20, 40),
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FormattingEnabled = true,
                    BackColor = Card,
                    ForeColor = TextMain,
                    DisplayMember = "Lender"
                };
                foreach (Loan loan in loans)
                    loanInput.Items.Add(loan);
                loanInput.Format += delegate(object sender, ListControlConvertEventArgs e)
                {
                    Loan loan = e.ListItem as Loan;
                    if (loan != null)
                        e.Value = String.IsNullOrEmpty(loan.Description)
                            ? loan.Lender
                            : loan.Lender + " - " + loan.Description;
                };
                loanInput.SelectedIndex = 0;
                Controls.Add(loanInput);

                AddFieldLabel(this, "Repayment amount", 18, 82);
                amountInput = new NumericUpDown
                {
                    Location = new Point(20, 104),
                    Width = 160,
                    DecimalPlaces = 2,
                    Maximum = 1000000000000m,
                    Minimum = 0.01m,
                    ThousandsSeparator = true,
                    BackColor = Card,
                    ForeColor = TextMain
                };
                Controls.Add(amountInput);

                AddFieldLabel(this, "Repayment date", 202, 82);
                dateInput = new DateTimePicker
                {
                    Location = new Point(204, 104),
                    Width = 166,
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "MMM d, yyyy",
                    MinDate = new DateTime(1753, 1, 1),
                    MaxDate = new DateTime(9998, 12, 31),
                    Value = DateTime.Today
                };
                Controls.Add(dateInput);

                var save = new Button { Text = "Schedule", Location = new Point(256, 171), Size = new Size(114, 32), DialogResult = DialogResult.OK };
                save.Click += delegate
                {
                    if (SelectedLoan == null)
                    {
                        MessageBox.Show(this, "Select a loan first.", "Check repayment",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                    }
                };
                Controls.Add(save);
                AcceptButton = save;
            }
        }

        private sealed class LoanSelectionDialog : Form
        {
            private readonly ComboBox loanInput;
            public Loan SelectedLoan { get { return loanInput.SelectedItem as Loan; } }

            public LoanSelectionDialog(System.Collections.Generic.IList<Loan> loans)
            {
                Text = "Select loan";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(390, 110);
                BackColor = Canvas;
                ForeColor = TextMain;
                Font = new Font("Segoe UI", 9F);
                loanInput = new ComboBox
                {
                    Location = new Point(20, 18),
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FormattingEnabled = true,
                    BackColor = Card,
                    ForeColor = TextMain
                };
                foreach (Loan loan in loans)
                    loanInput.Items.Add(loan);
                loanInput.Format += delegate(object sender, ListControlConvertEventArgs e)
                {
                    Loan loan = e.ListItem as Loan;
                    if (loan != null)
                        e.Value = loan.Lender + (String.IsNullOrEmpty(loan.Description) ? "" : " - " + loan.Description);
                };
                loanInput.SelectedIndex = 0;
                Controls.Add(loanInput);
                var select = new Button { Text = "Select", Location = new Point(256, 62), Size = new Size(114, 32), DialogResult = DialogResult.OK };
                Controls.Add(select);
                AcceptButton = select;
            }
        }

        private static void AddFieldLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = TextMuted
            });
        }

        private static TextBox AddTextBox(int x, int y, int width)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                BackColor = Card,
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
        }
    }
}
