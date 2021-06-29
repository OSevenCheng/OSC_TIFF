using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OSC_TIFF;
public class ShowTIFF : MonoBehaviour {

	// Use this for initialization
	void Start () {
		MeshRenderer r =transform.GetComponent<MeshRenderer>();
		//r.material= new Material(Shader.Find("Custom/QuadImage"));
		Material m = r.material;
		string FileName = Application.dataPath+ "/Data/2020_07_21_index_blue.tif";// R float32 LZW predictor==3
		//string FileName = Application.dataPath + "/Data/1.tif";   // RGBA float32 LZW predictor==1
		//string FileName = Application.dataPath + "/Data/2.tiff";  // RGB  uint8 LZW predictor==2
		TIFF tiff = new TIFF();
		tiff.Init(FileName);
		//tiff.PrintInfo();
		tiff.PrintInfo();
	    Texture2D tex = tiff.GetUnityTexture();
		m.SetTexture("_MainTex",tex);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
