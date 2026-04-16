using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XlentWPF.ViewModels;

namespace XlentWPF.Components
{
    public partial class TranscriptPanel : UserControl
    {
        public TranscriptPanel()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel vm)
            {
                vm.Transcript.Segments.CollectionChanged += OnSegmentsChanged;
                // Also scroll when partial text appears (live typing indicator)
                vm.Transcript.PropertyChanged += (_, pe) =>
                {
                    if (pe.PropertyName == nameof(TranscriptViewModel.HasPartialText))
                        ScrollToBottom();
                };
            }

            // Unsubscribe from the old view model to avoid leaks
            if (e.OldValue is MainViewModel old)
                old.Transcript.Segments.CollectionChanged -= OnSegmentsChanged;
        }

        private void OnSegmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(
                () => TranscriptScroll.ScrollToEnd(),
                DispatcherPriority.Background);
        }

        private void PiiSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            PiiSettingsPopup.IsOpen = !PiiSettingsPopup.IsOpen;
        }
    }
}
