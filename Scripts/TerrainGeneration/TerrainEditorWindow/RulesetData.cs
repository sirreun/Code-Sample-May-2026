using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newrulesetdata.asset", menuName = "ScriptableObjects/Ruleset")]
public class RulesetData : ScriptableObject
{
     public string r_name;

     [SerializeField] public List<TagConstraint> ruleset;
 

     public RulesetData(List<TagConstraint> iRuleset, string iName = "new")
     {
          this.r_name = iName;
          this.ruleset = iRuleset;
     }

     public List<ModuleSO> GetModuleList(){
          List<ModuleSO> output = new List<ModuleSO>();

          foreach (TagConstraint tagconstraint in ruleset){
               ModuleSO newModule = new ModuleSO(tagconstraint);
               output.Add(newModule);
          }

          return output;
     }
}