using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class onChangeEnvironment : MonoBehaviour
{

    public GameObject ViewerMain;

    public GameObject Light;

    Color curLight;

    UmaViewerBuilder targetChar;

    void Start()
    {  
        curLight = Light.GetComponent<Light>().color;
        targetChar = ViewerMain.GetComponent<UmaViewerBuilder>();
    }

    void LateUpdate()
    {
        if(curLight != Light.GetComponent<Light>().color){
            curLight = Light.GetComponent<Light>().color;
            
            if(targetChar.CurrentUMAContainer != null){

                //eye material
                Color _ToonBrightColor = curLight;
                _ToonBrightColor.a = 0;
                Color _ToonDarkColor = _ToonBrightColor;
                targetChar.CurrentUMAContainer.EyeMaterial.SetColor("_ToonBrightColor",_ToonBrightColor);
                targetChar.CurrentUMAContainer.EyeMaterial.SetColor("_ToonDarkColor",_ToonBrightColor);

                //Mayu material
                Color _CharaColor = _ToonBrightColor;
                targetChar.CurrentUMAContainer.MayuMaterial.SetColor("_CharaColor",_CharaColor);
                //targetChar.CurrentUMAContainer.MayuMaterial.SetColor("_ToonDarkColor",_ToonBrightColor);
                //targetChar.CurrentUMAContainer.MayuMaterial.SetColor("_LightProbeColor",_ToonBrightColor);

            }

        }
        
    }
}
