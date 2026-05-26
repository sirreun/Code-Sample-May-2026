using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newrulesetdata.asset", menuName = "ScriptableObjects/RulesetCounter")]
public class RulesetCounter : ScriptableObject
{
    [Tooltip("Keeps the number one above the max number used in naming a ruleset.")]
    [Header("Keeps the number one above the max \nnumber used in naming a ruleset.")]
    public int rulesetCounter = 0;
    [Tooltip("Keeps the number one above the max number used in naming a tag constraint.")]
    [Header("Keeps the number one above the max \nnumber used in naming a tag constraint.")]
    public int tagconstraintCounter = 0;
}
