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
using Accord.Controls;
using Accord.IO;
using Accord.Math;
using Accord.Controls;
using Accord.IO;
using Accord.Math;
using AForge;
using AForge.Imaging;
using LearnRooms.Ogmo;
using System.Drawing.Drawing2D;
using SimpleLottery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
namespace LearnRooms {
    public partial class MainForm : Form {
        string levelName;
        string setName;
        Bitmap image;
        Bitmap convertedImage;
        List<Bitmap> tiles;
        HashSet<int> mainImageColors = new HashSet<int>();
        List<string> roomTypes = new List<string>();
        Dictionary<string, Dictionary<int, List<Room>>> roomClusters;
        Random rand = new Random();
        Room roomL;
        Room roomR;
        Room roomC;
        int snapshot = 0;
        Dictionary<string, double[,]> componentVec;
        public MainForm() {
            InitializeComponent();
        }
        public static string toString(double[,] array) {
            string str = "";
            for (int ii = 0; ii < array.GetLength(0); ii++) {
                for (int jj = 0; jj < array.GetLength(1); jj++) {
                    str += array[ii, jj] + ",";
                }
                str = str.Substring(0, str.Length - 1);
                str += ";";

            }
            str = str.Substring(0, str.Length - 1);
            return str;
        }
        private void MainForm_Load(object sender, EventArgs e) {
            List<Room> rooms = new List<Room>();
            string[] roomList = new string[]{
                "../OgmoZelda/LoZ-1.oel",
                "../OgmoZelda/LoZ-2.oel",
                "../OgmoZelda/LoZ-3.oel",
                "../OgmoZelda/LoZ-4.oel",
                "../OgmoZelda/LoZ-5.oel",
                "../OgmoZelda/LoZ-6.oel",
                "../OgmoZelda/LoZ-7.oel",
                "../OgmoZelda/LoZ-8.oel",
                "../OgmoZelda/LoZ-9.oel",
                "../OgmoZelda/LoZ2-1.oel",
            };
            foreach (var room in roomList){
                rooms.AddRange(readRooms(room, "../OgmoZelda/ZeldaRoom.oep", 16, 11, 12, 7));
            }
            
            roomList = new string[]{
                "../OgmoZelda/LttP1.oel", "../OgmoZelda/LttP10.oel", "../OgmoZelda/LttP11.oel", "../OgmoZelda/LttP12.oel",
                "../OgmoZelda/LttP13.oel", "../OgmoZelda/LttP14.oel", "../OgmoZelda/LttP15.oel", "../OgmoZelda/LttP16.oel", 
                "../OgmoZelda/LttP17.oel", "../OgmoZelda/LttP18.oel", "../OgmoZelda/LttP19.oel", "../OgmoZelda/LttP2.oel", 
                "../OgmoZelda/LttP20.oel", "../OgmoZelda/LttP21.oel", "../OgmoZelda/LttP22.oel", "../OgmoZelda/LttP3.oel", 
                "../OgmoZelda/LttP4.oel", "../OgmoZelda/LttP5.oel", "../OgmoZelda/LttP6.oel", "../OgmoZelda/LttP7.oel",
                "../OgmoZelda/LttP8.oel", "../OgmoZelda/LttP9.oel",
            };
            foreach (var room in roomList) {
                rooms.AddRange(readRooms(room, "../OgmoZelda/ZeldaRoom.oep", 16, 16, 11, 10));
            }
            roomList = new string[]{
                "../OgmoZelda/LA1.oel", "../OgmoZelda/LA2.oel",  "../OgmoZelda/LA3.oel", 
                "../OgmoZelda/LA4.oel", "../OgmoZelda/LA5.oel", "../OgmoZelda/LA6.oel", "../OgmoZelda/LA7.oel",
                "../OgmoZelda/LA8.oel",
            };
            foreach (var room in roomList) {
                rooms.AddRange(readRooms(room, "../OgmoZelda/ZeldaRoom.oep", 10, 8, 8, 6));
            }
             
            int ii = 0;
            List<Room> flipped = new List<Room>();
            foreach (var room in rooms) {
               // room.toBitmap().Save("room" + ii + ".png");
                room.changeSize(12, 10);
              //  room.toBitmap().Save("room" + ii + "A.png");
                Room temp = room.FlipUpDown();
                flipped.Add(temp);
                flipped.Add(room.FlipLeftRight());
                flipped.Add(temp.FlipLeftRight());
                ii++;
            }
            rooms.AddRange(flipped);
            Console.WriteLine(rooms.Count);
            var output = Factorization.Factorize(rooms, 65);
            roomClusters = output.Item1;
            componentVec = output.Item2;

            foreach (var roomtype in roomClusters.Keys) {
                roomTypes.Add(roomtype);
            }
            roomTypes.Sort();
            listBox1.DataSource = roomTypes;
            XDocument componentDoc = new XDocument(new XElement("root"));
            foreach (var component in componentVec){
                componentDoc.Root.Add(new XElement(component.Key, new XAttribute("dim1", component.Value.GetLength(0)), new XAttribute("dim2", component.Value.GetLength(1)),toString(component.Value)));
            }
            componentDoc.Save("components.xml");
            XDocument clusterDoc = new XDocument(new XElement("root"));
            foreach (var cluster in roomClusters) {
                foreach (var bunch in cluster.Value){
                    var clusterBunch = new XElement("Cluster",new XAttribute("name",cluster.Key),new XAttribute("id",bunch.Key));
                    foreach (var room in bunch.Value){
                        clusterBunch.Add(room.toXML());
                    }
                    clusterDoc.Root.Add(clusterBunch);
                }
            }
            clusterDoc.Save("clusters.xml");
            XDocument noclusterDoc = new XDocument(new XElement("root"));
            foreach (var cluster in roomClusters) {
                var clusterBunch = new XElement("Cluster", new XAttribute("name", cluster.Key), new XAttribute("id", 0));
                foreach (var bunch in cluster.Value) {
                    foreach (var room in bunch.Value) {
                        clusterBunch.Add(room.toXML());
                    }
                }
                noclusterDoc.Root.Add(clusterBunch);
            }
            noclusterDoc.Save("noclusters.xml");
        }
        public static List<Room> readRooms(string levelFile, string projectFile, 
                int roomToRoomWidth, int roomToRoomHeight, int roomWidth, int roomHeight) {
            List<Room> rooms = new List<Room>();
            OgmoLevel level = new OgmoLevel(levelFile);
            LevelStructure structure = new LevelStructure(projectFile);
            int cornerX = level.layers["Background"].entities[0].x;
            int cornerY = level.layers["Background"].entities[0].y;
            if (level.layers.ContainsKey("RoomMarker")) {
                if (level.layers["RoomMarker"].entities.Count > 0) {
                    cornerX = level.layers["RoomMarker"].entities[0].x;
                    cornerY = level.layers["RoomMarker"].entities[0].y;

                }
            }
            for (int ii = cornerX / 16 + (roomToRoomWidth - roomWidth)/2; ii < level.width / 16 - roomToRoomWidth; ii += roomToRoomWidth) {
                for (int jj = cornerY / 16 + (roomToRoomHeight - roomHeight)/2; jj < level.height / 16 - roomToRoomHeight; jj += roomToRoomHeight) {
                    Room room = new Room(roomWidth,roomHeight);
                    for (int xx = 0; xx < roomWidth; xx++) {
                        for (int yy = 0; yy < roomHeight; yy++) {
                            room.objects["blocks"][xx, yy] = level.layers["Blocks"].tiles[xx + ii, yy + jj];
                            room.objects["enemies"][xx, yy] = level.layers["Enemies"].tiles[xx + ii, yy + jj];
                            room.objects["keys"][xx, yy] = level.layers["Key"].tiles[xx + ii, yy + jj];
                            room.objects["keyItems"][xx, yy] = level.layers["KeyItem"].tiles[xx + ii, yy + jj];
                            room.objects["items"][xx, yy] = level.layers["Items"].tiles[xx + ii, yy + jj];
                            room.objects["traps"][xx, yy] = level.layers["Traps"].tiles[xx + ii, yy + jj];
                            room.objects["puzzles"][xx, yy] = level.layers["Puzzles"].tiles[xx + ii, yy + jj];
                            room.objects["water"][xx, yy] = level.layers["Water"].tiles[xx + ii, yy + jj];
                            if (level.layers.ContainsKey("Teleporter")) { 
                                room.objects["teleporters"][xx, yy] = level.layers["Teleporter"].tiles[xx + ii, yy + jj];
                            }
                        }
                    }
                    if (!room.isAllZero()) {
                        rooms.Add(room);
                    }
                }    
                
            }
            
            return rooms;
        }

