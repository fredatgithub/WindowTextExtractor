﻿using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using WindowTextExtractor.Extensions;

namespace WindowTextExtractor.Forms
{
    public partial class MainForm : Form, IMessageFilter
    {
        private readonly int _processId;
        private readonly int _messageId;
        private bool _isButtonTargetTextMouseDown;
        private bool _isButtonTargetPasswordMouseDown;
        private Cursor _targetTextCursor;
        private Cursor _targetPasswordCursor;

        public MainForm()
        {
            InitializeComponent();
            _isButtonTargetTextMouseDown = false;
            _isButtonTargetPasswordMouseDown = false;
            _targetTextCursor = new Cursor(Properties.Resources.TargetText.Handle);
            _targetPasswordCursor = new Cursor(Properties.Resources.TargetPassword.Handle);
            _processId = Process.GetCurrentProcess().Id;
            _messageId = NativeMethods.RegisterWindowMessage("WINDOW_TEXT_EXTRACTOR_HOOK");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Application.AddMessageFilter(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.RemoveMessageFilter(this);
        }

        private void btnTargetText_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_isButtonTargetTextMouseDown)
            {
                _isButtonTargetTextMouseDown = true;
                Cursor.Current = _targetTextCursor;
                if (!TopMost)
                {
                    SendToBack();
                }
            }
        }

        private void btnTargetPassword_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_isButtonTargetPasswordMouseDown)
            {
                _isButtonTargetPasswordMouseDown = true;
                Cursor.Current = _targetPasswordCursor;
                if (!TopMost)
                {
                    SendToBack();
                }
            }
        }

        private void txtContent_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void txtContent_MultilineChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuItemFont_Click(object sender, EventArgs e)
        {
            var dialog = new FontDialog();
            dialog.ShowHelp = false;
            dialog.ShowColor = false;
            dialog.Font = txtContent.Font;
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                txtContent.Font = dialog.Font;
            }
        }

        private void menuItemAlwaysOnTop_Click(object sender, EventArgs e)
        {
            TopMost = !TopMost;
            menuItemAlwaysOnTop.Checked = TopMost;
        }

        private void menuItemAbout_Click(object sender, EventArgs e)
        {
            var dialog = new AboutForm();
            dialog.ShowDialog(this);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeConstants.WM_COPYDATA:
                    {
                        var cds = (CopyDataStruct)Marshal.PtrToStructure(m.LParam, typeof(CopyDataStruct));
                        var password = Marshal.PtrToStringAuto(cds.lpData);
                        txtContent.Text = password;
                        txtContent.ScrollTextToEnd();
                        UpdateStatusBar();
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (_isButtonTargetTextMouseDown || _isButtonTargetPasswordMouseDown)
            {
                switch (m.Msg)
                {
                    case NativeConstants.WM_LBUTTONUP:
                        {
                            _isButtonTargetTextMouseDown = false;
                            _isButtonTargetPasswordMouseDown = false;
                            Cursor.Current = Cursors.Default;
                            if (!TopMost)
                            {
                                BringToFront();
                            }
                        } break;

                    case NativeConstants.WM_MOUSEMOVE:
                        {
                            try
                            {
                                var cursorPosition = System.Windows.Forms.Cursor.Position;
                                var element = AutomationElement.FromPoint(new System.Windows.Point(cursorPosition.X, cursorPosition.Y));
                                if (element != null && element.Current.ProcessId != _processId)
                                {
                                    if (element.Current.IsPassword && _isButtonTargetPasswordMouseDown)
                                    {
                                        var elementHandle = new IntPtr(element.Current.NativeWindowHandle);
                                        NativeMethods.SetHook(Handle, elementHandle, _messageId);
                                        NativeMethods.QueryPasswordEdit();
                                        NativeMethods.UnsetHook(Handle, elementHandle);
                                    }

                                    if (!element.Current.IsPassword && _isButtonTargetTextMouseDown)
                                    {
                                        var text = element.GetTextFromConsole() ?? element.GetTextFromWindow();
                                        txtContent.Text = text;
                                        txtContent.ScrollTextToEnd();
                                        UpdateStatusBar();
                                    }
                                }
                            }
                            catch
                            {
                            }
                        } break;
                }
            }

            return false;
        }

        private void UpdateStatusBar()
        {
            lblTotalChars.Text = "Total Chars: " + txtContent.Text.Length;
            lblTotalLines.Text = "Total Lines: " + txtContent.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
        }
    }
}