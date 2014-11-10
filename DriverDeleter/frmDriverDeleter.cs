using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DriverDeleter
{
	public partial class frmDriverDeleter : Form
	{
		public frmDriverDeleter()
		{
			InitializeComponent();
		}

		private void frmDriverDeleter_Load(object sender, EventArgs e)
		{
			refreshDrivers();
		}

		private void btnDeleteDriver_Click(object sender, EventArgs e)
		{
			List<string> lstSuccess = new List<string>();
			List<string> lstFailure = new List<string>();
			StringBuilder message = new StringBuilder("Done.\n\n");

			foreach (DataGridViewRow row in dgvOemInfFiles.SelectedRows)
			{
				string infFile = row.Cells["File"].Value as String;

				try
				{
					try
					{ 
						OemInfFile.Uninstall(infFile);
						lstSuccess.Add(infFile);
					}
					catch (Win32Exception win32Ex)
					{
						if (win32Ex.NativeErrorCode == -536870340 || win32Ex.NativeErrorCode == 1008) // driver package has already been deleted from driver store; delete INF/PNF
						{
							string errorMsg = String.Format(
								"{0} received the error \"{1}\". This usually means the driver package has already been removed from the driver store.\n\nDo you want to try deleting the orphaned INF/PNF file(s)?",
								infFile, win32Ex.Message);

							if (MessageBox.Show(errorMsg, "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
							{
								OemInfFile.Delete(infFile);
								lstSuccess.Add(infFile);
							}
							else
							{
								throw;
							}
						}
						else
						{
							throw;
						}
					}
				}
				catch (Win32Exception ex)
				{                   
					lstFailure.Add(infFile);
					lstFailure.Add(String.Format("Win32 Error: {0}. {1}.", ex.NativeErrorCode, ex.Message));
				}
				catch (Exception ex)
				{
					lstFailure.Add(infFile);
					lstFailure.Add(ex.Message);
				}
			}

			if (lstSuccess.Count > 0)
			{
				message.AppendLine("The following drivers were successfully deleted:\n");
				message.AppendLine(String.Join(Environment.NewLine, lstSuccess));
			}

			if (lstFailure.Count > 0)
			{
				message.AppendLine("The following drivers failed to uninstall:\n");
				message.AppendLine(String.Join(Environment.NewLine, lstFailure));
			}

			MessageBox.Show(message.ToString(), "Driver Deleter Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			dgvOemInfFiles.Rows.Clear();
			refreshDrivers();
		}

		private void btnExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void refreshDrivers()
		{
			string[] infFilePaths = OemInfFile.GetOemInfFilePaths();

			foreach (string infFilePath in infFilePaths)
			{
				OemInfFile oemInfFile = new OemInfFile(infFilePath);
				dgvOemInfFiles.Rows.Add(
					oemInfFile.FileName,
					oemInfFile.Provider,
					oemInfFile.DeviceName,
					oemInfFile.DriverClass,
					oemInfFile.DriverCatalog,
					oemInfFile.DriverVersion,
					oemInfFile.DriverDate);
			}
		}
	}
}
