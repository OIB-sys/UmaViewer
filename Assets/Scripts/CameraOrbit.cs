using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using System.IO;
using System.Text;

public class CameraOrbit : MonoBehaviour
{
    public static CameraOrbit instance;
    public int CameraMode = 0;

    [Header("Light")]
    public GameObject Light;

    public Dropdown CameraModeDropdown;

    public GameObject CameraTargetHelper;
    public Vector3 TargetCenter;

    EventSystem eventSystem;
    bool controlOn;

    bool SaveButtonOn = false;
    bool LoadButtonOn = false;
    string CameraPresetPath =  @"Presets\CameraPresets.csv";
    int DropdownSelectedId = -1;

    [Header("Orbit Camera")]
    public GameObject OrbitCamSettingsTab;
    public Slider OrbitCamFovSlider;
    public Slider OrbitCamZoomSlider;
    public Slider OrbitCamZoomSpeedSlider;
    public Slider OrbitCamTargetHeightSlider;
    public Slider OrbitCamHeightSlider;
    public Slider OrbitCamRotationSlider;
    public Slider OrbitCamSpeedSlider;
    float camDistMin = 1, camDistMax = 15;

    [Header("Free Camera")]
    public GameObject FreeCamSettingsTab;
    public Slider FreeCamFovSlider;
    public Slider FreeCamRotationSlider;
    public Slider FreeCamMoveSpeedSlider;
    public Slider FreeCamRotateSpeedSlider;
    bool FreeCamLeft = false;
    bool FreeCamRight = false;
    private Quaternion lookRotation;

    TMP_InputField TargetInputField;
    TMP_Dropdown TargetDropdown;
    Toggle TargetLoadModeToggle;

    bool isLoadModeToggle = true;

    void Start()
    {
        OrbitCamZoomSlider.minValue = camDistMin;
        OrbitCamZoomSlider.maxValue = camDistMax;

        eventSystem = EventSystem.current;

        lookRotation = transform.localRotation;
        instance = this;

        TargetInputField = GameObject.Find("CameraNameInputField").GetComponent<TMP_InputField>();

        TargetDropdown = GameObject.Find("LoadCamerasDropdown").GetComponent<TMP_Dropdown>();

        TargetLoadModeToggle = GameObject.Find("LoadModeToggle").GetComponent<Toggle>();
        isLoadModeToggle = TargetLoadModeToggle.isOn;

        UpdateDropdown();
    }

    void UpdateDropdown()
    {

        bool isFileExists = true;
        if (!File.Exists(CameraPresetPath)) {
            return;
        }

        string[] presets = File.ReadAllLines(CameraPresetPath);

        var dropdownMembers = new List<string>(){};
        TargetDropdown.ClearOptions();
        foreach (var preset in presets) {
            string[] cells = preset.Split(',');
            dropdownMembers.Add(cells[0]);
        }
        TargetDropdown.AddOptions(dropdownMembers);
        if(DropdownSelectedId==-1){
            DropdownSelectedId=0;
        }
        TargetDropdown.value=DropdownSelectedId;
    }

    void Update()
    {
        if (HandleManager.InteractionInProgress) return;

        if(CameraMode != CameraModeDropdown.value)
        {
            switch (CameraMode) //old
            {
                case 0:
                    OrbitCamSettingsTab.SetActive(false);
                    break;
                case 1:
                    FreeCamSettingsTab.SetActive(false);
                    break;
            }
            switch (CameraModeDropdown.value) //new
            {
                case 0:
                    OrbitCamSettingsTab.SetActive(true);
                    break;
                case 1:
                    FreeCamSettingsTab.SetActive(true);
                    lookRotation.y = transform.eulerAngles.y;
                    break;
            }
            CameraMode = CameraModeDropdown.value;
        }

        switch (CameraModeDropdown.value)
        {
            case 0:
                OrbitAround();
                OrbitLight();
                break;
            case 1: FreeCamera(); break;
        }
    }

    #region PC controls
    /// <summary>
    /// Rotate Light Source
    /// </summary>
    private void OrbitLight()
    {
        if (Input.GetMouseButton(1))
        {
            Light.transform.Rotate(Input.GetAxis("Mouse X") * Vector3.up, Space.Self);
            Light.transform.Rotate(Input.GetAxis("Mouse Y") * Vector3.right, Space.Self);
        }
    }

