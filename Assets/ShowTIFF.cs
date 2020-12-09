using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OSC_TIFF;
public class ShowTIFF : MonoBehaviour {

	// Use this for initialization
	void Start () {
		MeshRenderer r =transform.GetComponent<MeshRenderer>();
		//Material m = new Material(Shader.Find("Custom/QuadImage"));
		Material m = r.material;
		string FileName = Application.dataPath+"/Data/1.tif";
		TIFF tiff = new TIFF();
		tiff.Init(FileName);
		//tiff.PrintInfo();

		Texture2D tex = tiff.GetUnityTexture();
		m.SetTexture("_MainTex",tex);
		
		//r.material = m;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
