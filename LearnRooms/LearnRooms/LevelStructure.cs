using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace LearnRooms {
    public class LayerDefinition{
        public int width = -1 ;
        public int height = -1;
        public LayerDefinition(int w, int h) {
            width = w;
            height = h;
        }
    }
    public class LevelStructure {
        public Dictionary<string, LayerDefinition> definitions = new Dictionary<string, LayerDefinition>();
        public LevelStructure(string file) {

            XmlDocument xml = new XmlDocument();
            xml.Load(file);
            //   xml.LoadXml(xmlFile.text);
            XmlNode root = xml.FirstChild.NextSibling;
            XmlNodeList children = root.ChildNodes;
            for (int ii = 0; ii < children.Count; ii++) {
                Console.WriteLine(children[ii].Name);
                foreach(XmlNode child in children[ii].ChildNodes){
                    if (child.Name == "LayerDefinition") {
                        string name = "";
                        int gridX = 0;
                        int gridY = 0;
                        foreach (XmlNode subChild in child.ChildNodes) {
                            if (subChild.Name == "Name") {
                                name = subChild.InnerText;
                            }
                            else if (subChild.Name == "Grid") {
                                foreach (XmlNode grid in subChild.ChildNodes) {
                                    if (grid.Name == "Width") {
                                        gridX = int.Parse(grid.InnerText);
                                    }
                                    else if (grid.Name == "Height") {
                                        gridY = int.Parse(grid.InnerText);
                                    }
                                }
                            }
                        }
                        definitions[name] = new LayerDefinition(gridX, gridY);
                    }
                }
            }

        }
    }
}
