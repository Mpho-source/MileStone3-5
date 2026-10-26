using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    [ToolboxItem(true)]
    public class BusyButton : Button
    {
        private bool actionInProgress;
        private long lastCompletion;

        protected override void OnClick(EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                base.OnClick(e);
                return;
            }

            // Prevent a queued double-click from submitting the same operation twice.
            long now = Stopwatch.GetTimestamp();
            if (actionInProgress ||
                (lastCompletion != 0 &&
                 now - lastCompletion < Stopwatch.Frequency * 350L / 1000L))
                return;

            actionInProgress = true;
            IDisposable loading = null;
            try
            {
                loading = FormLoadingIndicator.Show(FindForm());
                base.OnClick(e);
            }
            finally
            {
                if (loading != null) loading.Dispose();
                lastCompletion = Stopwatch.GetTimestamp();
                actionInProgress = false;
            }
        }
    }

    internal sealed class FormLoadingIndicator : IDisposable
    {
        private readonly Form owner;
        private readonly Control overlay;
        private readonly bool previousWaitCursor;
        private bool disposed;

        private FormLoadingIndicator(Form form)
        {
            owner = form;
            previousWaitCursor = form.UseWaitCursor;

            var shade = new Panel
            {
                Name = "eduConnectBusyOverlay",
                BackColor = Color.FromArgb(230, 237, 249),
                Bounds = form.ClientRectangle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left |
                         AnchorStyles.Right | AnchorStyles.Bottom,
                Cursor = Cursors.WaitCursor
            };

            var card = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(355, 138)
            };
            card.Location = new Point(
                Math.Max(0, (shade.ClientSize.Width - card.Width) / 2),
                Math.Max(0, (shade.ClientSize.Height - card.Height) / 2));

            var heading = new Label
            {
                Text = "Loading...",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 56, 100),
                Location = new Point(24, 17),
                Size = new Size(290, 40)
            };
            var detail = new Label
            {
                Text = "Please wait while EduConnect processes your request.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(83, 104, 132),
                Location = new Point(24, 60),
                Size = new Size(307, 36)
            };
            var progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 24,
                Location = new Point(24, 110),
                Size = new Size(307, 12)
            };

            card.Controls.Add(heading);
            card.Controls.Add(detail);
            card.Controls.Add(progress);
            shade.Controls.Add(card);
            overlay = shade;
            form.Controls.Add(shade);
            shade.BringToFront();
            form.UseWaitCursor = true;

            // Paint a visible busy state before a synchronous OleDb operation
            // blocks the WinForms message loop; never use Application.DoEvents().
            shade.Refresh();
            card.Refresh();
        }

        public static IDisposable Show(Form form)
        {
            if (form == null || form.IsDisposed || form.Disposing ||
                !form.IsHandleCreated || !form.Visible)
                return null;
            return new FormLoadingIndicator(form);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (owner != null && !owner.IsDisposed && !owner.Disposing)
            {
                if (overlay != null && !overlay.IsDisposed)
                {
                    owner.Controls.Remove(overlay);
                    overlay.Dispose();
                }
                owner.UseWaitCursor = previousWaitCursor;
            }
        }
    }
}
