using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


class VAMJoint
{
    public DynamicBone jiggleBone;
    public FixedJoint fixedJoint;
    public List<SpringJoint> attachedSprings;
    public List<Rigidbody> attachedRigidBodies;
    public JSONStorableBool forceEnable;
    public bool newlyAddedJoint = true;
    public string jointType;

    public static string JIGGLEBONE = "JiggleBone";
    public static string FIXED = "Fixed";


    public VAMJoint(DynamicBone dynamicBone)
    {
        jointType = JIGGLEBONE;
        jiggleBone = dynamicBone;
    }

    public VAMJoint(FixedJoint fixedJoint_, List<SpringJoint> attachedSprings_, List<Rigidbody> attachedRigidBodies_)
    {
        jointType = FIXED;
        fixedJoint = fixedJoint_;
        attachedRigidBodies = attachedRigidBodies_;
        attachedSprings = attachedSprings_;
        
    }

}

class JigglesAndJoints : MVRScript
    {
   
    List<string> transformIds;
    Dictionary<string, Transform> transforms;
    bool subscene = false;
    int counter = 0;
    const string XPSLoaderName = "XPSLoader.XPSLoader";
    bool attachedXPSLoader = false;
    string XPSLoaderPluginID = null;
    Dictionary<int, List<UIDynamic>> uiDict;
    Dictionary<int, VAMJoint> jointDict;
   // JSONStorableBool xpsModelLoaded;
    JSONClass pluginJson;

    public override void Init()
        {


        jointDict = new Dictionary<int, VAMJoint>();
        uiDict = new Dictionary<int, List<UIDynamic>>();

        if (this.containingAtom.isSubSceneRestore || containingAtom.name.Contains('/'))
            subscene = true;
        else
            subscene = false;

        refreshTransforms(subscene);

        JSONStorableStringChooser jointType = new JSONStorableStringChooser("jointType", new List<string>(new string[] { VAMJoint.JIGGLEBONE, VAMJoint.FIXED }), new List<string>(new string[] { "Jiggle Bone", "Fixed Joint"}), null, "Pick Joint Type");
        //RegisterStringChooser(jointType);        
        UIDynamicPopup wiggleTypeDP = CreatePopup(jointType);

        UIDynamicButton but = CreateButton("Configure");
        but.button.onClick.AddListener(delegate ()
        {
            setJointType(jointType.val);                 
        });

        UIDynamicButton refreshT = CreateButton("Refresh Transforms", false);
        refreshT.button.onClick.AddListener(delegate () {
            refreshTransforms(subscene);
        });

    }

    protected void setJointType(string type_)
    {
        List<UIDynamic> uicomp = new List<UIDynamic>();

        if (type_.Equals(VAMJoint.JIGGLEBONE))
        {
            uicomp = setupJiggleBoneUI(uicomp);

        }
        else if (type_.Equals(VAMJoint.FIXED))
        {
            uicomp = setupFixedJointUI(uicomp);

        }

        uicomp.Add(CreateSpacer());

        if (uiDict.ContainsKey(counter))
            uiDict[counter].AddRange(uicomp);
        else
            uiDict.Add(counter, uicomp);

        counter++;
    }

    private List<UIDynamic> setupJiggleBoneUI(List<UIDynamic> uicomp, string boneName_ =  "", bool restore = false)
    {
        string boneName = "";
        
        if (restore)
        {
            boneName = boneName_;
        }

        uicomp.Add(CreateLabel("Jiggle Bone Config", false));
        string storableName = "jointConfig_JB_" + counter;
        JSONStorableStringChooser transformJSON = new JSONStorableStringChooser(storableName, null, null, "Jiggle Bone");
        RegisterStringChooser(transformJSON);

        transformJSON.choices = transformIds;
        transformJSON.val = boneName;

        UIDynamicPopup fp = CreateFilterablePopup(transformJSON);
        fp.popupPanelHeight = 700f;
        uicomp.Add(fp);
        int currentCount = counter;

        bool addedJoint = false;
        UIDynamicButton addButton = CreateButton("Add to CUA", false);

        UIDynamicButton removeButton = CreateButton("Remove Config", false);        

        addButton.button.onClick.AddListener(delegate () {
            addButton.buttonColor = Color.green;

            setUpJiggleBone(transformJSON.val, currentCount);
            createJointPostAddUI(currentCount, VAMJoint.JIGGLEBONE, restore);
            addedJoint = true;
            addButton.button.interactable = false;
            removeButton.button.interactable = true;
            fp.popup.topButton.interactable = false;
        });

        addButton.buttonColor = Color.red;
        uicomp.Add(addButton);

        removeButton.button.onClick.AddListener(delegate () {
            cleanupUI(currentCount, addedJoint);            
            RemoveButton(removeButton);
        });

        uicomp.Add(removeButton);
        
        if (restore)
        {
            addButton.buttonColor = Color.green;
            addButton.button.interactable = false;
            removeButton.button.interactable = true;
            setUpJiggleBone(boneName, currentCount, true);
            createJointPostAddUI(currentCount, VAMJoint.JIGGLEBONE, restore);
        }

        uicomp.Add(CreateSpacer());

        return uicomp;
    }

    private List<UIDynamic> setupFixedJointUI(List<UIDynamic> uicomp, string boneName_ = "", bool restore = false)
    {
        string boneName = "";

        if (restore)
        {
            boneName = boneName_;
        }

        uicomp.Add(CreateLabel("Fixed Joint Config", false));

        string storableName = "jointConfig_FJ_" + counter;

        JSONStorableStringChooser transformJSON = new JSONStorableStringChooser(storableName, null, null, "Fixed Root");
        RegisterStringChooser(transformJSON);

        transformJSON.choices = transformIds;
        transformJSON.val = boneName;

        UIDynamicPopup fp = CreateFilterablePopup(transformJSON);
        fp.popupPanelHeight = 700f;
        uicomp.Add(fp);
        int currentCount = counter;

        bool addedJoint = false;
        UIDynamicButton addButton = CreateButton("Add to CUA", false);

        UIDynamicButton removeButton = CreateButton("Remove Config", false);

        addButton.button.onClick.AddListener(delegate () {
            addButton.buttonColor = Color.green;

            setUpFixedJoint(transformJSON.val, currentCount);
            createJointPostAddUI(currentCount, VAMJoint.FIXED, restore);
            addedJoint = true;
            addButton.button.interactable = false;
            removeButton.button.interactable = true;
            fp.popup.topButton.interactable = false;
        });

        addButton.buttonColor = Color.red;
        uicomp.Add(addButton);

        removeButton.button.onClick.AddListener(delegate () {
            cleanupUI(currentCount, addedJoint);
            RemoveButton(removeButton);
        });

        uicomp.Add(removeButton);

        if (restore)
        {
            addButton.buttonColor = Color.green;
            addButton.button.interactable = false;
            removeButton.button.interactable = true;
            setUpFixedJoint(boneName, currentCount, true);
            createJointPostAddUI(currentCount, VAMJoint.FIXED, restore);
        }

        uicomp.Add(CreateSpacer());

        return uicomp;
    }

    private void setUpJiggleBone(string bone, int counted, bool restore = false)
    {
        List<DynamicBoneColliderBase> dbc = new List<DynamicBoneColliderBase>();
        Transform JB = transforms[bone];
      
        
        DynamicBone db = JB.gameObject.AddComponent<DynamicBone>();

        //db.m_UpdateMode = DynamicBone.UpdateMode.AnimatePhysics;
        db.m_Damping = 0.2f;
        db.m_Elasticity = 0.1f;
        db.m_Stiffness = 0.7f;
        db.m_Inert = 1.0f;
        db.m_Radius = 0.1f;
        db.m_EndOffset = Vector3.Scale(JB.forward, new Vector3(0.1f, 0.1f, 0.1f));
        db.m_Root = JB;
        db.m_Colliders = dbc;
        db.UpdateParameters();

        jointDict.Add(counted, new VAMJoint(db));
       
    }

    private void setUpFixedJoint(string bone, int counted, bool restore = false)
    {
        Transform joint = transforms[bone];

        Rigidbody rgb = joint.gameObject.AddComponent<Rigidbody>();
        rgb.isKinematic = true;
        rgb.useGravity = false;

        FixedJoint hinge = joint.gameObject.AddComponent<FixedJoint>();
        hinge.enableCollision = true;

        List<SpringJoint> attachedSprings = new List<SpringJoint>();
        List<Rigidbody> attachedRigidBodies = new List<Rigidbody>();

        Transform[] tt = joint.GetComponentsInChildren<Transform>();

        Rigidbody currentRG = rgb;
        foreach (Transform bon in tt)
        {
            if (bon.Equals(joint))
                continue;

            if (bon.childCount < 2)
            {
                currentRG = addSpringJoint(bon, currentRG);
                attachedRigidBodies.Add(currentRG);
                attachedSprings.Add(currentRG.GetComponent<SpringJoint>());
            }
            else
            {
                int path = 0;
                bool shouldAdd = true;
                for (int i = 0; i < bon.childCount; i++)
                {
                    if (path == 0 && bon.GetChild(i).childCount > 0)
                        path = bon.GetChild(i).childCount; //item with more one child or more
                    else if (path > 0 && bon.GetChild(i).childCount > 0)
                        shouldAdd = false;                  //multiple items with one+ child
                }

                if (path > 0 && shouldAdd)
                {
                    currentRG = addSpringJoint(bon, currentRG);
                    attachedRigidBodies.Add(currentRG);
                    attachedSprings.Add(currentRG.GetComponent<SpringJoint>());

                }
                else
                    break;
            }
        }

        jointDict.Add(counted, new VAMJoint(hinge, attachedSprings, attachedRigidBodies));
    }

    Rigidbody addSpringJoint(Transform trans, Rigidbody connected)
    {
        Rigidbody rgb = trans.gameObject.AddComponent<Rigidbody>();
        rgb.isKinematic = false;
        rgb.useGravity = true;
        rgb.mass = 0.1f;

        SpringJoint spring = trans.gameObject.AddComponent<SpringJoint>();
        spring.enableCollision = true;
        spring.connectedBody = connected;        
        spring.spring = 200f;
        spring.damper = 10f;

        return rgb;
    }

    private void refreshTransforms(bool forceSS)
    {
        refreshTransforms(getActualContainingGOM(forceSS));
    }

    public GameObject getActualContainingGOM(bool forceSS = false)
    {
        if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
        {
            return this.containingAtom.containingSubScene.containingAtom.gameObject;
        }
        else
        {
            return containingAtom.gameObject;
        }
    }

    public Atom getActualContainingAtom(bool forceSS = false)
    {
        if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
            return SuperController.singleton.GetAtomByUid(this.containingAtom.subScenePath.Split('/')[0]);
        else
            return containingAtom;
    }

    private void refreshTransforms(GameObject root)
    {
        SkinnedMeshRenderer[] smr = root.GetComponentsInChildren<SkinnedMeshRenderer>();

        transformIds = new List<string>();
        transforms = new Dictionary<string, Transform>();

        foreach (SkinnedMeshRenderer sm in smr)
        {

            Transform[] tt = sm.bones;


            foreach (Transform trans in tt)
            {
                if (trans != null)
                {
                    if (trans.gameObject.GetComponent<Atom>() != null || trans.gameObject.GetComponent<RectTransform>() != null || trans.gameObject.GetComponent<FreeControllerV3>() != null || trans.gameObject.GetComponent<SubAtom>() != null)
                    {
                        continue;
                    }


                    if (transforms.ContainsKey(trans.name))
                    {
                        if (transforms[trans.name].Equals(trans)) //this is the same bone.. ignore it.
                            continue;
                        else //a different bone with the same name, add a uniq version of it.
                        {
                            string uniqName = trans.name;
                            int count = 0;
                            while (transforms.ContainsKey(uniqName))
                            {
                                uniqName = trans.name + "_" + count;
                                count++;
                            }
                            transforms.Add(uniqName, trans);
                            transformIds.Add(uniqName);
                        }
                    }
                    else
                    {
                        transforms.Add(trans.name, trans);
                        transformIds.Add(trans.name);
                    }

                }
            }
        }

        //hmm no bones attached to a skinned mesh.. mayeb we just go get all teh bones ?
        if(transforms.Count == 0)
        {
            Transform[] tt = containingAtom.GetComponentsInChildren<Transform>();


            foreach (Transform trans in tt)
            {
                if (trans != null)
                {
                    if (trans.gameObject.GetComponent<Atom>() != null || trans.gameObject.GetComponent<RectTransform>() != null || trans.gameObject.GetComponent<FreeControllerV3>() != null || trans.gameObject.GetComponent<SubAtom>() != null)
                    {
                        continue;
                    }


                    if (transforms.ContainsKey(trans.name))
                    {
                        if (transforms[trans.name].Equals(trans)) //this is the same bone.. ignore it.
                            continue;
                        else //a different bone with the same name, add a uniq version of it.
                        {
                            string uniqName = trans.name;
                            int count = 0;
                            while (transforms.ContainsKey(uniqName))
                            {
                                uniqName = trans.name + "_" + count;
                                count++;
                            }
                            transforms.Add(uniqName, trans);
                            transformIds.Add(uniqName);
                        }
                    }
                    else
                    {
                        transforms.Add(trans.name, trans);
                        transformIds.Add(trans.name);
                    }

                }
            }
        }
    }

    public UIDynamicTextField CreateLabel(string label, bool rhs, int height = 40)
    {
        JSONStorableString jsonLabel = new JSONStorableString(label, label);
        UIDynamicTextField labelField = CreateTextField(jsonLabel, rhs);
        SetTextFieldHeight(labelField, height);

        return labelField;
    }

    public static void SetTextFieldHeight(UIDynamicTextField textField, int height)
    {
        LayoutElement component = textField.GetComponent<LayoutElement>();
        if (component != null)
        {
            component.minHeight = height;
            component.preferredHeight = height;
        }
        textField.height = height;
    }

    void cleanupUI(int itemNum, bool addedJoints)
    {
        foreach (UIDynamic dd in uiDict[itemNum])
        {
            if (dd.GetType().Equals(typeof(UIDynamicPopup)))
            {
                RemovePopup((UIDynamicPopup)dd);
                DestroyImmediate(dd);
            }
            else if (dd.GetType().Equals(typeof(UIDynamicButton)))
            {
                RemoveButton((UIDynamicButton)dd);
                DestroyImmediate(dd);
            }
            else if (dd.GetType().Equals(typeof(UIDynamicTextField)))
            {
                RemoveTextField((UIDynamicTextField)dd);
                DestroyImmediate(dd);
            }
            else if (dd.GetType().Equals(typeof(UIDynamicToggle)))
            {
                RemoveToggle((UIDynamicToggle)dd);
                DestroyImmediate(dd);
            }
            else if (dd.GetType().Equals(typeof(UIDynamicSlider)))
            {
                RemoveSlider((UIDynamicSlider)dd);
                DestroyImmediate(dd);
            }
            else if (dd.GetType().Equals(typeof(UIDynamic)))
            {
                RemoveSpacer((UIDynamic)dd);
                DestroyImmediate(dd);
            }
        }

        if (jointDict.ContainsKey(itemNum))
        {
            if(jointDict[itemNum].jointType.Equals(VAMJoint.JIGGLEBONE))
            {     
                Destroy(jointDict[itemNum].jiggleBone);
                jointDict.Remove(itemNum);
            }
            else if (jointDict[itemNum].jointType.Equals(VAMJoint.FIXED))
            {
                foreach(SpringJoint sj in jointDict[itemNum].attachedSprings)
                    Destroy(sj);

                foreach (Rigidbody rg in jointDict[itemNum].attachedRigidBodies)
                    Destroy(rg);

                Rigidbody rgb = jointDict[itemNum].fixedJoint.GetComponent<Rigidbody>();
                Destroy(jointDict[itemNum].fixedJoint);
                Destroy(rgb);
                jointDict.Remove(itemNum);
            }

        }

    }

    private void createJointPostAddUI(int jointID, string jointType, bool restore)
    {
        List<UIDynamic> uiDictL = new List<UIDynamic>();

        if (jointDict.ContainsKey(jointID))
        {
            VAMJoint jointD = jointDict[jointID];
            //solvD.forceEnable;
            uiDictL.Add(CreateLabel("Joint " + (jointID + 1) + " " + jointType.ToString(), true));

            if (jointType.Equals(VAMJoint.JIGGLEBONE))
            {
                DynamicBone jb = jointD.jiggleBone;

                jointD.forceEnable = createToggleStorable("enableJoint" + jointID, jb.enabled, (bool val) => { jb.enabled = val; }, restore);
                UIDynamicToggle tog = CreateToggle(jointD.forceEnable, true);
                tog.labelText.text = "Enabled";
                uiDictL.Add(tog);
                            
                uiDictL.Add(createFloatSlider("dampingJB" + jointID, "Damping", jb.m_Damping, (float val) => { jb.m_Damping = val; }, restore));
                uiDictL.Add(createFloatSlider("elasticityJB" + jointID, "Elasticity", jb.m_Elasticity, (float val) => { jb.m_Elasticity = val; }, restore));
                uiDictL.Add(createFloatSlider("stiffJB" + jointID, "Stiffness", jb.m_Stiffness, (float val) => { jb.m_Stiffness = val; }, restore));
                uiDictL.Add(createFloatSlider("InertJB" + jointID, "Inert", jb.m_Inert, (float val) => { jb.m_Inert = val; }, restore));
                uiDictL.Add(createFloatSlider("radiusJB" + jointID, "Radius", jb.m_Radius, (float val) => { jb.m_Radius = val; }, restore));

                uiDictL.Add(createFloatSlider("xAxis" + jointID, "Direction X", jb.m_EndOffset.x, (float val) => { jb.m_EndOffset = new Vector3(val, jb.m_EndOffset.y, jb.m_EndOffset.z); }, restore));
                uiDictL.Add(createFloatSlider("yAxis" + jointID, "Direction Y", jb.m_EndOffset.y, (float val) => { jb.m_EndOffset = new Vector3(jb.m_EndOffset.x, val, jb.m_EndOffset.z); }, restore));
                uiDictL.Add(createFloatSlider("zAxis" + jointID, "Direction Z", jb.m_EndOffset.z, (float val) => { jb.m_EndOffset = new Vector3(jb.m_EndOffset.x, jb.m_EndOffset.y, val); }, restore));

            }
            else if (jointType.Equals(VAMJoint.FIXED))
            {
                FixedJoint hj = jointD.fixedJoint;
                Rigidbody rgb = jointD.fixedJoint.GetComponent<Rigidbody>();

                SpringJoint spring = jointD.attachedSprings[0];
                Rigidbody springRGB = jointD.attachedRigidBodies[0];
              
                uiDictL.Add(createFloatSlider("spring" + jointID, "Spring", spring.spring, (float val) => { jointD.attachedSprings.ForEach(spr => spr.spring = val); }, 500f, restore));
                uiDictL.Add(createFloatSlider("damper" + jointID, "Damper", spring.damper, (float val) => { jointD.attachedSprings.ForEach(spr => spr.damper = val); }, 100f, restore));
                uiDictL.Add(createFloatSlider("massScale" + jointID, "Mass Scale", spring.massScale, (float val) => { jointD.attachedSprings.ForEach(spr => spr.massScale = val); }, 100f, restore));
                uiDictL.Add(createFloatSlider("mass" + jointID, "Mass", springRGB.mass, (float val) => { jointD.attachedRigidBodies.ForEach(rg => rg.mass = val); }, 5f, restore));
            }

            uiDictL.Add(CreateSpacer(true));   
        }

        if (uiDict.ContainsKey(jointID))
            uiDict[jointID].AddRange(uiDictL);
        else
            uiDict.Add(jointID, uiDictL);
    }

    protected UIDynamicSlider createFloatSlider(string name, string displayName, float initialVal, Action<float> settable, bool restore)
    {
        JSONStorableFloat settableVal = new JSONStorableFloat(name, initialVal, 0f, 1f);
        RegisterFloat(settableVal);        
        if (restore && pluginJson != null) settableVal.RestoreFromJSON(pluginJson);
        settableVal.setJSONCallbackFunction += delegate (JSONStorableFloat js) { settable(js.val); };
        UIDynamicSlider solverPositionWeightslider = CreateSlider(settableVal, true);
        solverPositionWeightslider.labelText.text = displayName;
        return solverPositionWeightslider;
    }

    protected UIDynamicSlider createFloatSlider(string name, string displayName, float initialVal, Action<float> settable, float maxVal, bool restore)
    {
        JSONStorableFloat settableVal = new JSONStorableFloat(name, initialVal, 0f, maxVal, false);
        RegisterFloat(settableVal);       
        if (restore && pluginJson != null) settableVal.RestoreFromJSON(pluginJson);
        settableVal.setJSONCallbackFunction += delegate (JSONStorableFloat js) { settable(js.val); };
        UIDynamicSlider solverPositionWeightslider = CreateSlider(settableVal, true);
        solverPositionWeightslider.labelText.text = displayName;
        return solverPositionWeightslider;
    }

    protected UIDynamic createToggle(string name, string displayName, bool initialVal, Action<bool> settable, bool restore)
    {
        JSONStorableBool solverFixTransforms = new JSONStorableBool(name, true);
        RegisterBool(solverFixTransforms);        
        if (restore && pluginJson != null) solverFixTransforms.RestoreFromJSON(pluginJson);
        solverFixTransforms.setJSONCallbackFunction += (delegate (JSONStorableBool js) { settable(js.val); });
        UIDynamicToggle tog = CreateToggle(solverFixTransforms, true);
        tog.labelText.text = displayName;
        return tog;
    }

    protected JSONStorableBool createToggleStorable(string name, bool initialVal, Action<bool> settable, bool restore)
    {
        JSONStorableBool solverFixTransforms = new JSONStorableBool(name, true);
        RegisterBool(solverFixTransforms);       
        if (restore && pluginJson != null) solverFixTransforms.RestoreFromJSON(pluginJson);
        solverFixTransforms.setJSONCallbackFunction += (delegate (JSONStorableBool js) { settable(js.val); });
        return solverFixTransforms;
    }

    private JSONClass extractPluginJSON(JSONNode file, string id)
    {
        JSONClass retJson = null;

        JSONNode sceneFile = file.AsObject["atoms"];

        foreach (JSONNode st in sceneFile.Childs)
        {
            if (st["id"].ToString().Equals("\"" + id + "\""))
            {

                foreach (JSONNode subSt in st["storables"].Childs)
                {
                    if (subSt["id"].ToString().Equals("\"" + storeId + "\""))
                    {
                        retJson = subSt.AsObject;
                        break;
                    }
                }
                break;
            }
        }

        return retJson;
    }

    public override void PostRestore()
    {
        pluginJson = null;

        if (!subscene)
        {
            pluginJson = extractPluginJSON(SuperController.singleton.loadJson, this.AtomUidToStoreAtomUid(this.containingAtom.uid));
            RestoreFromJSON((JSONClass)pluginJson);
        }
        else
        {

            JSONNode subsceneSave = SuperController.singleton.GetSaveJSON(this.containingAtom.parentAtom).AsObject["atoms"]; ;
            string ssPath = null;

            foreach (JSONNode st in subsceneSave.Childs)
            {
                if (st["id"].ToString().Equals("\"" + this.containingAtom.subScenePath.TrimEnd('/') + "\""))
                {
                    foreach (JSONNode subSt in st["storables"].Childs)
                    {
                        if (subSt["id"].ToString().Equals("\"" + this.containingAtom.containingSubScene.storeId + "\""))
                        {
                            pluginJson = subSt.AsObject;
                            ssPath = subSt["storePath"];
                            break;
                        }
                    }
                    break;
                }
            }

            if (ssPath != null && ssPath.Contains("/"))
            {
                try
                {
                    JSONNode subsceneNode = SuperController.singleton.LoadJSON(ssPath);
                    pluginJson = extractPluginJSON(subsceneNode, this.AtomUidToStoreAtomUid(this.containingAtom.uid).Split('/')[1]);
                }
                catch (Exception e)
                {
                    SuperController.LogError("Unable to load stored JSON: " + ssPath);
                }

                if (pluginJson != null)
                    RestoreFromJSON((JSONClass)pluginJson);
            }

        }

        base.PostRestore();

      

        StartCoroutine(restoreScene());
    }

    private void checkForOtherPlugins()
    {
        foreach (string st in this.containingAtom.GetStorableIDs())
        {
            if (st.Contains(XPSLoaderName))
            {
                XPSLoaderPluginID = st;
                attachedXPSLoader = true;
            }
        }
        
    }

    public void OnDestroy()
    {
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator restoreScene()
    {
        //do we have an XPS loader ?
        checkForOtherPlugins();
     
        CustomUnityAssetLoader dd = (CustomUnityAssetLoader)containingAtom.GetStorableByID("asset");        

        while (SuperController.singleton.isLoading)
        {
            yield return null;
        }

        //no XPS loader and we have a custom asset.
        if (!attachedXPSLoader && dd!=null)
        {
            string assetUrl = dd.GetUrlParamValue("assetUrl");
            string assetName = dd.GetStringParamValue("assetName");

            if(assetUrl!=null && assetName!=null)
            {     
              while (!dd.isAssetLoaded)
              {
                    yield return null;
                 }
            }
        }
        else if(attachedXPSLoader)
        {            
            JSONStorable xpsL = this.containingAtom.GetStorableByID(XPSLoaderPluginID);
            var bindings = new List<object>();
            if(xpsL!=null)
            { 
            xpsL.SendMessage("ModelLoadComplete", bindings, SendMessageOptions.DontRequireReceiver);
            bool modlLoad = (bool)bindings[0];

            while (!modlLoad)
            {                    
                    xpsL.SendMessage("ModelLoadComplete", bindings, SendMessageOptions.RequireReceiver);
                    modlLoad = (bool)bindings[0];
                    yield return new WaitForSeconds(2f);
                }
            }
        }
       
        refreshTransforms(subscene);

        foreach (string subSt in pluginJson.Keys)
        {
            if (subSt.StartsWith("jointConfig"))
            {
                string type = subSt.Split('_')[1];
                int counted = int.Parse(subSt.Split('_')[2]);
                string boneName = pluginJson[subSt];
                
                counter = counted;
                List<UIDynamic> uiComp = new List<UIDynamic>();
                if(type.Equals("JB"))
                {
                    setupJiggleBoneUI(uiComp, boneName, true);
                }
                else if (type.Equals("FJ"))
                {
                    setupFixedJointUI(uiComp, boneName, true);
                }

            }
        }
    }

}

