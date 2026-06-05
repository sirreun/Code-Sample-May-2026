using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class WaveFunctionCollapse
{
#if UNITY_EDITOR
    private static string logFileName = "wavefunctioncollapselogfile.txt";
#endif

    private static bool addDebuggingStatementsToLog = false;
    private static bool isDebugging = true;
    private static int tileDebuggingLimit = 20000; // 
    private static bool ifRecalculatingAdjacentLimit = true;
    private static int adjacentCount = 0;
    private static int adjacentLimit = 100000;
    private static int weightCap = 10000;

    private static (int, int) north = (0 , -1);
    private static (int, int) east = (1, 0);
    private static (int, int) south = (0 , 1);
    private static (int, int) west = (-1, 0);

    private static (int, int)[] directions = {north, east, south, west};

    private static List<ModuleSO> rulesetStatic;

    public static ModuleSO[,] Generate(List<ModuleSO> ruleset, int width, int height)
    {
        int totalTiles = height * width;
        int completedTileCount = 0;
        string selectedValue = "";
        int numberOfLoops = 0;
        string message;
        rulesetStatic = new List<ModuleSO>(ruleset);
#if UNITY_EDITOR
        OpenTextLogFile();

        ///
        AddEntryToLog("Generating Terrain with " + width + " columns and " + height + " rows,");
        message = "\nRule set: \n";
        foreach (ModuleSO module in rulesetStatic)
        {
            message += module.ToString() + "\n";
        }
        AddEntryToLog(message);
        ///
#endif
        List<ModuleSO.NameWeightPair>[,] probabilityGrid = InitializeProbabilityGrid(width, height);
        ModuleSO[,] output = new ModuleSO[width,height];


        // Pick random tile to start generation
        (int, int) currentTile = (UnityEngine.Random.Range(0, width - 1), UnityEngine.Random.Range(0, height - 1));

        while (completedTileCount < totalTiles)
        {
            numberOfLoops++;

            int column = currentTile.Item1;
            int row = currentTile.Item2;
            List<ModuleSO.NameWeightPair> currentTileOptions = probabilityGrid[column, row];

            ///
            AddEntryToLog("\n-----------------------------------------------\nCurrent Tile: (" + column + ", " + row + ")");
            message = "Options for the current tile:\n" + PrintTileDomain(currentTileOptions) + "\n";
            AddEntryToLog(message);
            ///
            
            selectedValue = SelectValueFromConstraints(currentTileOptions);
            
            ///
            if (selectedValue == "")
            {
                AddEntryToLog("\n///ERROR: No possible due to mathematical error.");
                Debug.LogError("WaveFunctionCollapse.cs: Generate(): please fix mathematical error in code.");
                return output;
            }
            message = "Selected value for current tile:\n" + selectedValue + "\n";
            AddDebugStatementToLog(message);
            ///
            
            output[column,row] = GetModuleSOFromName(selectedValue);

            List<ModuleSO.NameWeightPair> generatedValue = new List<ModuleSO.NameWeightPair>();
            generatedValue.Add(new ModuleSO.NameWeightPair(selectedValue, 0));
            probabilityGrid[column, row] = generatedValue;
            ///
            AddDebugStatementToLog("Grid value:\n" + PrintTileDomain(probabilityGrid[column, row]));
            ///

            UpdateAdjacentTileDomains(column, row, width, height, probabilityGrid);

            currentTile = SelectMinimumEntropyTile(probabilityGrid, width, height);

            completedTileCount++;

            AddEntryToLog(ProbabilityGridToString(probabilityGrid, width, height));

            if (isDebugging)
            {
                if (numberOfLoops >= tileDebuggingLimit) 
                {
                    ///
                    AddDebugStatementToLog("Met Debugging limit, function stopped.");
                    ///
                    return output;
                }
                ///
                AddDebugStatementToLog("Loop number :" + numberOfLoops + "\n");
                AddDebugStatementToLog(completedTileCount + " of " + totalTiles + " completed.");
                ///
            }
        }

        ///
        AddEntryToLog("\n FINAL GRID:");
        AddEntryToLog(ProbabilityGridToString(probabilityGrid, width, height));
        ///
        
        return output;
    }

    private static ModuleSO GetModuleSOFromName(string name)
    {
        foreach (ModuleSO module in rulesetStatic)
        {
            if (module.Name == name)
            {
                return module;
            }
        }
        Debug.LogWarning(name + " not found in ruleset.");

        return new ModuleSO();
    }

    private static List<ModuleSO.NameWeightPair>[,] InitializeProbabilityGrid(int height, int width)
    {
        List<ModuleSO.NameWeightPair>[,] probabilityGrid = new List<ModuleSO.NameWeightPair>[height, width];
        List<ModuleSO.NameWeightPair> fullDomain = GetStartingWeights();//GetDomainFromModules();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                /// For every tile in the grid:
                /// Set to full domain
                probabilityGrid[i, j] = new List<ModuleSO.NameWeightPair>(fullDomain);
            }
        }

        ///
        string message = "Full Domain:\n" + PrintTileDomain(fullDomain) + "\nInitialized probability grid to:\n" + ProbabilityGridToString(probabilityGrid, height, width);
        AddEntryToLog(message);
        /// 

        return probabilityGrid;
    }

    private static List<ModuleSO.NameWeightPair> GetStartingWeights()
    {
        List<ModuleSO.NameWeightPair> domainList = new List<ModuleSO.NameWeightPair>();

        foreach (ModuleSO module in rulesetStatic)
        {
            ModuleSO.NameWeightPair newConstraint = new ModuleSO.NameWeightPair(module.Name, module.StartingWeight);
            domainList.Add(newConstraint);       
        }

        return domainList;
    }

    /// <summary>
    ///  Used for initializing the grid domains.
    /// </summary>
    /// <param name="ruleset"></param>
    /// <returns></returns>
    private static List<ModuleSO.NameWeightPair> GetDomainFromModules()
    {
        List<ModuleSO.NameWeightPair> domainList = new List<ModuleSO.NameWeightPair>();
        int index;

        foreach (ModuleSO module in rulesetStatic)
        {
            foreach (ModuleSO.NameWeightPair constraint in module.Constraints)
            {
                ModuleSO.NameWeightPair newConstraint = new ModuleSO.NameWeightPair(constraint.Name, constraint.Weight);
                
                /// If already in domain, add weight 
                if (ListContainsNameWeightPair(domainList, constraint, out index))
                {
                    domainList[index].Weight += constraint.Weight;
                }
                else
                {
                    domainList.Add(newConstraint);
                }
            }         
        }

        return domainList;
    }

    private static List<ModuleSO.NameWeightPair> OrDomains(List<ModuleSO.NameWeightPair> domainOne, List<ModuleSO.NameWeightPair> domainTwo)
    {
        if (domainOne.Count == 0)
        {
            return domainTwo;
        }
        else if (domainTwo.Count == 0)
        {
            return domainOne;
        }
        List<ModuleSO.NameWeightPair> output = new List<ModuleSO.NameWeightPair>();

        ///
        AddDebugStatementToLog("\tAdding (OR) domain\n" + PrintTileDomain(domainOne) + " to domain\n" + PrintTileDomain(domainTwo));
        AddDebugStatementToLog("\tOutput:");
        ///
        
        int index;

        foreach (ModuleSO.NameWeightPair constraintTwo in domainTwo) 
        {
            if (ListContainsNameWeightPair(domainOne, constraintTwo, out index))
            {
                int minWeight = Math.Min(domainOne[index].Weight, constraintTwo.Weight);
                ModuleSO.NameWeightPair newConstraint = new ModuleSO.NameWeightPair(constraintTwo.Name, minWeight);

                output.Add(newConstraint);
                ///
                AddDebugStatementToLog("\t+ " + newConstraint.ToString());
                /// 
            }
        }

        return output;
    }

    private static List<ModuleSO.NameWeightPair> AndDomains(List<ModuleSO.NameWeightPair> domainOne, List<ModuleSO.NameWeightPair> domainTwo)
    {
        if (domainOne.Count == 0)
        {
            return domainTwo;
        }
        else if (domainTwo.Count == 0)
        {
            return domainOne;
        }

        ///
        AddDebugStatementToLog("\tAdding (AND) domain\n" + PrintTileDomain(domainOne) + " to domain\n" + PrintTileDomain(domainTwo));
        AddDebugStatementToLog("\tOutput:");
        ///
        int index;
        bool downsizingNeeded = false;
        float denominator = 100f;
        int maxWeight = 0;

        foreach (ModuleSO.NameWeightPair constraintTwo in domainTwo) 
        {
            if (ListContainsNameWeightPair(domainOne, constraintTwo, out index))
            {
                domainOne[index].Weight += constraintTwo.Weight;
                ///
                AddDebugStatementToLog("\t^ " + domainOne[index].ToString());
                /// 
                int domainOneWeight = domainOne[index].Weight;
                if (domainOneWeight > weightCap)
                {
                    downsizingNeeded = true;
                    if (maxWeight < domainOneWeight)
                    {
                        maxWeight = domainOneWeight;
                    }
                }
            }
            else
            {
                domainOne.Add(constraintTwo);
                ///
                AddDebugStatementToLog("\t+ " + constraintTwo.ToString());
                ///
            }
        }
        

        if (downsizingNeeded)
        {
            foreach (ModuleSO.NameWeightPair constraint in domainOne)
            {
                constraint.Weight = (int)Math.Ceiling(constraint.Weight / denominator);
            }
        }

        return domainOne;
    }

    private static bool ListContainsNameWeightPair(List<ModuleSO.NameWeightPair> list, ModuleSO.NameWeightPair constraint, out int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            ModuleSO.NameWeightPair pair = list[i];
            if (pair.Name == constraint.Name)
            {
                index = i;
                return true;
            }
        }
        index = list.Count - 1;
        return false;
    }

    private static string ProbabilityGridToString(List<ModuleSO.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        string output = "";

        for (int i = 0; i < width; i++) 
        { 
            for (int j = 0; j < height; j++)
            {
                if (probabilityGrid[i,j].First().Weight != 0)
                {
                    // Not yet selected, is variable
                    output += "~, ";
                }
                else
                {
                    output += probabilityGrid[i, j].First().Name + ", ";
                }
            }
            output += "\n";
        }

        return output;
    }

    private static string SelectValueFromConstraints(List<ModuleSO.NameWeightPair> tileOptions)
    {
        string output = "";

        int totalWeight = 0;
        foreach (ModuleSO.NameWeightPair tileOption in tileOptions)
        {
            totalWeight += tileOption.Weight;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        foreach (ModuleSO.NameWeightPair tile in tileOptions)
        {
            if (randomValue < tile.Weight)
            {
                output = tile.Name;
                break;
            }

            randomValue = randomValue - tile.Weight;
        }

        return output;
    }

    private static void UpdateAdjacentTileDomains(int column, int row, int width, int height, List<ModuleSO.NameWeightPair>[,] probabilityGrid)
    {
        CheckAllAdjacentTiles((0,0), (column, row), probabilityGrid, width, height); // (0,0) since there is no back direction
    }

    private static bool MoveInDirection(int column, int row, (int, int) direction, int width, int height, out (int, int) output)
    {
        output = (column + direction.Item1, row + direction.Item2);

        if (output.Item1 >= width || output.Item2 >= height)
        {
            ///
            AddDebugStatementToLog(" X Unable to move in this direction " + direction + "\n   " + output.ToString() + " is out of bounds for a " + width + " x " + height + " grid.\n");
            ///
            
            return false;
        }
        else if (output.Item1 < 0 || output.Item2 < 0)
        {
            ///
            AddDebugStatementToLog(" X Unable to move in this direction " + direction + "\n   " + output.ToString() + " is out of bounds for a " + width + " x " + height + " grid.\n");
            ///
            
            return false;
        }
        ///
        AddDebugStatementToLog(" > Moving to " + output.ToString() + "\n");
        ///

        return true;
    }

    private static void CheckAllAdjacentTiles((int, int) backDirection, (int, int) tile, List<ModuleSO.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        /// Used for debugging to reduce crashes
        adjacentCount++;
        if ((adjacentCount >= adjacentLimit) && ifRecalculatingAdjacentLimit)
        {
            return;
        }

        int column = tile.Item1;
        int row = tile.Item2;

        if (north != backDirection)
        {
            ///
            AddDebugStatementToLog("Checking north of " + tile.ToString() + "\n");
            ///

            (int, int) adjacentTile;
            bool canMove = MoveInDirection(column, row, north, width, height, out adjacentTile);
            
            if (canMove)
            {
                CheckAdjacentTileForDomainChange(north, adjacentTile, probabilityGrid, width, height);
                ///
                AddDebugStatementToLog(" < Returned to " + tile.ToString() + "\n");
                ///
            }
        }

        if (east != backDirection)
        {
            ///
            AddDebugStatementToLog("Checking east of " + tile.ToString() + "\n");
            ///
            (int, int) adjacentTile;
            bool canMove = MoveInDirection(column, row, east, width, height, out adjacentTile);
            
            if (canMove)
            {
                CheckAdjacentTileForDomainChange(east, adjacentTile, probabilityGrid, width, height);
                ///
                AddDebugStatementToLog("Returned to " + tile.ToString() + "\n");
                ///
            }
        }

        if (south != backDirection)
        {
            ///
            AddDebugStatementToLog("Checking south of " + tile.ToString() + "\n");
            ///
            (int, int) adjacentTile;
            bool canMove = MoveInDirection(column, row, south, width, height, out adjacentTile);
            
            if (canMove)
            {
                CheckAdjacentTileForDomainChange(south, adjacentTile, probabilityGrid, width, height);
                ///
                AddDebugStatementToLog("Returned to " + tile.ToString() + "\n");
                ///
            }
        }

        if (west != backDirection)
        {
            ///
            AddDebugStatementToLog("Checking west of " + tile.ToString() + "\n");
            ///
            (int, int) adjacentTile;
            bool canMove = MoveInDirection(column, row, west, width, height, out adjacentTile);
            
            if (canMove)
            {
                CheckAdjacentTileForDomainChange(west, adjacentTile, probabilityGrid, width, height);
            }
        }
    }

    private static void CheckAdjacentTileForDomainChange((int, int) explorationDirection, (int, int) tile, List<ModuleSO.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        if (probabilityGrid[tile.Item1, tile.Item2].First().Weight == 0 && probabilityGrid[tile.Item1, tile.Item2].Count == 1)
        {
            ///
            AddDebugStatementToLog("\tTile already selected, skipping...");
            ///
            return;
        }
        else 
        {
            AddDebugStatementToLog(" > Check AdjacentTileForDomainChange");
        }

        List<ModuleSO.NameWeightPair> oldDomain = probabilityGrid[tile.Item1, tile.Item2]; // column, row
        List<ModuleSO.NameWeightPair> newDomain = new List<ModuleSO.NameWeightPair>(); 
        ///
        AddDebugStatementToLog("\tChecking " + tile.ToString() + "'s domain :\nOld domain:\n" + PrintTileDomain(oldDomain));
        ///

        (int, int) adjacentTile;
        int i = 1;
        bool setNewDomain = false;

        foreach ((int, int) direction in directions)
        {
            List<ModuleSO.NameWeightPair> newDirectionDomain = new List<ModuleSO.NameWeightPair>();
            bool canMove = MoveInDirection(tile.Item1, tile.Item2, direction, width, height, out adjacentTile);

            if (canMove)
            {
                ///
                AddDebugStatementToLog("\t" + i + ". Getting domain from " + adjacentTile.ToString());
                AddDebugStatementToLog("\t\t" + adjacentTile.ToString() + " has domain:\n" + PrintTileDomain(probabilityGrid[adjacentTile.Item1, adjacentTile.Item2]));
                /// 
                if (!setNewDomain)
                {
                    AddDebugStatementToLog("\tSetting new domain to grid constraints from tile " + adjacentTile.ToString());
                    if (probabilityGrid[adjacentTile.Item1, adjacentTile.Item2].First().Weight == 0 && probabilityGrid[adjacentTile.Item1, adjacentTile.Item2].Count == 1)
                    {
                        ///
                        AddDebugStatementToLog("\tTile already selected, grabbing domain from ruleset.");
                        /// 
                        newDirectionDomain = GetConstraintsForID(probabilityGrid[adjacentTile.Item1, adjacentTile.Item2].First().Name);
                        
                    }
                    else 
                    {
                        newDirectionDomain = probabilityGrid[adjacentTile.Item1, adjacentTile.Item2];
                    }
                    ///
                    AddDebugStatementToLog("\tInitialized new domain.");
                    /// 
                    newDomain = newDirectionDomain;
                    setNewDomain = true;
                }
                else 
                {
                    if (probabilityGrid[adjacentTile.Item1, adjacentTile.Item2].First().Weight == 0)
                    {
                        ///
                        AddDebugStatementToLog("\tTile already selected, grabbing domain from ruleset.");
                        /// 
                        newDirectionDomain = GetConstraintsForID(probabilityGrid[adjacentTile.Item1, adjacentTile.Item2].First().Name);

                    }
                    else 
                    {
                        newDirectionDomain = probabilityGrid[adjacentTile.Item1, adjacentTile.Item2];
                    }

                    newDomain = OrDomains(newDomain, newDirectionDomain);
                }

                ///
                AddDebugStatementToLog("\tAdjacent tile domain is:\n" + PrintTileDomain(newDirectionDomain));
                ///
            }

            i++;
        }

        if (newDomain.Count == 0)
        {
            ///
            AddEntryToLog("///ERROR: domain addition has caused an empty domain, ruleset issue (or later implement a way to redo chunks)");
            /// 
        }

        probabilityGrid[tile.Item1, tile.Item2] = newDomain;

        ///
        AddEntryToLog(EntropyGridToString(probabilityGrid, width, height));
        /// 

        if (oldDomain.Count == newDomain.Count)
        {
            // No Change
            if (addDebuggingStatementsToLog)
            {
                AddEntryToLog(PrintTileDomain(oldDomain) + " equals\n" + PrintTileDomain(newDomain));
            }
            return;
        }
        else 
        {
            // Check new changes in adjacent tiles
            ///
            string message = "Domain changed to\n" + PrintTileDomain(newDomain) + "in " + tile.ToString() + ".\n";
            AddDebugStatementToLog(message);
            ///

            (int, int) backDirection = GetOppositeDirection(explorationDirection);
        }
    }

    private static (int, int) GetOppositeDirection((int, int) direction)
    {
        return (direction.Item1 * -1, direction.Item2 * -1);
    }

    private static string PrintTileDomain(List<ModuleSO.NameWeightPair> tiles)
    {
        string message = "";
        foreach (ModuleSO.NameWeightPair pair in tiles)
        {
            message += pair.ToString();
        }

        return message;
    }

    // Decide new current tile using entropy calculations, ignore tiles with a 0 in weight.
    // A list with one item has the lowest entropy, while a long list with equal weights has more entropy than a shorter list
    // and/or list with uneven weights.
    private static (int, int) SelectMinimumEntropyTile(List<ModuleSO.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        int minimumWidth = 0;
        int minimumHeight = 0;
        double? minimumEntropy = null;

        List<(int, int)> tilesWithOneOption = new List<(int, int)> ();

        for (int i = 0; i < width; i++)
        { 
            for (int j = 0; j < height; j++)
            {
                List<ModuleSO.NameWeightPair> currentTile = probabilityGrid[i, j];
                int numberOfOptions = currentTile.Count;
                // check if value has been assigned yet, if not return, else skip
                if (numberOfOptions == 1)
                {
                    if (currentTile[0].Weight != 0)
                    {
                        tilesWithOneOption.Add((i, j));
                    }
                }
                else if (numberOfOptions == 0)
                {
                    AddEntryToLog("///ERROR: CalculateMinimumEntropy: No options for tile at (" + i + ", " + j + ").");
                    Debug.LogError("No options for this tile, something has gone terribly wrong.");
                }
                else
                { 
                    List<double> surprise = new List<double>();
                    List<double> probabilities = new List<double>();
                    double weightDenominator = 0;

                    foreach (ModuleSO.NameWeightPair option in currentTile)
                    {
                        weightDenominator += option.Weight;
                    }

                    foreach (ModuleSO.NameWeightPair option in currentTile)
                    {
                        try
                        {
                            double probability = option.Weight / weightDenominator;
                            probabilities.Add(probability);
                            surprise.Add(System.Math.Log(probability, numberOfOptions));
                        }
                        catch
                        {
                            AddEntryToLog("///ERROR: CalculateMinimumEntropy: Dividing by zero.");
                            Debug.LogError("CalculateMinimumEntropy: Dividing by zero, something has gone terribly wrong.");
                        }   
                    }

                    double entropy = 0;

                    for(int k = 0; k < numberOfOptions; k++)
                    {
                        entropy += (probabilities[k] * surprise[k]);
                    }

                    entropy *= -1;

                    ///
                    string message = "Entropy is " + entropy;
                    AddDebugStatementToLog(message);
                    ///

                    if (minimumEntropy != null)
                    {
                        if (entropy < minimumEntropy)
                        {
                            minimumEntropy = entropy;
                            minimumWidth = i;
                            minimumHeight = j;
                        }
                    }
                    else
                    {
                        minimumEntropy = entropy;
                        minimumWidth = i;
                        minimumHeight = j;
                    }
                }
            }
        }
        if (tilesWithOneOption.Count > 0)
        {
            /// Return a random tile from the list
            int index = UnityEngine.Random.Range(0, tilesWithOneOption.Count);
            return tilesWithOneOption[index];
        }

        (int, int) minimumEntropyCoordinates = (minimumWidth, minimumHeight);
        return minimumEntropyCoordinates;
    }

    private static string EntropyGridToString(List<ModuleSO.NameWeightPair>[,] probabilityGrid, int width, int height)
    {
        string output = "";

        for (int i = 0; i < width; i++) 
        { 
            for (int j = 0; j < height; j++)
            {
                if (probabilityGrid[i,j].First().Weight != 0)
                {
                    output += probabilityGrid[i,j].Count + ", ";
                }
                else
                {
                    output += probabilityGrid[i, j].First().Name + ", ";
                }
            }
            output += "\n";
        }

        return output;
    }

    private static List<ModuleSO.NameWeightPair> GetConstraintsForID(string ID)
    {
        foreach (ModuleSO module in rulesetStatic)
        {
            if (module.ID == ID)
            {
                List<ModuleSO.NameWeightPair> output = new List<ModuleSO.NameWeightPair>(module.Constraints.Count);
                module.Constraints.ForEach((constraint) =>
                {
                    output.Add(new ModuleSO.NameWeightPair(constraint));
                });
                return output;
            }
        }
        ///
        AddEntryToLog("///ERROR: ID: " + ID + " not found in ruleset");
        Debug.LogWarning("ID: " + ID + " not found in ruleset");
        ///

        return new List<ModuleSO.NameWeightPair>();
    }

#if UNITY_EDITOR
    private static void OpenTextLogFile()
    {
        /// Overwrites the previous logfile if it exists
        string path = "Assets/TerrainOutput/" + logFileName;
        StreamWriter writer = new StreamWriter(path, false);
        writer.Close();
    }
#endif

    private static void AddEntryToLog(string message)
    {
#if UNITY_EDITOR
        string path = "Assets/TerrainOutput/" + logFileName;
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(message);
        writer.Close();
#endif
    }

    private static void AddDebugStatementToLog(string message)
    {
#if UNITY_EDITOR
        if (!addDebuggingStatementsToLog)
        {
            return;
        }
        AddEntryToLog(message);
#endif
    }
}