    /// <summary>
    /// Orbit camera around world center
    /// </summary>
    void OrbitAround(bool aroundCharacter = false)
    {
        TargetCenter = Vector3.zero;

        Camera.main.fieldOfView = OrbitCamFovSlider.value;
        Vector3 position = transform.position;

        if (Input.GetMouseButtonDown(0) && !eventSystem.IsPointerOverGameObject())
        {
            controlOn = true;
        }
        if (Input.GetMouseButton(0) && controlOn)
        {
            position -= Input.GetAxis("Mouse X") * transform.right * OrbitCamSpeedSlider.value;
            OrbitCamHeightSlider.value -= Input.GetAxis("Mouse Y") * OrbitCamSpeedSlider.value;
        }
        else
        {
            controlOn = false;
        }

        if (!eventSystem.IsPointerOverGameObject())
        {
            OrbitCamZoomSlider.value -= Input.mouseScrollDelta.y * OrbitCamZoomSpeedSlider.value;
        }

        if ((Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.LeftControl)) && !eventSystem.IsPointerOverGameObject())
        {
            CameraTargetHelper.SetActive(true);
        }
        if ((Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftControl)) && CameraTargetHelper.activeSelf)
        {
            OrbitCamTargetHeightSlider.value -= Input.GetAxis("Mouse Y");
            CameraTargetHelper.transform.position = OrbitCamTargetHeightSlider.value * Vector3.up;
        }
        if (Input.GetMouseButtonUp(2) || Input.GetKeyUp(KeyCode.LeftControl))
        {
            CameraTargetHelper.SetActive(false);
        }


        float camDist = OrbitCamZoomSlider.value;
        OrbitCamHeightSlider.maxValue = camDist + 1 - camDist * 0.2f;
        OrbitCamHeightSlider.minValue = -camDist + 1 + camDist * 0.2f;

        Vector3 target = TargetCenter + OrbitCamTargetHeightSlider.value * Vector3.up; //set target offsets

        position.y = TargetCenter.y + OrbitCamHeightSlider.value; //set camera height
        transform.position = position;  //set final position of camera at target
        transform.LookAt(target); //look at target position
        transform.Rotate(0,0,OrbitCamRotationSlider.value);
        transform.position = target - transform.forward * camDist; //move away from target
    }

    /// <summary>
    /// Free Camera, Unity-Style
    /// </summary>
    void FreeCamera()
    {
        Camera.main.fieldOfView = FreeCamFovSlider.value;
        float moveSpeed = FreeCamMoveSpeedSlider.value;
        float rotateSpeed = FreeCamRotateSpeedSlider.value;

        if (Input.GetMouseButtonDown(0) && !eventSystem.IsPointerOverGameObject())
        {
            FreeCamLeft = true;
        }
        if (Input.GetMouseButton(0) && FreeCamLeft)
        {
            transform.position += Input.GetAxis("Vertical") * transform.forward * Time.deltaTime * moveSpeed;
            transform.position += Input.GetAxis("Horizontal") * transform.right * Time.deltaTime * moveSpeed;

            lookRotation.x -= Input.GetAxis("Mouse Y") * rotateSpeed;
            lookRotation.y += Input.GetAxis("Mouse X") * rotateSpeed;

            lookRotation.x = Mathf.Clamp(lookRotation.x, -90, 90);
        }
        else
        {
            FreeCamLeft = false;
        }
        transform.localRotation = Quaternion.Euler(lookRotation.x, lookRotation.y, FreeCamRotationSlider.value);

        if (Input.GetMouseButtonDown(1) && !eventSystem.IsPointerOverGameObject())
        {
            FreeCamRight = true;
        }
        if (Input.GetMouseButton(1) && FreeCamRight)
        {
            transform.position += Input.GetAxis("Mouse X") * transform.right  * Time.deltaTime * moveSpeed;
            transform.position += Input.GetAxis("Mouse Y") * transform.up * Time.deltaTime * moveSpeed;
        }
        else
        {
            FreeCamRight = false;
        }

    }

    public void SaveCamera()
    {
        //UnityEngine.Debug.Log("Save");
        string SavePresetName = TargetInputField.text;

        if(SavePresetName =="" ){
            return;
        }

        var presetMembers = new List<string>(){};
        
        presetMembers.Add(SavePresetName);

        presetMembers.Add(CameraMode.ToString());

        //Position
        presetMembers.Add(transform.position.x.ToString());
        presetMembers.Add(transform.position.y.ToString());
        presetMembers.Add(transform.position.z.ToString());

        //Rotation
        presetMembers.Add(transform.eulerAngles.x.ToString());
        presetMembers.Add(transform.eulerAngles.y.ToString());
        presetMembers.Add(transform.eulerAngles.z.ToString());

        if(CameraMode == 0){

            //orbit camera value
            presetMembers.Add(OrbitCamFovSlider.value.ToString());
            presetMembers.Add(OrbitCamZoomSlider.value.ToString());
            presetMembers.Add(OrbitCamZoomSpeedSlider.value.ToString());
            presetMembers.Add(OrbitCamTargetHeightSlider.value.ToString());
            presetMembers.Add(OrbitCamHeightSlider.value.ToString());
            presetMembers.Add(OrbitCamRotationSlider.value.ToString());
            presetMembers.Add(OrbitCamSpeedSlider.value.ToString());
        }else if(CameraMode == 1){

            //free camera value
            presetMembers.Add(FreeCamFovSlider.value.ToString());
            presetMembers.Add(FreeCamRotationSlider.value.ToString());
            presetMembers.Add(FreeCamMoveSpeedSlider.value.ToString());
            presetMembers.Add(FreeCamRotateSpeedSlider.value.ToString());

            //padding
            presetMembers.Add("");
            presetMembers.Add("");
            presetMembers.Add("");
        }else{
            return;
        }

        //Directional light position
        presetMembers.Add(Light.transform.position.x.ToString());
        presetMembers.Add(Light.transform.position.y.ToString());
        presetMembers.Add(Light.transform.position.z.ToString());

        //Directional light rotation
        presetMembers.Add(Light.transform.eulerAngles.x.ToString());
        presetMembers.Add(Light.transform.eulerAngles.y.ToString());
        presetMembers.Add(Light.transform.eulerAngles.z.ToString());

        //Directional light color
        presetMembers.Add(Light.GetComponent<Light>().color.r.ToString());
        presetMembers.Add(Light.GetComponent<Light>().color.g.ToString());
        presetMembers.Add(Light.GetComponent<Light>().color.b.ToString());
        presetMembers.Add(Light.GetComponent<Light>().color.a.ToString());

        bool isFileExists = true;
        if (!File.Exists (CameraPresetPath)) {
            isFileExists = false;
        }

        StreamWriter fp = new StreamWriter(CameraPresetPath, isFileExists, System.Text.Encoding.UTF8);
        string presetLine = string.Join(",", presetMembers);
        fp.WriteLine(presetLine);

        fp.Close();

        UpdateDropdown();
    }

    public void LoadCamera()
    {
        string TargetPresetName = TargetDropdown.captionText.text;

        if(TargetPresetName == ""){
            return;
        }
        
        int TargetPresetLabel = TargetDropdown.value;
        
        System.IFormatProvider flt = CultureInfo.InvariantCulture.NumberFormat;

        string[] presets = File.ReadAllLines(CameraPresetPath);
        string[] targetPreset = presets[TargetPresetLabel].Split(',');

        int CameraMode_tmp = int.Parse(targetPreset[1]);
        CameraModeDropdown.value = CameraMode_tmp;

        transform.position = new Vector3(float.Parse(targetPreset[2], flt), float.Parse(targetPreset[3], flt), float.Parse(targetPreset[4], flt));
        transform.localRotation = Quaternion.Euler(float.Parse(targetPreset[5], flt), float.Parse(targetPreset[6], flt), float.Parse(targetPreset[7], flt));

        if(CameraMode_tmp == 0){
            OrbitCamFovSlider.value = float.Parse(targetPreset[8], flt);
            OrbitCamZoomSlider.value = float.Parse(targetPreset[9], flt);
            OrbitCamZoomSpeedSlider.value = float.Parse(targetPreset[10], flt);
            OrbitCamTargetHeightSlider.value = float.Parse(targetPreset[11], flt);
            OrbitCamHeightSlider.value = float.Parse(targetPreset[12], flt);
            OrbitCamRotationSlider.value = float.Parse(targetPreset[13], flt);
            OrbitCamSpeedSlider.value = float.Parse(targetPreset[14], flt);

        }else if(CameraMode_tmp == 1){
            FreeCamFovSlider.value = float.Parse(targetPreset[8], flt);
            FreeCamRotationSlider.value = float.Parse(targetPreset[9], flt);
            FreeCamMoveSpeedSlider.value = float.Parse(targetPreset[10], flt);
            FreeCamRotateSpeedSlider.value = float.Parse(targetPreset[11], flt);
        }else{
            return;
        }

        if(isLoadModeToggle){
            Light.transform.position = new Vector3(float.Parse(targetPreset[15], flt), float.Parse(targetPreset[16], flt), float.Parse(targetPreset[17], flt));
            Light.transform.localRotation = Quaternion.Euler(float.Parse(targetPreset[18], flt), float.Parse(targetPreset[19], flt), float.Parse(targetPreset[20], flt));
            Light.GetComponent<Light>().color = new Color(float.Parse(targetPreset[21], flt), float.Parse(targetPreset[22], flt), float.Parse(targetPreset[23], flt), float.Parse(targetPreset[24], flt));
        }

    }

    public void onClickDel()
    {

        int TargetPresetLabel = TargetDropdown.value;
        string TargetPresetName = TargetDropdown.captionText.text;

        if(TargetPresetName == ""){
            return;
        }

        DropdownSelectedId=-1;

        List<string> presets = new List<string>();
        presets.AddRange(File.ReadAllLines(CameraPresetPath));
        presets.RemoveAt(TargetPresetLabel);

        File.WriteAllLines(CameraPresetPath, presets, System.Text.Encoding.UTF8);

        UpdateDropdown();

    }

    public void onDropdownValueChanged()
    {
        DropdownSelectedId = TargetDropdown.value;
    }

    public void onLoadModeToggleChanged()
    {
        isLoadModeToggle = TargetLoadModeToggle.isOn;
    }
    #endregion
}