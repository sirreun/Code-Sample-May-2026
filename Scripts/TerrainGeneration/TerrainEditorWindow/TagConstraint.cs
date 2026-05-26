using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TagConstraint : ScriptableObject
{
    public string Name;
    public int StartingWeight = 1;
    [Header("If all weights are 1, then the options are all equally weighted. \nIf a weight is zero it will be ignored.")]
    public List<ModuleSO.NameWeightPair> Options;

    public GameObject Prefab;



    public TagConstraint(string name)
    {
        this.Name = name;
        this.Options = new List<ModuleSO.NameWeightPair>();
    }


    public TagConstraint()
    {
        this.Name = "new";
        this.Options = new List<ModuleSO.NameWeightPair>();
    }

    public override string ToString()
    {
        string output = this.Name + " with options:\n";

        foreach (ModuleSO.NameWeightPair option in Options)
        {
            output += option.ToString();
        }

        return output;
    }

    [System.Serializable]
    public class NameWeightPair
    {
        public string Name;
        public int Weight;

        public NameWeightPair(string pairName = "new", int weight = 1)
        {
            this.Name = pairName;
            this.Weight = weight;
        }

        public override string ToString()
        {
            return this.Name + " with weight " + this.Weight.ToString() + "\n";
        }
    }
}
