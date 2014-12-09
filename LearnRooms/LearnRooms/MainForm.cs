using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Accord;
using Accord.Imaging;
using Accord.Imaging.Converters;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.DensityKernels;
using AForge;
using AForge.Imaging;
namespace LearnRooms {
    public partial class MainForm : Form {
        string levelName;
        string setName;
        Bitmap image;
        Bitmap convertedImage;
        List<Bitmap> tiles;

        public MainForm() {
            InitializeComponent();
        }
        public static Bitmap ConvertFormat(Bitmap orig, System.Drawing.Imaging.PixelFormat format) {
            Bitmap clone = new Bitmap(orig.Width, orig.Height, format);
            using (Graphics gr = Graphics.FromImage(clone)) {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }
            return clone;
        }
        private void MainForm_Load(object sender, EventArgs e) {
            maskedTextBox1.Mask = "0.00";
            listBox1.HorizontalScrollbar = true;
            image = ConvertFormat(new Bitmap("Levels/LoZ/Raw/level1-2.png"),System.Drawing.Imaging.PixelFormat.Format24bppRgb);
             Bitmap tile = ConvertFormat(new Bitmap("Tiles/LoZ/Water/mapTile135.png"),System.Drawing.Imaging.PixelFormat.Format24bppRgb);

          //  string[] array1 = Directory.GetFiles(@"C:\");
            //Directory.GetCurrentDirectory

          /* 
            tiles = Tile.getTiles(image, 16, 16);
            ImageToArray imageToArray = new ImageToArray(min: -1, max: +1);
            double[][] pixels; imageToArray.Convert(image, out pixels);
            ArrayToImage arrayToImage = new ArrayToImage(image.Width, image.Height, min: -1, max: +1);
            Bitmap outImage = null;
            arrayToImage.Convert(pixels, out outImage);
            
            this.clusterTile.Image = outImage;
           * */
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
        //    this.clusterTile.Image = clusteredTiles[this.ClusterList.SelectedIndex][0].tileImage;
        }
        private void button1_Click(object sender, EventArgs e) {
            /*
            int[] clusters = kModesClustering.Compute(tiles, 0.1);
            List<int> clusterList = new List<int>();
            clusteredTiles = new List<List<Tile>>();
            for (int ii = 0; ii < (int)clusterCount.Value; ii++) {
                clusterList.Add(ii+1);
                clusteredTiles.Add(new List<Tile>());
                for (int jj = 0; jj < clusters.Length; jj++) {
                    if (clusters[jj] == (ii)) {
                        clusteredTiles[ii].Add(tiles[jj][0]);
                    }
                }
            }

            this.ClusterList.DataSource = clusterList;
             * */
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e) {
       //      kModesClustering = new KModes<Tile>((int)clusterCount.Value, Tile.GetDistance);
         //   int[] clusters = kModesClustering.Compute(tiles, 0.1);
        }


        private void pictureBox1_Click(object sender, EventArgs e) {

        }

        private void button1_Click_1(object sender, EventArgs e) {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog1.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                try {
                    if ((myStream = openFileDialog1.OpenFile()) != null) {
                        using (myStream) {
                            this.levelName = openFileDialog1.FileName.Substring(openFileDialog1.FileName.LastIndexOf("\\")+1);
                            image = ConvertFormat(new Bitmap(System.Drawing.Image.FromStream(myStream)), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            pictureBox1.Image = image;
                        }
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }
        List<int> listBox1_selection = new List<int>();
        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e) {
            TrackSelectionChange((ListBox)sender, listBox1_selection);
            if (tiles != null && listBox1_selection.Count> 0)
            pictureBox3.Image = tiles[listBox1_selection[listBox1_selection.Count - 1]];
        }

        private void TrackSelectionChange(ListBox lb, List<int> selection) {
            ListBox.SelectedIndexCollection sic = lb.SelectedIndices;
            foreach (int index in sic)
                if (!selection.Contains(index)) selection.Add(index);

            foreach (int index in new List<int>(selection))
                if (!sic.Contains(index)) selection.Remove(index);
        }
        private void button2_Click(object sender, EventArgs e) {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = Directory.GetCurrentDirectory();
            folderBrowserDialog.ShowNewFolderButton = false;
            //folderBrowserDialog.RootFolder = Environment.SpecialFolder.Directory.GetCurrentDirectory();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK) {
                string folderName = folderBrowserDialog.SelectedPath;
                this.setName = folderName.Substring(folderName.LastIndexOf("\\")+1);
                string[] files = Directory.GetFiles(folderName);
                tiles = new List<Bitmap>();
                string[] editedFiles = new string[files.Length];
                int ii = 0;
                foreach (var file in files) {
                    tiles.Add(ConvertFormat(new Bitmap(System.Drawing.Image.FromFile(file)),System.Drawing.Imaging.PixelFormat.Format24bppRgb));
                    editedFiles[ii] = file.Substring(file.LastIndexOf("\\")+1);
                    ii++;
                }

                listBox1.DataSource = editedFiles;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e) {

        }
        private void pictureBox2_Click_1(object sender, EventArgs e) {

        }

        private void button3_Click(object sender, EventArgs e) {
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(float.Parse(maskedTextBox1.Text));
            convertedImage = new Bitmap(image.Width / 16, image.Height / 16, PixelFormat.Format24bppRgb);
            foreach (var selectedTile in listBox1_selection) { 
                TemplateMatch[] matchings = tm.ProcessImage(image, tiles[selectedTile]);


                foreach (var match in matchings) {
                    convertedImage.SetPixel(match.Rectangle.X / 16, match.Rectangle.Y / 16, Color.White);
                }
            }

           // convertedImage.Save("convertedImage.png", System.Drawing.Imaging.ImageFormat.Png);
            pictureBox2.Image = convertedImage;
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {

        }

        private void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) {

        }

        private void button4_Click(object sender, EventArgs e) {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.FileName = levelName.Substring(0, levelName.LastIndexOf(".")) + this.setName + ".png";
            saveFileDialog1.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                if ((myStream = saveFileDialog1.OpenFile()) != null) {
                    // Code to write the stream goes here.
                    convertedImage.Save(myStream,ImageFormat.Png);
                    myStream.Close();
                }
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e) {

        }


    }
}
