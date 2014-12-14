using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace LearnRooms.Ogmo {
public enum LayerType{
	TILES,
	GRID,
	ENTITIES
}
public class OgmoLayer {
	public string name;
	public int tileWidth;
	public int tileHeight;
	public List<OgmoEntity> entities;
	public int[,] tiles;
	
	public OgmoLayer(XmlNode layerNode, int width, int height){
		
		entities = new List<OgmoEntity>();
		LayerType type = LayerType.ENTITIES;
		name = layerNode.Name;
		if (layerNode.Attributes.GetNamedItem("tileset") != null){
			type  = LayerType.TILES;
		}
		else if (layerNode.Attributes.GetNamedItem("exportMode") != null){
			type = LayerType.GRID;
		}
		
		switch (type){
		case LayerType.ENTITIES:
			ParseEntityLayer(layerNode);
			break;
		case LayerType.TILES:
			ParseTileLayer(layerNode,width,height);
			break;
		case LayerType.GRID:
			ParseGridLayer(layerNode,width,height);
			break;
		}
			
	}
	
	protected void ParseEntityLayer(XmlNode layerNode){
		tiles = new int[0,0];
		tileWidth = 0;
		tileHeight = 0;
		
		for (int ii=0; ii < layerNode.ChildNodes.Count; ii++) {
			entities.Add (new OgmoEntity(layerNode.ChildNodes[ii]));
	    }
	}
	
	protected void ParseGridLayer(XmlNode layerNode, int width, int height){
		string text = layerNode.InnerText;
		string[] textLines = text.Split(new string[2]{"\n","\r"},System.StringSplitOptions.RemoveEmptyEntries);
		tiles = new int[textLines[0].Length,textLines.Length];
		for (int ii = 0; ii < tiles.GetLength(0); ii++){
			for (int jj = 0; jj < tiles.GetLength(1); jj++){
				tiles[ii,jj] = System.Convert.ToInt32("" + textLines[jj][ii]);
			}
		}
		tileWidth = width/tiles.GetLength(0);
		tileHeight = height/tiles.GetLength(1);
	}
	
	protected void ParseTileLayer(XmlNode layerNode, int width, int height){
		string text = layerNode.InnerText;
		string[] textLines = text.Split('\n');
		for (int jj = 0; jj < textLines.Length; jj++){
			string[] splitLine = textLines[jj].Split(',');
			if (tiles == null){
				tiles = new int[splitLine.Length,textLines.Length];
			}
			for (int ii = 0; ii < splitLine.Length; ii++){
				tiles[ii,jj] = System.Convert.ToInt32("" +splitLine[ii]);
			}
		}
		tileWidth = width/tiles.GetLength(0);
		tileHeight = height/tiles.GetLength(1);
	}
}
}
