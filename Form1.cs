using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DEPEdit
{
    public partial class Form1 : Form
    {
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
        private Dictionary<string, string> registryEntries;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadRegistryEntries();

            // Fixes losing focus on listbox refresh
            BringToFront();
            Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddNewDEPItem();
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewDEPItem();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedItem();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DeleteSelectedItem();
        }

        private void ListBoxPrograms_SelectedValueChanged(object sender, EventArgs e)
        {
            if (listBoxPrograms.SelectedItem != null)
                button2.Visible = true;
            else
                button2.Visible = false;
        }

        private void LoadRegistryEntries()
        {
            listBoxPrograms.Items.Clear();
            registryEntries = new Dictionary<string, string>();

            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(RegistryPath, false))
            {
                if (key != null)
                {
                    string[] valueNames = key.GetValueNames();
                    foreach (string subKeyName in valueNames)
                    {
                        string fileDescription = GetFileDescription(subKeyName);
                        string displayText = $"{fileDescription} ({subKeyName})";
                        listBoxPrograms.Items.Add(displayText);
                        registryEntries[displayText] = subKeyName;
                    }
                }
                else
                {
                    MessageBox.Show("Failed to open registry key.");
                }
            }
        }

        private string GetFileDescription(string filePath)
        {
            try
            {
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                return fileInfo.FileDescription ?? Path.GetFileName(filePath);
            }
            catch
            {
                return Path.GetFileName(filePath);
            }
        }

        private void AddNewDEPItem()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "DEPEdit - Select Executable to Exclude";
                openFileDialog.InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(RegistryPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(filePath, "DisableNXShowUI", RegistryValueKind.String);
                        }
                        else
                        {
                            MessageBox.Show("Failed to open registry key for writing.");
                        }
                    }

                    // Hide Button and Refresh Listbox
                    button2.Visible = false;
                    LoadRegistryEntries();

                    // Deselect any listbox item
                    listBoxPrograms.SelectedIndex = -1;
                }
            }
        }

        private void DeleteSelectedItem()
        {
            bool success = false;

            if (listBoxPrograms.SelectedItem != null)
            {
                string selectedItem = listBoxPrograms.SelectedItem.ToString();
                if (registryEntries.TryGetValue(selectedItem, out string filePath))
                {
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(RegistryPath, true))
                    {
                        if (key != null)
                        {
                            if (key.GetValue(filePath) != null)
                            {
                                key.DeleteValue(filePath, false);
                                success = true;
                            }
                            else
                            {
                                MessageBox.Show($"Value {filePath} not found in the registry.");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Failed to open registry key for writing.");
                        }
                    }

                    // Hide Button and Refresh Listbox
                    button2.Visible = false;
                    LoadRegistryEntries();

                    // Deselect any listbox item
                    listBoxPrograms.SelectedIndex = -1;

                    // Show Success Msgbox after reloading listbox
                    if (success)
                    {
                        ShowDeletedMessageBox(filePath);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to find the registry entry for the selected item.");
                }
            }
        }

        private void ShowDeletedMessageBox(string filePath)
        {
            MessageBox.Show($"Deleted {filePath} from the registry.");
        }

        private void label1_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/TheCodingCarson/DEPEdit";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception error)
            {
                Console.WriteLine("An error occurred: " + error.Message);
            }
        }
    }
}
