using System;
using System.Windows.Forms;

namespace PokerParty.Client.Dialogs
{
    public partial class RaiseDialog : Form
    {
        public int CurrentTableBet { get; set; }
        public int CurrentPlayerBet { get; set; }
        public int MaxPlayerBet { get; set; }
        public int BetAmount { get; set; }

        public RaiseDialog()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            amountValue.Minimum = Math.Max(0, CurrentTableBet - CurrentPlayerBet + 20);
            amountValue.Maximum = MaxPlayerBet;

            currentBetValue.Text = CurrentTableBet.ToString();

            base.OnShown(e);
        }

        private void amountValue_ValueChanged(object sender, EventArgs e)
        {
            newBetValue.Text = (CurrentPlayerBet + (int)amountValue.Value).ToString();
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            BetAmount = (int)amountValue.Value;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
