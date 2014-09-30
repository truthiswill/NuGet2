﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NuGet.Client.Resolution;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window, IExecutionLogger
    {
        private readonly Dispatcher _uiDispatcher;
        private DateTime _loadedTime;
        private readonly TimeSpan minimumVisibleTime = TimeSpan.FromMilliseconds(500);
        private FileConflictAction _fileConflictAction;

        public ProgressDialog(FileConflictAction fileConflictAction)
        {
            _fileConflictAction = fileConflictAction;
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            this.Loaded += ProgressDialog_Loaded;
            InitializeComponent();
        }

        void ProgressDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _loadedTime = DateTime.UtcNow;
        }

        public void RequestToClose()
        {
            TimeSpan timeOpened = DateTime.UtcNow - _loadedTime;
            if (timeOpened < minimumVisibleTime)
            {
                Task.Factory.StartNew(() =>
                {
                    System.Threading.Thread.Sleep(minimumVisibleTime - timeOpened);
                    Dispatcher.Invoke(
                        () => this.Close());
                });
            }
            else
            {
                this.Close();
            }
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            var s = string.Format(CultureInfo.CurrentCulture, message, args);

            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.BeginInvoke(
                    new Action<MessageLevel, string>(AddMessage),
                    level,
                    s);
            }
            else 
            {
                AddMessage(level, s);
            }
        }

        private void AddMessage(MessageLevel level, string message)
        {
            Brush messageBrush;

            // select message color based on MessageLevel value.
            // these colors match the colors in the console, which are set in MyHostUI.cs
            if (SystemParameters.HighContrast)
            {
                // Use the plain System brush
                messageBrush = SystemColors.ControlTextBrush;
            }
            else
            {
                switch (level)
                {
                    case MessageLevel.Debug:
                        messageBrush = Brushes.DarkGray;
                        break;

                    case MessageLevel.Error:
                        messageBrush = Brushes.Red;
                        break;

                    case MessageLevel.Warning:
                        messageBrush = Brushes.Magenta;
                        break;

                    default:
                        messageBrush = Brushes.Black;
                        break;
                }
            }

            Paragraph paragraph = null;

            // delay creating the FlowDocument for the RichTextBox
            // the FlowDocument will contain a single Paragraph, which
            // contains all the logging messages.
            if (MessagePane.Document == null)
            {
                MessagePane.Document = new FlowDocument();
                paragraph = new Paragraph();
                MessagePane.Document.Blocks.Add(paragraph);
            }
            else
            {
                // if the FlowDocument has been created before, retrieve 
                // the last paragraph from it.
                paragraph = (Paragraph)MessagePane.Document.Blocks.LastBlock;
            }

            // each message is represented by a Run element
            var run = new Run(message)
            {
                Foreground = messageBrush
            };

            // if the paragraph is non-empty, add a line break before the new message
            if (paragraph.Inlines.Count > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(run);

            // scroll to the end to show the latest message
            MessagePane.ScrollToEnd();
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return _fileConflictAction;
        }
    }
}