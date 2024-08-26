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

    void Start()
    {
        OrbitCamZoomSlider.minValue = camDistMin;
        OrbitCamZoomSlider.maxValue = camDistMax;

        eventSystem = EventSystem.current;

        lookRotation = transform.localRotation;
        instance = this;

        UpdateDropdown();
    }

    void UpdateDropdown()
    {

        bool isFileExists = true;
        if (!File.Exists(CameraPresetPath)) {
            return;
        }

        string[] presets = File.ReadAllLines(CameraPresetPath);
        
        TMP_Dropdown TargetDropdown = GameObject.Find("LoadCamerasDropdown").GetComponent<TMP_Dropdown>();

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
        Transform curTransform;

        TMP_InputField TargetInputField = GameObject.Find("CameraNameInputField").GetComponent<TMP_InputField>();
        string SavePresetName = TargetInputField.text;
        
        if(SavePresetName =="" ){
            return;
        }

        curTransform = transform;

        var presetMembers = new List<string>(){};
        
        presetMembers.Add(SavePresetName);

        presetMembers.Add(CameraMode.ToString());

        presetMembers.Add(curTransform.position.x.ToString());
        presetMembers.Add(curTransform.position.y.ToString());
        presetMembers.Add(curTransform.position.z.ToString());

        if(CameraMode == 0){
            presetMembers.Add(curTransform.localRotation.x.ToString());
            presetMembers.Add(curTransform.localRotation.y.ToString());
            presetMembers.Add(curTransform.localRotation.z.ToString());

            presetMembers.Add(OrbitCamFovSlider.value.ToString());
            presetMembers.Add(OrbitCamZoomSlider.value.ToString());
            presetMembers.Add(OrbitCamZoomSpeedSlider.value.ToString());
            presetMembers.Add(OrbitCamTargetHeightSlider.value.ToString());
            presetMembers.Add(OrbitCamHeightSlider.value.ToString());
            presetMembers.Add(OrbitCamRotationSlider.value.ToString());
            presetMembers.Add(OrbitCamSpeedSlider.value.ToString());
        }else if(CameraMode == 1){
            presetMembers.Add(lookRotation.x.ToString());
            presetMembers.Add(lookRotation.y.ToString());
            presetMembers.Add(FreeCamRotationSlider.value.ToString());

            presetMembers.Add(FreeCamFovSlider.value.ToString());
            presetMembers.Add(FreeCamRotationSlider.value.ToString());
            presetMembers.Add(FreeCamMoveSpeedSlider.value.ToString());
            presetMembers.Add(FreeCamRotateSpeedSlider.value.ToString());
        }else{
            return;
        }

        bool isFileExists = true;
        if (!File.Exists (CameraPresetPath)) {
            isFileExists = false;
        }

        StreamWriter fp = new StreamWriter(CameraPresetPath, isFileExists, System.Text.Encoding.UTF8);

        string[] preset = { SavePresetName, SavePresetName + "dummy", "dummy" + SavePresetName };
        string presetLine = string.Join(",", presetMembers);
        fp.WriteLine(presetLine);

        fp.Close();

        UpdateDropdown();
    }

    public void LoadCamera()
    {

        TMP_Dropdown TargetDropdown = GameObject.Find("LoadCamerasDropdown").GetComponent<TMP_Dropdown>();
        int TargetPresetLabel = TargetDropdown.value;
        string TargetPresetName = TargetDropdown.captionText.text;

        if(TargetPresetName == ""){
            return;
        }

        string[] presets = File.ReadAllLines(CameraPresetPath);
        string[] targetPreset = presets[TargetPresetLabel].Split(',');

        int CameraMode_tmp = int.Parse(targetPreset[1]);
        CameraModeDropdown.value = CameraMode_tmp;

        transform.position = new Vector3(float.Parse(targetPreset[2], CultureInfo.InvariantCulture.NumberFormat), float.Parse(targetPreset[3], CultureInfo.InvariantCulture.NumberFormat), float.Parse(targetPreset[4], CultureInfo.InvariantCulture.NumberFormat));
        transform.localRotation = Quaternion.Euler(float.Parse(targetPreset[5], CultureInfo.InvariantCulture.NumberFormat), float.Parse(targetPreset[6], CultureInfo.InvariantCulture.NumberFormat), float.Parse(targetPreset[7], CultureInfo.InvariantCulture.NumberFormat));

        if(CameraMode_tmp == 0){
            OrbitCamFovSlider.value = float.Parse(targetPreset[8], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamZoomSlider.value = float.Parse(targetPreset[9], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamZoomSpeedSlider.value = float.Parse(targetPreset[10], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamTargetHeightSlider.value = float.Parse(targetPreset[11], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamHeightSlider.value = float.Parse(targetPreset[12], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamRotationSlider.value = float.Parse(targetPreset[13], CultureInfo.InvariantCulture.NumberFormat);
            OrbitCamSpeedSlider.value = float.Parse(targetPreset[14], CultureInfo.InvariantCulture.NumberFormat);
        }else if(CameraMode_tmp == 1){
            FreeCamFovSlider.value = float.Parse(targetPreset[8], CultureInfo.InvariantCulture.NumberFormat);
            FreeCamRotationSlider.value = float.Parse(targetPreset[9], CultureInfo.InvariantCulture.NumberFormat);
            FreeCamMoveSpeedSlider.value = float.Parse(targetPreset[10], CultureInfo.InvariantCulture.NumberFormat);
            FreeCamRotateSpeedSlider.value = float.Parse(targetPreset[11], CultureInfo.InvariantCulture.NumberFormat);
        }else{
            return;
        }


    }

    public void onClickDel()
    {

        TMP_Dropdown TargetDropdown = GameObject.Find("LoadCamerasDropdown").GetComponent<TMP_Dropdown>();
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
        TMP_Dropdown TargetDropdown = GameObject.Find("LoadCamerasDropdown").GetComponent<TMP_Dropdown>();
        DropdownSelectedId = TargetDropdown.value;
    }
    #endregion
}