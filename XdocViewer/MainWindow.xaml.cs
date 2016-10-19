using System;
using System.Collections.Generic;
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

namespace XdocViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		public MainWindow()
		{
			InitializeComponent();
            if (ViewModel != null)
            {
                ViewModel.HighlightSearchResult += ViewModel_HighlightSearchResult;
                ViewModel.ReplaceSearchResult += ViewModel_ReplaceSearchResult;
            }
		}

        private void ViewModel_ReplaceSearchResult(object sender, System.Text.RegularExpressions.Match e)
        {
            txtMain.Focus();
            txtMain.SelectionStart = e.Index;
            txtMain.SelectionLength = e.Length;
            txtMain.SelectedText = ViewModel.ReplaceTerm;
        }

        /// <summary>
        /// highlight the text selected by the match.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModel_HighlightSearchResult(object sender, System.Text.RegularExpressions.Match e)
        {
            txtMain.Focus();
            txtMain.SelectionStart  = e.Index;
            txtMain.SelectionLength = e.Length;
        }

        /// <summary>
        /// gets the current view model 
        /// </summary>
        public XDocViewModel ViewModel {  get { return this.DataContext as XDocViewModel; } }

        /// <summary>
        /// updates the status text to show the current row/column position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void txtMain_SelectionChanged(object sender, RoutedEventArgs e)
		{
			int row = txtMain.GetLineIndexFromCharacterIndex(txtMain.SelectionStart);
			int col = txtMain.SelectionStart - txtMain.GetCharacterIndexFromLineIndex(row);
			ViewModel.Status = $"Position: Row {row} Col {col}";
		}

        /// <summary>
        /// updates ViewModel.IsChanged when the text changes to indicate it has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtMain_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!ViewModel.IsBusy)
            // record that the text is changed;
            this.ViewModel.IsChanged = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // warn the user if the contents have changed
            if (this.ViewModel.IsChanged)
            {
                var response = System.Windows.MessageBox.Show(this, $"The contents of {ViewModel.FileName} have changed.\r\nContinue? Changes will be lost.", "Save Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (response == MessageBoxResult.Cancel)
                {
                    // cancel the event
                    e.Cancel = true;
                }
            }
        }
    }
}
