using System.Windows;

namespace SHDTimer
{
	/// <summary>
	/// Interaction logic for ErrDialog.xaml
	/// </summary>
	public partial class ErrDialog : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ErrDialog"/> class.
		/// </summary>
		/// <param name="dialogText">The dialog text to be displayed.</param>
		public ErrDialog(string dialogText = null)
		{
			InitializeComponent();
			lblText.Text = dialogText ?? string.Empty;
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