public class Module 
{
    public string ID;
    public List<ModuleSO.NameWeightPair> Constraints; 

    public Module(string id)
    {
        this.ID = id;
        this.Constraints = new List<ModuleSO.NameWeightPair>();
    }

    public Module(ModuleSO moduleSO)
    {
        this.ID = moduleSO.ID;
        this.Constraints = new List<ModuleSO.NameWeightPair>();
    }

    public Module(TagConstraint tagconstraint)
    {
        ID = tagconstraint.Name;
        Constraints = tagconstraint.Options;
    }

    public override string ToString()
    {
        string output = this.ID + " with constraints:\n";

        foreach (ModuleSO.NameWeightPair constraint in Constraints)
        {
            output += constraint.ToString();
        }

        return output;
    }
}

public class ModuleSO // : ScriptableObject
{
    public string Name;
    public string ID;
    public int StartingWeight = 1;
    [Header("If all weights are 1, then the options are all equally weighted. \nIf a weight is zero it will be ignored.")]
    public List<ModuleSO.NameWeightPair> Constraints;
    public GameObject Prefab;
    
    public ModuleSO(string name)
    {
        this.Name = name;
        this.Constraints = new List<NameWeightPair>();
    }

    public ModuleSO()
    {
        this.Name = "new";
        this.Constraints = new List<NameWeightPair>();
    }