        private void pictureBox2_Click(object sender, EventArgs e) {
            //L
        }

        private void pictureBox1_Click(object sender, EventArgs e) {
            //c
            
        }

        private void pictureBox3_Click(object sender, EventArgs e) {
            //r
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e) {
            if (roomL != null) {
                roomC = Room.Interpolate(roomL, roomR, hScrollBar1.Value / ((float)hScrollBar1.Maximum)).reconstruct(componentVec, 0);
                pictureBox1.Image = roomC.toBitmap();
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void button1_Click(object sender, EventArgs e) {
            string selectedType = (string) listBox1.SelectedItem;
            Dictionary<int, List<Room>> clusters = roomClusters[selectedType];
            List<Room> cluster = clusters.ElementAt(rand.Next(clusters.Count)).Value;
            List<int> indices = new List<int>();
            for (int ii = 0; ii < cluster.Count;ii++ ) {
                indices.Add(ii);

            }
            Console.WriteLine(indices.Count);
            indices.Shuffle();
            roomL = cluster[rand.Next(indices[0])];
            roomR = cluster[rand.Next(indices[0])];
            if (indices.Count > 1){
                roomR = cluster[rand.Next(indices[1])];
            }
            pictureBox2.Image = roomL.toBitmap();
            pictureBox3.Image = roomR.toBitmap();
            roomC = Room.Interpolate(roomL, roomR, hScrollBar1.Value / ((float)hScrollBar1.Maximum)).reconstruct(componentVec, 0);
            pictureBox1.Image = roomC.toBitmap();

        }

        private void button2_Click(object sender, EventArgs e) {
            roomL.toBitmap().Save("L" + snapshot + ".png");
            roomR.toBitmap().Save("R" + snapshot + ".png");
            roomC.toBitmap().Save("Combined" + snapshot + ".png");
            snapshot++;
        }
        /*
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
            image = ConvertFormat(new Bitmap("../Levels/LoZ/Raw/level1-2.png"),System.Drawing.Imaging.PixelFormat.Format24bppRgb);
             Bitmap tile = ConvertFormat(new Bitmap("../Tiles/LoZ/Water/mapTile135.png"),System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
        //    this.clusterTile.Image = clusteredTiles[this.ClusterList.SelectedIndex][0].tileImage;
        }
        private void button1_Click(object sender, EventArgs e) {
          
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
                            this.mainImageColors = GetColors(image);
                            pictureBox1.Image = image;
                        }
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }
        private HashSet<int> GetColors(Bitmap im){
            HashSet<int> colors = new HashSet<int>();
            for (int ii = 0; ii < im.Width; ii++){
                for (int jj = 0; jj < im.Height; jj++) {
                    colors.Add(im.GetPixel(ii, jj).GetHashCode());
                }
            }
            return colors;
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
            foreach (var tile in tiles) {
                HashSet<int> tileColors = GetColors(tile);
                var inters= new HashSet<int>(tileColors.Intersect(mainImageColors));
                if (intersection.Count > 0) {
                    Console.WriteLine(intersection.Count);
                    TemplateMatch[] matchings = tm.ProcessImage(image,tile);


                    foreach (var match in matchings) {
                        convertedImage.SetPixel(match.Rectangle.X / 16, match.Rectangle.Y / 16, Color.White);
                    }
                }
                else {
                    Console.WriteLine("No Match");
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

*/
    }
}
