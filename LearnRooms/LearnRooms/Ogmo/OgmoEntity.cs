using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace LearnRooms.Ogmo {

    public class OgmoEntity {
        public string name;
        public int x;
        public int y;
        public float width;
        public float height;
        public List<int[]> nodes;
        public Dictionary<string, string> entityAttributes;
        public OgmoEntity(XmlNode node) {
            XmlAttributeCollection attributes = node.Attributes;
            name = node.Name;
            width = float.NaN;
            height = float.NaN;
            entityAttributes = new Dictionary<string, string>();
            for (int ii = 0; ii < attributes.Count; ii++) {
                XmlNode attribute = attributes[ii];
                switch (attribute.Name) {
                    case "x":
                        x = System.Convert.ToInt32(attribute.Value);
                        break;
                    case "y":
                        y = System.Convert.ToInt32(attribute.Value);
                        break;
                    case "width":
                        width = System.Convert.ToSingle(attribute.Value);
                        break;
                    case "height":
                        height = System.Convert.ToSingle(attribute.Value);
                        break;
                    default:
                        entityAttributes[attribute.Name] = attribute.Value;
                        break;
                }
            }
            nodes = new List<int[]>();
            for (int ii = 0; ii < node.ChildNodes.Count; ii++) {
                XmlNode childNode = node.ChildNodes[ii];
                attributes = childNode.Attributes;
                int[] nodePosition = new int[]{int.Parse(attributes.GetNamedItem("x").Value),
                                                    int.Parse(attributes.GetNamedItem("y").Value)};
                nodes.Add(nodePosition);
            }
        }
    }
}
