using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SHDTimer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The possible time values to shut down after.
		/// </summary>
		public static readonly KeyValuePair<int, string>[] Times = {
			new KeyValuePair<int, string>(0, "Choose Delay"),
			new KeyValuePair<int, string>(900, "15 minutes"),
			new KeyValuePair<int, string>(1800, "30 minutes"),
			new KeyValuePair<int, string>(3600, "1 hour"),
			new KeyValuePair<int, string>(5400, "1 hour 30 minutes"),
			new KeyValuePair<int, string>(7200, "2 hours"),
		};

		/// <summary>
		/// Is this instance ticking down at the moment?
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is counting; otherwise, <c>false</c>.
		/// </value>
		public bool IsCounting { get; protected set; }

		/// <summary>
		/// The timer used by this instance.
		/// </summary>
		private DispatcherTimer m_dTimer;
		/// <summary>
		/// When did the timer get paused? (remaining seconds)
		/// </summary>
		private int m_resumeTime = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow()
		{
			m_dTimer = new DispatcherTimer();
			m_dTimer.Interval = TimeSpan.FromMilliseconds(1000);
			m_dTimer.Tick += OnTimerTick;

			InitializeComponent();

			boxTime.SelectedValuePath = "Key";
			boxTime.DisplayMemberPath = "Value";

			foreach (KeyValuePair<int, string> t in Times)
				boxTime.Items.Add(t);

			boxTime.SelectedIndex = 0;
			btnPause.IsEnabled = false;
		}



		#region Public Methods
		/// <summary>
		/// Starts or continues the counting.
		/// </summary>
		public void Run()
		{
			if (IsCounting) return;
			int time = (m_resumeTime == 0 ? (int)boxTime.SelectedValue : m_resumeTime);
			if (Execute(new ProcessStartInfo("shutdown", string.Format("-s -c \"Shutdown in: {0} minutes\" -t {1}", (time / 60).ToString(), time.ToString()))))
			{
				pgBar.Maximum = (int)boxTime.SelectedValue;
				TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
				IsCounting = true;
				boxTime.IsEnabled = false;
				btnGo.Content = "Cancel";
				btnPause.IsEnabled = true;
				m_dTimer.Start();
			}
		}

		/// <summary>
		/// Cancels the current shutdown process.
		/// </summary>
		public void Cancel()
		{
			if (!IsCounting) return;
			if (!Execute(new ProcessStartInfo("shutdown", "-a")))
				TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
			else
			{
				m_dTimer.Stop();
				pgBar.Value = 0;
				m_resumeTime = 0;
				txtRemained.Text = string.Empty;
				TaskbarItemInfo.ProgressValue = 0;
				IsCounting = false;
				boxTime.IsEnabled = true;
				btnGo.Content = "Start";
				btnPause.IsEnabled = false;
			}
		}

		/// <summary>
		/// Pauses the current shutdown process.
		/// </summary>
		public void Pause()
		{
			if (!IsCounting) return;
			if (!Execute(new ProcessStartInfo("shutdown", "-a")))
				TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
			else
			{
				m_dTimer.Stop();
				TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
				m_resumeTime = (int)pgBar.Maximum - (int)pgBar.Value;
				IsCounting = false;
				btnGo.Content = "Resume";
				btnPause.IsEnabled = false;
			}
		}
		#endregion



		#region Private Methods
		/// <summary>
		/// Executes the specified Windows process.
		/// </summary>
		/// <param name="stInf">The start info of the process.</param>
		/// <returns><c>true</c> if the process has been started successfully; otherwise, <c>false</c>.</returns>
		private bool Execute(ProcessStartInfo stInf)
		{
			try
			{
				stInf.Verb = "runas";
				Process.Start(stInf);
			}
			catch (Win32Exception exc)
			{
				if (exc.NativeErrorCode == 1223)
				{
					ErrDialog dialog = new ErrDialog("You need to provide administrator rights for the program to work.");
					dialog.ShowDialog();
				}

				return false;
			}

			return true;
		}
		#endregion



		#region Events
		private void OnTimerTick(object sender, EventArgs e)
		{
			// Increase every progress by one second or percent.
			pgBar.Value++;
			TaskbarItemInfo.ProgressValue = (pgBar.Value / (pgBar.Maximum / 100)) / 100;
			// Display the remaining time in minutes.
			txtRemained.Text = string.Format("{0} minutes", (int)((pgBar.Maximum - pgBar.Value) / 60));

			// Stop the timer 5 seconds before the shutdown and display a "Shutting down..." message.
			if (pgBar.Value >= pgBar.Maximum - 5)
			{
				m_dTimer.Stop();
				txtRemained.Text = "Shutting down...";
			}
		}

		private void boxTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Enabling / Disabling the button if there is no selected time.
			btnGo.IsEnabled = ((int)boxTime.SelectedValue != 0);
        }

		private void btnGo_Click(object sender, RoutedEventArgs e)
		{
			// Resume, cancel or start the shutdown process.
			if (IsCounting)
				Cancel();
			else
				Run();
		}

		private void btnPause_Click(object sender, RoutedEventArgs e)
		{
			Pause();
		}
		#endregion
	}
}
