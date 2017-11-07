using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class BackgroundWorkerExample : Form
    {
        private int highestPercentageReached = 0;
        private System.Windows.Forms.Button startAsyncButton;
        private System.Windows.Forms.Button cancelAsyncButton;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;

        public BackgroundWorkerExample()
        {
            InitializeComponent();

            InitializeBackgroundWorker();
        }

        // Set up the BackgroundWorker object by 
        // attaching event handlers. 
        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork +=
                new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
            backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged +=
                new ProgressChangedEventHandler(
            backgroundWorker1_ProgressChanged);
        }

        private void startAsyncButton_Click(System.Object sender,
            System.EventArgs e)
        {
            // Reset the text in the result label.
            resultLabel.Text = String.Empty;

            // Disable the Start button until 
            // the asynchronous operation is done.
            this.startAsyncButton.Enabled = false;

            // Enable the Cancel button while 
            // the asynchronous operation runs.
            this.cancelAsyncButton.Enabled = true;

            // Reset the variable for percentage tracking.
            highestPercentageReached = 0;

            // Start the asynchronous operation.
            backgroundWorker1.RunWorkerAsync();
        }

        private void cancelAsyncButton_Click(System.Object sender,
            System.EventArgs e)
        {
            // Cancel the asynchronous operation.
            this.backgroundWorker1.CancelAsync();

            // Disable the Cancel button.
            cancelAsyncButton.Enabled = false;
        }

        // This event handler is where the actual,
        // potentially time-consuming work is done.
        private void backgroundWorker1_DoWork(object sender,
            DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;

            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            //e.Result = ComputeFibonacci((int)e.Argument, worker, e);

            e.Result = CheckUserMachine(worker, e);

        }

        // This event handler deals with the results of the
        // background operation.
        private void backgroundWorker1_RunWorkerCompleted(
            object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
                resultLabel.Text = "Canceled";
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                resultLabel.Text = e.Result.ToString();
            }

            // Enable the Start button.
            startAsyncButton.Enabled = true;

            // Disable the Cancel button.
            cancelAsyncButton.Enabled = false;
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
        }

        // This is the method that does the actual work and
        // reports progress as it does its work.

        public bool CheckUserMachine(BackgroundWorker worker, DoWorkEventArgs e)
        {
            bool isdocker = Class1.CheckdockerInstallation();
            bool ishyperV = Class1.CheckWinFeatureStatus(String.Concat(Class1.getWinFeature, Class1.hyperV), "dism.exe");
            bool isContainer = Class1.CheckWinFeatureStatus(String.Concat(Class1.getWinFeature, Class1.wincontainerFeature), "dism.exe");
            string message = "Do you want to install Docker for windows and enable hyperV and Windows container feature?";
            string caption = "Docker Environment Setup Alert";
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.YesNo;
            System.Windows.Forms.DialogResult result;
            Class1 class1 = new Class1();

            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                if (!isdocker)
                {
                    result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);

                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        // Report progress as a percentage of the total task.
                        Task.Factory.StartNew(() => { ProgressThread(worker); });

                        class1.InstallDockerforWindows();
                        if (!ishyperV)
                        {
                            Class1.CheckWinFeatureStatus(String.Concat(Class1.enableWinFeature, Class1.hyperV, " /all /NoRestart"), "dism.exe");
                        }
                        if (!isContainer)
                        {
                            Class1.CheckWinFeatureStatus(String.Concat(Class1.enableWinFeature, Class1.wincontainerFeature, " /all /NoRestart"), "dism.exe");
                        }
                        message = "Operation Succesfull. Restart to complete installation steps?";
                        caption = "Installation Complete Alert";

                        result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);

                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            BackgroundWorkerExample.ActiveForm.Close();
                            System.Diagnostics.Process.Start("shutdown", "/r /t 30");
                        }





                        return true;
                    }
                }
            }


            return false;

        }

        private void ProgressThread(BackgroundWorker worker)
        {
            int i = 0;
            bool isInstalled = false;
            while (!Class1._completed)
            {
                i = i + 10;
                Thread.Sleep(100);
                resultLabel.Text = "Downloading Docker for Windows ...";
                worker.ReportProgress(i);
            }
            i = 0;
            while (!isInstalled)
            {
                resultLabel.Text = "Installing Docker for Windows ..";
                i = i + 10;
                if (i > highestPercentageReached)
                {
                    Thread.Sleep(100);
                    highestPercentageReached = i;
                    worker.ReportProgress(i);
                    isInstalled = Class1.CheckdockerInstallation();
                }
            }
            worker.ReportProgress(progressBar1.Maximum);
        }

    }
}
