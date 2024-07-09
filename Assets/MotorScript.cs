using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEditor;

public class MoveChilds
{
    public List<string> Name { get; set; }
    public List<Vector3> Position { get; set; }
    public List<int> Level { get; set; }
    public List<Transform> Transforms { get; set; }

    public MoveChilds()
    {
        Name = new List<string>();
        Position = new List<Vector3>();
        Level = new List<int>();
        Transforms = new List<Transform>();
    }

    public void Temizle()
    {
        Name.Clear();
        Position.Clear();
        Level.Clear();
        Transforms.Clear();
    }

    public void VeriEkle(string name, Vector3 position, int level, Transform transforms)
    {
        Name.Add(name);
        Position.Add(position);
        Level.Add(level);
        Transforms.Add(transforms);
    }
}

public class MotorScript : MonoBehaviour
{    
    MoveChilds childs = new MoveChilds();

    public int distance = 4;
    private int current_level = 0;
    public float movDuration = 2;
    public Transform model = null;
    private int count = 0;

    public Dropdown Leveldropdown;
    public Dropdown Modeldropdown;
    public Button ResetButton;
    public Slider slider;

    private void ColliderAdder(Transform parent)
    {
        if (parent.childCount == 0)
        {
            if (!parent.GetComponent<Collider>())
            {
                MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    parent.gameObject.AddComponent<MeshCollider>();
                    MeshCollider meshCollider = parent.gameObject.GetComponent<MeshCollider>();
                    
                    meshCollider.sharedMesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

                    count++;
                    childs.VeriEkle(parent.gameObject.name, new Vector3(parent.position.x, parent.position.y, parent.position.z), 0, parent);
                }
                else
                {
                    childs.VeriEkle(parent.gameObject.name, new Vector3(parent.position.x, parent.position.y, parent.position.z), 0, parent);
                    Debug.LogWarning("MeshRenderer bulunamadı: " + parent.name);
                }
            }
        }
        else
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                ColliderAdder(parent.GetChild(i));
            }
        }
    }

    private void ListModels()
    {
        Modeldropdown.options.Clear();
        Modeldropdown.options.Add(new Dropdown.OptionData(""));

        GameObject[] models = Resources.LoadAll<GameObject>("");

        List<string> modelNames = new List<string>();

        foreach (GameObject model in models)
        {
            string modelName = model.name; // GameObject'un adını al

            modelNames.Add(modelName);
        }
        foreach (string modelName in modelNames)
        {
            Modeldropdown.options.Add(new Dropdown.OptionData(modelName));
        }

        Modeldropdown.onValueChanged.AddListener(delegate {
            ModelDropdownValueChanged(Modeldropdown);
        });
    }


    private void AddExpLevel(Transform model)
    {
        ColliderAdder(model);
        Vector3 model_center = new Vector3(0,0,0);
        if (model.GetComponent<MeshCollider>())
        {
            model_center = model.GetComponent<MeshCollider>().bounds.center;
        }
        for (int i = 0; i < childs.Name.Count; i++)
        {
            
            RaycastHit[] hit = Physics.RaycastAll(model_center, childs.Position[i], 100f, LayerMask.GetMask("Default"), QueryTriggerInteraction.UseGlobal);
            for (int j = 0; j < hit.Count(); j++)
            {
                if (childs.Transforms[i] == hit[j].transform)
                {
                    childs.Level[i] = hit.Count() - j;
                }
                else
                {
                    var index = childs.Transforms.FindIndex(a => a == hit[j].transform);
                    childs.Level[index] = hit.Count() - j;
                }
            }
            if (childs.Level[i] == 0)
            {
                for (int j = 0; j < hit.Count(); j++)
                {
                    Collider[] colliders = Physics.OverlapBox(hit[j].transform.GetComponent<MeshRenderer>().bounds.center, hit[j].transform.GetComponent<MeshRenderer>().bounds.size / 2);
                    var index = Array.FindIndex(colliders, a => a.transform == childs.Transforms[i]);
                    if (index != -1)
                    {
                        childs.Level[i] = hit.Count() - j;
                        for (int k = j; k < hit.Count(); k++)
                        {
                            childs.Level[k] = hit.Count() - k + 1;
                        }
                        break;
                    }
                }
            }
        }
        List<int> temp_levels = new List<int>();
        temp_levels = childs.Level.Distinct().ToList();
        temp_levels.Sort();
        for (int i = 0; i < temp_levels.Count; i++)
        {
            if (childs.Level.Any(str => str == temp_levels[i]))
            {
                int index = childs.Level.IndexOf(temp_levels[i]);
                childs.Level[index] = i + 1;
            }
        }
        AddLevels(temp_levels.Count());
    }


    void Start()
    {
        ListModels();
    }

    private void AddLevels(int count)
    {
        Leveldropdown.options.Clear();
        for (int i = 0; i <= count ; i++)
        {
            Leveldropdown.options.Add(new Dropdown.OptionData("Level " + i));
        }

        Leveldropdown.onValueChanged.AddListener(delegate {
            DropdownValueChanged(Leveldropdown);
        });
    }

    private void DropdownValueChanged(Dropdown change)
    {
        int selectedOptionIndex = change.value;
        string selectedOptionText = change.options[selectedOptionIndex].text;
        int level = int.Parse(selectedOptionText.Split(" ")[1]);

        if (level - current_level >= 0)
        {
            MoveForward(level, distance);
        }
        else
        {
            MoveBackward(level, distance);
        }
        current_level = level;
    }

    private Vector3 currentVelocity = Vector3.zero;

    private void MoveForward(int level, int child_distance)
    {
        for (int i = 0; i < childs.Name.Count; i++)
        {
            Vector3 direction = new Vector3(0, 0, 0);
            if (childs.Level[i] <= level)
            {
                if (childs.Level[i] <= current_level)
                {
                    direction = childs.Position[i];
                    direction = childs.Transforms[i].position + (direction * child_distance * (level - current_level)) / 5;
                    StartCoroutine(SmoothMove(childs.Transforms[i], direction));
                }
                else
                {
                    direction = childs.Position[i];
                    direction = childs.Transforms[i].position + (direction * child_distance * (level - childs.Level[i] + 1)) / 5;
                    StartCoroutine(SmoothMove(childs.Transforms[i], direction));
                }
            }
        }
    }

    private void MoveBackward(int level, int child_distance)
    {
        for (int i = 0; i < childs.Name.Count; i++)
        {
            Vector3 direction = new Vector3(0, 0, 0);
            if (childs.Level[i] <= current_level)
            {
                if (level == 0)
                {
                    StartCoroutine(SmoothMove(childs.Transforms[i], childs.Position[i]));
                }
                else if (childs.Level[i] <= level)
                {
                    direction = childs.Position[i]; // Negate the direction
                    int distanceMultiplier = level - current_level;
                    direction = childs.Transforms[i].position + (direction * child_distance * distanceMultiplier) / 5;
                    StartCoroutine(SmoothMove(childs.Transforms[i], direction));
                }
                else
                {
                    direction = childs.Position[i]; // Negate the direction
                    int distanceMultiplier = current_level - childs.Level[i] + 1;
                    direction = childs.Transforms[i].position - (direction * child_distance * distanceMultiplier) / 5;
                    StartCoroutine(SmoothMove(childs.Transforms[i], direction));
                }
            }
        }
    }

    private void MoveDistanceChange(int level, int child_distance)
    {
        for (int i = 0; i < childs.Name.Count; i++)
        {
            Vector3 direction = new Vector3(0, 0, 0);
            if (current_level == 0)
            {
                return;
            }
            else if (childs.Level[i] <= level)
            {
                direction = childs.Position[i] + childs.Position[i] * child_distance * (current_level - childs.Level[i] + 1)/5;
                StartCoroutine(SmoothMove(childs.Transforms[i], direction));
            }
        }
    }

    IEnumerator SmoothMove(Transform child, Vector3 targetPosition)
    {
        Vector3 currentPosition = child.position;
        Vector3 currentVelocity = Vector3.zero;

        while (Vector3.Distance(currentPosition, targetPosition) > 0.0001f)
        {
            currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref currentVelocity, .3f);
            child.position = currentPosition;
            yield return null;
        }
    }

    public void OnSliderValueChanged()
    {
        int distance_diff = (int)slider.value;
        if (distance != distance_diff)
        {
            MoveDistanceChange(current_level, distance_diff);
        }
        distance = (int)slider.value;
    }

    private void ModelDropdownValueChanged(Dropdown change)
    {
        
        int selectedOptionIndex = change.value;
        string selectedOptionText = change.options[selectedOptionIndex].text;
        if (model != null && model.name != selectedOptionText && model.gameObject.activeSelf == true)
        {
            model.gameObject.SetActive(false);
            Leveldropdown.options.Clear();
            current_level = 0;
            Leveldropdown.ClearOptions();
            Leveldropdown.value = 0;
            childs.Temizle();
            Destroy(model.gameObject);
        }
        try
        {
            GameObject fbxModel = Resources.Load<GameObject>(selectedOptionText);
            if (fbxModel)
            {
                GameObject temp = Instantiate(fbxModel, new Vector3(0, 0, 0), Quaternion.Euler(0f, 0f, 0f));
                if (temp.name == "Box(Clone)" || temp.name == "Piggy_Van(Clone)")
                {
                    temp.transform.localScale = new Vector3(15f, 15f, 15f);
                }
                else 
                {
                    temp.transform.localScale = new Vector3(2f, 2f, 2f);
                }
                model = temp.transform;
                AddExpLevel(model);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void Update()
    {
        OnSliderValueChanged();
    }
}