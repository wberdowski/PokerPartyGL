﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PokerParty.Client
{
    public class Program
    {
        internal static bool primary;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();

            if(args.Length > 0 && args[0] == "primary")
            {
                primary = true;
            }

            new Window(1280, 720, "PokerParty");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            return;
#endif
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
                    TaskDialogButton.Close,
                    new TaskDialogButton("Open crash report file")
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
                },
                // TODO: Upload error report
            };

            page.Buttons[0].Enabled = false;
            page.Buttons[1].Enabled = false;

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
                page.Buttons[1].Enabled = true;
                page.DefaultButton = page.Buttons[0];

                page.ProgressBar.State = TaskDialogProgressBarState.Normal;
                page.ProgressBar.Value = 100;

                page.Footnote.Text = "Report successfully uploaded. We will try to resolve this error as fast as possible.";
                page.Footnote.Icon = TaskDialogIcon.ShieldSuccessGreenBar;
            });

            var result = TaskDialog.ShowDialog(page);

            if (result == page.Buttons[1])
            {
                ProcessStartInfo psi = new ProcessStartInfo("crashreport.log");
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
        }

    }
}