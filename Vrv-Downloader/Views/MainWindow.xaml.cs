using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using VrvDownloader.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace VrvDownloader.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _vm;
		public MainWindow()
		{
			DataContext = _vm = new MainWindowViewModel();
			InitializeComponent();
		}

		private void SaveButtonClick(object sender, RoutedEventArgs e)
		{
			// Create OpenFileDialog
			var dialog = new FolderBrowserDialog();
			var result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.Cancel || dialog.SelectedPath is null) return;
			_vm.SavePath = dialog.SelectedPath;
		}

		private async void button_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(_vm.Url))
			{
				MessageBox.Show("Please, put a URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			if (!Uri.TryCreate(_vm.Url, UriKind.Absolute, out var uri) || null == uri)
			{
				//Invalid URL
				MessageBox.Show("Please, put a valid URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			//if (string.IsNullOrEmpty(_vm.SavePath))
			//{
				//MessageBox.Show("Please, put a save path.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				//return;
			//}
			if (SubtitlesCheckBox.IsChecked ?? false)
			{

				if (string.IsNullOrEmpty(_vm.Language))
				{
					MessageBox.Show("Please, choose a language.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
				if (string.IsNullOrEmpty(_vm.Format))
				{
					MessageBox.Show("Please, choose a sub format.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
				_vm.AreSubtitlesEnabled = true;
			}
            await _vm.Download();
        }

		private void AboutButtonClick(object sender, RoutedEventArgs e)
		{
			var aboutWindow = new AboutWindow { Owner = this };
			aboutWindow.ShowDialog();
		}

        private void Prem_Checked(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow(_vm) { Owner = this };
            loginWindow.ShowDialog();
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}