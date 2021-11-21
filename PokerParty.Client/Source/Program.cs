using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PokerParty.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();
            new Window(1280, 720, "PokerParty");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if !DEBUG
            var ex = (Exception)e.ExceptionObject;

            var report = $"Source: {ex.Source}\nCode: 0x{ex.HResult:X4}\nEntry point: {ex.TargetSite?.Name}\nMessage: {ex.Message}\nStack trace:\n{ex.StackTrace}";

            var page = new TaskDialogPage()
            {
                Caption = "PokerParty - Crash report",
                SizeToContent = true,
                Heading = $"Crash report - 0x{ex.HResult:X4} - {ex.Source}",
                Text = $"Entry point: {ex.TargetSite?.Name}\nError message: {ex.Message}",
                ProgressBar = new TaskDialogProgressBar()
                {
                    State = TaskDialogProgressBarState.Marquee
                },
                Buttons = {
                    TaskDialogButton.Close
                },
                Icon = TaskDialogIcon.ShieldErrorRedBar,
                Expander = new TaskDialogExpander()
                {
                    Text = $"Stack trace:\n{ex.StackTrace}",
                    Position = TaskDialogExpanderPosition.AfterFootnote
                },
                Footnote = new TaskDialogFootnote()
                {
                    Text = "Report is being uploaded to the PokerParty developers. Please do not close this window.",
                    Icon = TaskDialogIcon.ShieldWarningYellowBar
                }
                // TODO: Upload
            };

            page.Buttons[0].Enabled = false;

            Task.Run(async () =>
            {
                using (var fs = File.Open("crashreport.log", FileMode.Create, FileAccess.Write))
                {
                    var bytes = Encoding.UTF8.GetBytes(report);
                    await fs.WriteAsync(bytes);
                    await fs.FlushAsync();
                }

                await Task.Delay(1000);

                page.Buttons[0].Enabled = true;

                page.ProgressBar.State = TaskDialogProgressBarState.Normal;
                page.ProgressBar.Value = 100;

                page.Footnote.Text = "Report successfully uploaded. We will try to resolve this error as fast as possible.";
                page.Footnote.Icon = TaskDialogIcon.ShieldSuccessGreenBar;
            });

            TaskDialog.ShowDialog(page);
#endif

        }

    }
}