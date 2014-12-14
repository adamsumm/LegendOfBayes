using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;

namespace LearnRooms.Ogmo {

    public class OgmoLevel {
        public string name;
        public int width;
        public int height;
        public Dictionary<string, OgmoLayer> layers;
        public OgmoLevel(string fileName) {
         //   name = xmlFile.name;
            XmlDocument xml = new XmlDocument();
            xml.Load(fileName);
         //   xml.LoadXml(xmlFile.text);
            XmlNode root = xml.FirstChild;
            width = System.Convert.ToInt32(root.Attributes.GetNamedItem("width").Value);
            height = System.Convert.ToInt32(root.Attributes.GetNamedItem("height").Value);
            XmlNodeList children = root.ChildNodes;
            layers = new Dictionary<string, OgmoLayer>();
            for (int ii = 0; ii < children.Count; ii++) {
                OgmoLayer layer = new OgmoLayer(children[ii], width, height);
                layers[layer.name] = layer;
            }
        }
    }
}