    public ModuleSO(TagConstraint tagconstraint)
    {
        ID = tagconstraint.Name;
        Name = tagconstraint.Name;
        Constraints = tagconstraint.Options;
        Prefab = tagconstraint.Prefab;
        StartingWeight = tagconstraint.StartingWeight;
    }

    public ModuleSO(Module module, string name, GameObject prefab){
        this.Name = name;
        this.ID = module.ID;
        this.Constraints = module.Constraints;
        this.Prefab = prefab;
    }

    public override string ToString()
    {
        string output = this.Name + " with constraints:\n";

        foreach (NameWeightPair constraint in Constraints)
        {
            output += constraint.ToString();
        }

        return output;
    }

    [System.Serializable]
    public class NameWeightPair
    {
        public string Name;
        //[Range(1,100)]
        public int Weight;

        public NameWeightPair(string pairName = "new", int weight = 1)
        {
            this.Name = pairName;
            if (weight < 0){
                weight = 0;
            }
            this.Weight = weight;
        }

        public NameWeightPair(NameWeightPair nwp)
        {
            this.Name = nwp.Name;
            if (nwp.Weight < 0){
                nwp.Weight = 0;
            }
            this.Weight = nwp.Weight;
        }

        public override string ToString()
        {
            return this.Name + " with weight " + this.Weight.ToString() + "\n";
        }
    }
}
