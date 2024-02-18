using LiveCharts;
using LiveCharts.Definitions.Charts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace GraphForm
{
    public partial class Form1 : Form
    {
        public string genericFileName = "file";

        public enum FileType
        {
            none,
            XML,
            CSV
        }

        public static FileType filetype = FileType.none;

        public Form1()
        {
            InitializeComponent();

        }


        public void SaveGraph()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
            saveFileDialog.FileName = "graph.png"; // Set the default file name here

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Create a bitmap to render the LiveCharts chart
                Bitmap bmp = new Bitmap(cartesianChart1.Width, cartesianChart1.Height);
                cartesianChart1.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

                // Get the selected file extension from the save dialog
                string fileExtension = Path.GetExtension(saveFileDialog.FileName);

                // Save the bitmap to the selected file
                switch (fileExtension.ToLower())
                {
                    case ".png":
                        bmp.Save(saveFileDialog.FileName, ImageFormat.Png);
                        break;
                    case ".jpg":
                        bmp.Save(saveFileDialog.FileName, ImageFormat.Jpeg);
                        break;
                    case ".bmp":
                        bmp.Save(saveFileDialog.FileName, ImageFormat.Bmp);
                        break;
                    default:
                        // Handle unsupported file types or display an error message
                        MessageBox.Show("Unsupported file type", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
        }
    

        public void UpdateGraph()
        {
            cartesianChart1.Series.Clear();
            LiveCharts.SeriesCollection sc = new LiveCharts.SeriesCollection();

            var years = (from o in valuesBindingSource.DataSource as List<Values> select new { Year = o.Year }).Distinct();

            foreach (var y in years)
            {
                List<double> values = new List<double>();

                for (int month = 1; month <= 12; month++)
                {
                    double val = 0;

                    var data = from o in valuesBindingSource.DataSource as List<Values>
                               where o.Year.Equals(y.Year) && o.Month.Equals(month)
                               orderby o.Month ascending
                               select new { o.Value, o.Month };

                    // Sum the values for the same month
                    foreach (var item in data)
                    {
                        val += item.Value;
                    }

                    values.Add(val);
                }

                sc.Add(new LineSeries()
                {
                    Title = y.Year.ToString(),
                    Values = new ChartValues<double>(values)
                });
            }

            cartesianChart1.Series = sc;
        }


        private void PrintGraph(object sender, PrintPageEventArgs e) // should prnt i guess
        {
            Bitmap bmp = new Bitmap(cartesianChart1.Width, cartesianChart1.Height);
            cartesianChart1.DrawToBitmap(bmp, cartesianChart1.ClientRectangle);

            e.Graphics.DrawImage(bmp, 0,0);
        }

        public void ImportData(string filename, FileType filetype)
        {
            try
            {
                if (filetype == FileType.XML)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Values>));

                    using (TextReader reader = new StreamReader(filename))
                    {
                        List<Values> importedData = (List<Values>)serializer.Deserialize(reader);
                        valuesBindingSource.DataSource = importedData;
                        UpdateGraph();
                    }
                }
                else if (filetype == FileType.CSV)
                {
                    List<Values> importedData = new List<Values>();

                    using (var reader = new StreamReader(filename))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            var values = line.Split(',');

                            if (values.Length == 3)
                            {
                                int year, month;
                                double value;

                                if (int.TryParse(values[0], out year) && int.TryParse(values[1], out month) && double.TryParse(values[2], out value))
                                {
                                    importedData.Add(new Values { Year = year, Month = month, Value = value });
                                }
                            }
                            // Handle incorrect CSV format or display an error message
                        }
                    }

                    valuesBindingSource.DataSource = importedData;
                    UpdateGraph();
                }
                else
                {
                    MessageBox.Show("Invalid file type.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while importing data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public void ExportData(FileType filetype)
        {
            List<Values> dataList = valuesBindingSource.DataSource as List<Values>;

            if (dataList == null)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                if (filetype == FileType.XML)
                {
                    saveFileDialog.Filter = "XML Files|*.xml";
                    saveFileDialog.FileName = "data.xml"; // Set the default file name here
                }
                else if (filetype == FileType.CSV)
                {
                    saveFileDialog.Filter = "CSV Files|*.csv";
                    saveFileDialog.FileName = "data.csv"; // Set the default file name here
                }
                else
                {
                    MessageBox.Show("Invalid file type.");
                    return;
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    if (filetype == FileType.XML)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<Values>));

                        using (TextWriter writer = new StreamWriter(filePath))
                        {
                            serializer.Serialize(writer, dataList);
                        }
                    }
                    else if (filetype == FileType.CSV)
                    {
                        StringBuilder csvContent = new StringBuilder();

                        // Add header
                        csvContent.AppendLine("Year,Month,Value");

                        foreach (var data in dataList)
                        {
                            // Format the line as per your CSV structure
                            csvContent.AppendLine($"{data.Year},{data.Month},{data.Value}");
                        }

                        File.WriteAllText(filePath, csvContent.ToString());
                    }
                    else
                    {
                        MessageBox.Show("Invalid file type.");
                        return;
                    }

                    MessageBox.Show($"Data exported to {filePath}");
                }
            }
        }


        private void pieChart1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            valuesBindingSource.DataSource = new List<Values>();

            cartesianChart1.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Month",
            });
            cartesianChart1.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Value",
            });

            cartesianChart1.LegendLocation = LiveCharts.LegendLocation.Right;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (valuesBindingSource.DataSource == null)
            {
                return;
            }

            UpdateGraph();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveGraph();
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pDg = new PrintDialog();
            var pDoc = new PrintDocument();

            pDoc.PrintPage += new PrintPageEventHandler(PrintGraph);

            if (pDg.ShowDialog() == DialogResult.OK)
            {
                pDoc.Print();
            }


        }

        private void XMLExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SaveFileDialog saveFileDialog = new SaveFileDialog();

            ExportData(FileType.XML);
        }

        private void CSVExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SaveFileDialog saveFileDialog = new SaveFileDialog();

            ExportData(FileType.CSV);
        }

        private void XMLImportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files|*.xml";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportData(openFileDialog.FileName, FileType.XML);
            }
        }

        private void CSVImportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files|*.csv";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportData(openFileDialog.FileName, FileType.CSV);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by: Jan Klein\n\r\n\rRegistered Trademark 2024", "About");
        }
    }
}
