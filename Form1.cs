using System;
using System.Windows.Forms;
using System.Management;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace WindowManager
{
    public partial class Form1 : Form
    {
        List<Process> processes = null;
        private ListViewItemComparer comparer = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            processes = new List<Process>();
            GetProcesses();
            RefreshProcessesList();

            comparer = new ListViewItemComparer();
            comparer.ColumnIndex = 0;
        }

        private void GetProcesses()
        {
            processes.Clear();
            processes = Process.GetProcesses().ToList<Process>();
        }

        private void RefreshProcessesList()
        {
            listView1.Items.Clear();
            double memSize = 0;
            foreach (Process p in processes)
            {
                memSize = 0;

                PerformanceCounter pc = new PerformanceCounter();
                pc.CategoryName = "Process";
                pc.CounterName = "Working Set - Private";
                pc.InstanceName = p.ProcessName;

                memSize = (double)pc.NextValue() / (1000 * 1000);

                // Массив строк, в котором будут хранится колонки
                string[] row = new string[] { p.ProcessName.ToString(), Math.Round(memSize,1).ToString()};
                listView1.Items.Add(new ListViewItem(row));

                pc.Close();
                pc.Dispose();
            }

            Text = "Запущено процессов: " + processes.Count.ToString();
        }

        private void RefreshProcessesList(List<Process> processes, string keyword)
        {
            try
            {
                listView1.Items.Clear();
                double memSize = 0;

                foreach (Process p in processes)
                {
                    if (p != null)
                    {
                        PerformanceCounter pc = new PerformanceCounter();
                        pc.CategoryName = "Process";
                        pc.CounterName = "Working Set - Private";
                        pc.InstanceName = p.ProcessName;

                        memSize = (double)pc.NextValue() / (1000 * 1000);

                        // Массив строк, в котором будут хранится колонки
                        string[] row = new string[] { p.ProcessName.ToString(), Math.Round(memSize, 1).ToString() };
                        listView1.Items.Add(new ListViewItem(row));

                        pc.Close();
                        pc.Dispose();
                    }
                }

                Text = $"Запущено процессов '{keyword}': " + processes.Count.ToString();
            }
            catch (Exception) { }
        }

        private void KillProcess(Process process)
        {
            process.Kill();
            process.WaitForExit();
        }

        private void KillProcessAndChildren(int pid)
        {
            if (pid == 0) return;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "Select * From Win32_Process Where ParentProcessID=" + pid);

            ManagementObjectCollection objectCollection = searcher.Get();

            foreach (ManagementObject obj in objectCollection)
            {
                KillProcessAndChildren(Convert.ToInt32(obj["ProcessID"]));
            }

            try
            {
                Process p = Process.GetProcessById(pid);
                p.Kill();
                p.WaitForExit();
            } catch (ArgumentException) { }
        }

        private int GetParentProcessId(Process p)
        {
            int parentID = 0;

            try
            {
                ManagementObject management = new ManagementObject("win32_process.handle='" + p.Id + "'");
                management.Get();
                parentID = Convert.ToInt32(management["ParentProcessId"]);
            } catch (Exception) { }

            return parentID;
        }

        private void refreshStripButton1_Click(object sender, EventArgs e)
        {
            GetProcesses();
            RefreshProcessesList();
        }

        private void killStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    // Сравниваем имя каждого процесса в списке, с именем в правой колонке
                    Process processToKill = processes.Where((p) => p.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];

                    KillProcess(processToKill);
                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        private void killTreeStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    // Сравниваем имя каждого процесса в списке, с именем в правой колонке
                    Process processToKill = processes.Where((p) => p.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];

                    KillProcessAndChildren(GetParentProcessId(processToKill));
                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        private void killProcessTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    // Сравниваем имя каждого процесса в списке, с именем в правой колонке
                    Process processToKill = processes.Where((p) => p.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];

                    KillProcessAndChildren(GetParentProcessId(processToKill));
                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        private void killToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    // Сравниваем имя каждого процесса в списке, с именем в правой колонке
                    Process processToKill = processes.Where((p) => p.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];

                    KillProcess(processToKill);
                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        //MessageBox & InputBox
        private void runTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Interaction.InputBox("Введите имя программы", "Запуск новой задачи");

            try
            {
                Process.Start(path);
            }
            catch (Exception) { }
        }

        // Adding Filter
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            GetProcesses();
            List<Process> filteredProcesses = processes.Where(p => p.ProcessName.ToLower().Contains(toolStripTextBox1.Text.ToLower())).ToList<Process>();

            RefreshProcessesList(filteredProcesses, toolStripTextBox1.Text);
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Получаем индекс колонки, по которой нажали
            comparer.ColumnIndex = e.Column;

            // Меняем направление сортировки
            comparer.SortDirection = (comparer.SortDirection == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;

            listView1.ListViewItemSorter = comparer;
            listView1.Sort();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
