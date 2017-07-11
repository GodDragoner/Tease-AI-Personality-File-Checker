using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TeaseAIScriptChecker
{
    public partial class GUI : Form
    {
        public GUI()
        {
            InitializeComponent();
        }

        private System.IO.StreamWriter logFileWriter;
        private bool fileProducedErrors = false;
        private bool ignoreTeaseAIPathChange = false;
        private Dictionary<string, ArrayList> variableUsageInFiles;
        private Dictionary<string, ArrayList> flagUsageInFiles;
        private Dictionary<string, ArrayList> dateUsageInFiles;
        private Dictionary<string, ArrayList> vocabularyUsageInFiles;
        private Dictionary<string, ArrayList> gotoPointers;
        private Dictionary<string, int> jumpPoints;
        private ArrayList vocabularyFound;


        private int currentLine;
        private string currentLineString;
        private string currentFilePath;

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e) {}

        private void selectFolderButton_Click(object sender, EventArgs e)
        {

            if (!IsValidPath(textBox1.Text))
            {
                folderBrowserDialog1.SelectedPath = Directory.GetCurrentDirectory();
            } else
            {
                folderBrowserDialog1.SelectedPath = textBox1.Text;
            }

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            if (Directory.Exists(Directory.GetCurrentDirectory() + "\\System") && Directory.Exists(Directory.GetCurrentDirectory() + "\\Scripts"))
            {
                teaseAIFolderTextBox.Text = Directory.GetCurrentDirectory();
            } else
            {
                textBox1.Text = Directory.GetCurrentDirectory();
            }
        }

        private void startScanButton_Click(object sender, EventArgs e)
        {

            if (!IsValidPath(textBox1.Text))
            {
                MessageBox.Show("Please use a valid path to a folder.");
                return;
            }

            bool checkPaths = IsValidPath(teaseAIFolderTextBox.Text);

            if (checkPaths && !Directory.Exists(teaseAIFolderTextBox.Text + "\\System"))
            {
                MessageBox.Show("No 'System' folder found in the given Tease AI folder. Please select a valid folder.");
                return;
            }

            EnableControls(false);

            logFileWriter = new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "/fileCheckerLog.txt");

            if(consoleLogCheckBox.Checked)
            {
                logTextbox.Text = "";
            }

            string[] filesNames = Directory.GetFiles(textBox1.Text, "*", SearchOption.AllDirectories);
            ArrayList filesToScan = new ArrayList();

            infoTextBox.Text = "Scanning " + filesNames.Length + " files for scripts in folder '" + Path.GetFileName(textBox1.Text) + "'.";

            foreach (string filePath in filesNames)
            {
                String fileName = Path.GetFileName(filePath);
                //Check whether it is a script file
                if (fileName.EndsWith(".txt") && !fileName.Equals("fileCheckerLog.txt")) 
                {
                    filesToScan.Add(filePath);
                }
            }

            int amount = filesToScan.Count;
            scanProgressBar.Maximum = amount;

            WriteLineToLog("Starting check of " + amount + " script files in folder '" + Path.GetFileName(textBox1.Text) + "'.", false);

            if(!checkPaths)
            {
                WriteLineToLog("Skipping check of paths (videos/sounds/images) because no valid tease ai folder was set.", false);
            }

            variableUsageInFiles = new Dictionary<string, ArrayList>();
            flagUsageInFiles = new Dictionary<string, ArrayList>();
            dateUsageInFiles = new Dictionary<string, ArrayList>();
            vocabularyUsageInFiles = new Dictionary<string, ArrayList>(); ;
            vocabularyFound = new ArrayList();

            //Register default known vocab stuff
            vocabularyFound.Add("#PetName");
            vocabularyFound.Add("#SubName");
            vocabularyFound.Add("#RuinYourOrgasm");
            vocabularyFound.Add("#ShortName");
            vocabularyFound.Add("#Contact1");
            vocabularyFound.Add("#Contact2");
            vocabularyFound.Add("#Contact3");
            vocabularyFound.Add("#GeneralTime");
            vocabularyFound.Add("#GreetSub");
            vocabularyFound.Add("#DomHonorific");
            vocabularyFound.Add("#DomName");


            for (int x = 0; x < amount; x++)
            {
                currentFilePath = (string) filesToScan[x];

                fileProducedErrors = false;

                WriteLineToLog("Checking script " + currentFilePath + ".", true);

                scanProgressBar.Value = x + 1;
                infoTextBox.Text = "Checking script " + currentFilePath + " (" + x + "/" + amount + ").";

                //Check for vocabulary file
                String fileName = Path.GetFileName(currentFilePath);
                if (fileName.StartsWith("#") && currentFilePath.Contains("Vocabulary")) 
                {
                    vocabularyFound.Add(fileName.Substring(0, fileName.LastIndexOf(".txt")));
                }

                currentLine = 0;
                string line;

                gotoPointers = new Dictionary<string, ArrayList>();
                jumpPoints = new Dictionary<string, int>();

                System.IO.StreamReader file = new System.IO.StreamReader(currentFilePath);
                while ((line = file.ReadLine()) != null)
                {
                    currentLine++;
                    line = StripUselssWhitespace(line);
                    currentLineString = line;

                    // @Goto
                    int pointerAmount = 0;
                    foreach (string targetPoint in GetTextWithinRecognition(line, "@Goto(", ")", true))
                    {
                        RegisterPointerPoint(targetPoint);
                        pointerAmount++;
                    }

                    // Then
                    foreach (string targetPoint in GetTextWithinRecognition(line, "Then(", ")", true))
                    {
                        RegisterPointerPoint(targetPoint);
                    }

                    // Date difference interaction
                    foreach (string variableName in GetTextWithinRecognition(line, "#DateDifference(", ",", false))
                    {
                        RegisterDateInteraction(variableName);
                    }


                    // SetDate interaction
                    foreach (string variableName in GetTextWithinRecognition(line, "@SetDate(", ",", false))
                    {
                        RegisterDateInteraction(variableName);
                    }

                    // Check Date interaction
                    int checkDateIndex = 0;
                    foreach (string variableName in GetTextWithinRecognition(line, "@CheckDate(", ")", true))
                    {
                        if (checkDateIndex == 0)
                        {
                            RegisterDateInteraction(variableName);
                        
                        } else if(checkDateIndex == 1)
                        {
                            RegisterPointerPoint(variableName);
                        } else
                        {
                            WriteLineToLog("@CheckDate only allows exactly two parameters. More parameters found in string '" + line + "' in line " + currentLine + ". Usage: '@CheckDate(VarName, gotoLine)'.", false);
                        }

                        checkDateIndex++;
                    }

                    // CheckDate argument check
                    if(checkDateIndex == 1)
                    {
                        WriteLineToLog("@CheckDate only allows exactly two parameters. One parameter missing in string '" + line + "' in line " + currentLine + ". Usage: '@CheckDate(VarName, gotoLine)'.", false);
                    }

                    // CheckFlag (If second argument it is registered as a pointer because it is a goto line if check flag is true)
                    bool checkFlagFound = CheckLineForFlagUsage(line, "@CheckFlag(", true);

                    // Flag
                    CheckLineForFlagUsage(line, "@Flag(", true);

                    // SetFlag
                    CheckLineForFlagUsage(line, "@SetFlag(", true);

                    // DeleteFlag
                    CheckLineForFlagUsage(line, "@DeleteFlag(", true);

                    // NotFlag
                    CheckLineForFlagUsage(line, "@NotFlag(", true);

                    // TempFlag
                    CheckLineForFlagUsage(line, "@TempFlag(", true);

                    // Flag Or
                    CheckLineForFlagUsage(line, "@FlagOr(", true);

                    // PlaySound
                    if (line.Contains("@PlayAudio[") && !IsStringInComment(line, "@PlayAudio["))
                    {
                        string startString = "@PlayAudio[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        CheckPathValidity("Audio", path);
                    }

                    // ShowImage
                    if (line.Contains("@ShowImage[") && !IsStringInComment(line, "@ShowImage["))
                    {
                        string startString = "@ShowImage[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        CheckPathValidity("Images", path);
                    }

                    // ShowVideo
                    if (line.Contains("@ShowVideo[") && !IsStringInComment(line, "@ShowVideo["))
                    {
                        string startString = "@ShowVideo[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        CheckPathValidity("Video", path);
                    }

                    // CallReturn
                    if (line.Contains("@CallReturn(") && !IsStringInComment(line, "@CallReturn("))
                    {
                        string startString = "@CallReturn(";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf(")");
                        string path = remainingString.Substring(0, endIndex);
                        CheckPathValidity("", textBox1.Text, path);
                    }

                    // Call
                    if (line.Contains("@Call(") && !IsStringInComment(line, "@Call("))
                    {
                        string startString = "@Call(";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf(")");
                        string path = remainingString.Substring(0, endIndex);
                        CheckPathValidity("", textBox1.Text, path);
                    }

                    // Variable usage
                    if (line.Contains("[") && !IsStringInComment(line, "["))
                    {
                        int splitIndex = 0;
                        foreach (string substring in line.Split('['))
                        {
                            if (substring.Contains("]"))
                            {
                                int endIndex = substring.IndexOf("]");
                                string variableName = substring.Substring(0, endIndex);

                                double variable;
                                // Not an int (adding stuff or whatever) and not a function like random
                                if (!double.TryParse(variableName, out variable) && !variableName.Contains("#"))
                                {
                                    // Not an answer to a question
                                    if (!line.StartsWith("[" + variableName + "]"))
                                    {
                                        // Not a path of some sort
                                        if (!variableName.Contains("/") && !variableName.Contains("\\"))
                                        {
                                            // Ignore the second part (@CountVar[var, stop]), might need to only allow this for @CountVar
                                            if(variableName.Contains(","))
                                            {
                                                variableName = variableName.Split(',')[0];
                                            }

                                            variableName = StripUselssWhitespace(variableName);
                                            RegisterVariableInteraction(variableName);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                /*if (splitIndex > 0)
                                {
                                    writeLineToLog("Unclosed [ bracket in line " + counter + " at string '" + substring + "'.", false);
                                    fileProducedErrors = true;
                                }*/
                            }

                            splitIndex++;
                        }
                    }

                    // Vocabulary Usage
                    if (line.Contains("#") && !IsStringInComment(line, "#"))
                    {
                        int splitIndex = 0;
                        foreach (string substring in line.Split('#'))
                        {
                            // Skip stuff that we don't want
                            if(substring == null || substring.StartsWith("DateDifference(") || substring.StartsWith("Random(") || substring.Length < 1 || substring.Equals(" ") ||
                                splitIndex == 0 && !line.StartsWith("#") || substring.StartsWith("Var["))
                            {
                                continue;
                            }

                            var regex = new Regex("[^a-zA-Z_0-9]");
                            var match = regex.Match(substring);

                            int endIndex = match.Index;

                            if(endIndex == 0 )
                            {
                                endIndex = substring.Length;
                            }

                            string vocabName = StripUselssWhitespace(substring.Substring(0, endIndex));
          
                            RegisterVocabInteraction(vocabName);

                            splitIndex++;
                        }
                    }

                    // Jump point
                    if (line.StartsWith("(") && line.Contains("(") && line.Contains(")"))
                    {
                        // No @ in ()
                        if (!line.Contains("@") || line.IndexOf("@") > line.IndexOf(")"))
                        {
                            foreach (string jumpPointName in GetTextWithinRecognition(line, "(", ")", false))
                            {
                                int previousLine;
                                if (jumpPoints.TryGetValue(jumpPointName, out previousLine))
                                {
                                    WriteLineToLog("Duplicated jump point '" + jumpPointName + "' in line " + currentLine + " at string '" + line + "'. Previous same named jump point was in line " + previousLine + ".", false);
                                    fileProducedErrors = true;
                                }

                                jumpPoints[jumpPointName] = currentLine;
                                if (debugLogCheckBox.Checked)
                                {
                                    WriteLineToLog("Found jump point " + jumpPointName + " in line " + currentLine, true);
                                }
                            }
                        }
                    }

                    // Unclosed brackets
                    if (line.Contains("("))
                    {
                        int openCount = line.Split('(').Length - 1;
                        int closeCount = line.Split(')').Length - 1;

                        if (openCount != closeCount)
                        {
                            WriteLineToLog("Disparate amount of '(' / ')' brackets found in line " + currentLine + " at string '" + line + "'.", false);
                            fileProducedErrors = true;
                        }
                    }

                    if (line.Contains("["))
                    {
                        int openCount = line.Split('[').Length - 1;
                        int closeCount = line.Split(']').Length - 1;

                        if (openCount != closeCount)
                        {
                            WriteLineToLog("Disparate amount of '[' / ']' brackets found in line " + currentLine + " at string '" + line + "'.", false);
                            fileProducedErrors = true;
                        }
                    }

                    // Wrong usage of random
                    if (line.Contains("#Random["))
                    {
                        WriteLineToLog("Wrong brackets at #Random '[]' instead of'()' found in line " + currentLine + " at string '" + line + "'.", false);
                        fileProducedErrors = true;
                    }
                }

                file.Close();

                foreach (KeyValuePair<string, ArrayList> entry in gotoPointers)
                {
                    int value;
                    if(!jumpPoints.TryGetValue(entry.Key, out value))
                    {
                        if (logCheckBox.Checked)
                        {
                            foreach(string pointer in entry.Value)
                            {
                                WriteLineToLog("Found pointer '" + entry.Key + "'" + pointer + " with no jump point.", false);
                                fileProducedErrors = true;
                            }
                        }
                    }
                }

                if (fileProducedErrors)
                {
                    WriteLineToLog(" ", false);
                    WriteLineToLog("All above issues where found in file '" + currentFilePath + "'.", false);
                    WriteLineToLog("----------------------------------------------------------------------------------------------------------------------------------------------------------------", false);
                }
            }

            // Variable usage
            foreach (KeyValuePair<string, ArrayList> entry in variableUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    WriteLineToLog("Variable '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }
            }

            //Flag usage
            foreach (KeyValuePair<string, ArrayList> entry in flagUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    WriteLineToLog("Flag '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }
            }

            //Vocab usage
            foreach (KeyValuePair<string, ArrayList> entry in vocabularyUsageInFiles)
            {
                if(!vocabularyFound.Contains(entry.Key))
                {
                    WriteLineToLog("Vocabulary '" + entry.Key + "' was not known to the system" + entry.Value[0], false);
                }
            }

            //Date Usage
            foreach (KeyValuePair<string, ArrayList> entry in dateUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    WriteLineToLog("Date variable '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }

                ArrayList variableUsages;
                if(variableUsageInFiles.TryGetValue(entry.Key, out variableUsages)) {
                    foreach(string variableUsage in variableUsages)
                    {
                        WriteLineToLog("Date variable '" + entry.Key + "' was used as a normal variable too" + variableUsage, false);
                    }
                }
            }


            infoTextBox.Text = "Finished checking " + amount + " script files.";
            WriteLineToLog("Finished checking " + amount + " script files.", false);

            logFileWriter.Close();

            EnableControls(true);
        }

        private bool IsValidPath(string path)
        {
            return path != null && path.Length > 0 && Directory.Exists(path);
        }

        private void EnableControls(bool enable)
        {
            foreach (var control in this.Controls)
                ((Control)control).Enabled = enable;
        }

        private void RegisterPointerPoint(string targetPoint)
        {
            targetPoint = StripUselssWhitespace(targetPoint);

            ArrayList value;
            if (!gotoPointers.TryGetValue(targetPoint, out value))
            {
                value = new ArrayList();
                gotoPointers[targetPoint] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "' ");

            WriteLineToLog("Found pointer to " + targetPoint + " in line " + currentLine, true);
        }

        private string StripUselssWhitespace(string s)
        {
            while (s.StartsWith(" "))
            {
                s = s.Substring(1);
            }


            while (s.EndsWith(" "))
            {
                s = s.Substring(0, s.Length - 1);
            }

            return s;
        }

        private void RegisterVariableInteraction(String variableName)
        {

            ArrayList value;
            if(!variableUsageInFiles.TryGetValue(variableName, out value))
            {
                value = new ArrayList();
                variableUsageInFiles[variableName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            WriteLineToLog("Found usage of variable '" + variableName + "' in line " + currentLine, true);
        }

        private void RegisterVocabInteraction(String vocabName)
        {
            vocabName = "#" + vocabName;

            ArrayList value;
            if (!vocabularyUsageInFiles.TryGetValue(vocabName, out value))
            {
                value = new ArrayList();
                vocabularyUsageInFiles[vocabName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            WriteLineToLog("Found usage of vocabulary '" + vocabName + "' in line " + currentLine, true);
        }


        private void RegisterDateInteraction(String variableName)
        {
            ArrayList value;
            if (!dateUsageInFiles.TryGetValue(variableName, out value))
            {
                value = new ArrayList();
                dateUsageInFiles[variableName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            WriteLineToLog("Found usage of date variable '" + variableName + "' in line " + currentLine, true);
        }


        private void RegisterFlagInteraction(String flagName)
        {

            ArrayList value;
            if (!flagUsageInFiles.TryGetValue(flagName, out value))
            {
                value = new ArrayList();
                flagUsageInFiles[flagName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            WriteLineToLog("Found usage of flag '" + flagName + "' in line " + currentLine, true);
        }


        private void CheckPathValidity(string subFolder, string path)
        {
            CheckPathValidity(subFolder, teaseAIFolderTextBox.Text, path);
        }

        private void CheckPathValidity(string subFolder, string rootPath, string path)
        {
            path = path.Replace("/", "\\");

            if (!path.StartsWith("\\"))
            {
                path = "\\" + path;
            }

            if (subFolder.Length > 0)
            {
                path = "\\" + subFolder + path;
            }

            WriteLineToLog("Checking path '" + rootPath + path + "' for validity.", true);

            // Check for not random but set path
            if (!path.Contains("*"))
            {
                // Check for set path
                if (!File.Exists(rootPath + path))
                    {
                        WriteLineToLog("Invalid path to file '" + rootPath + path + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                        fileProducedErrors = true;
                }
            }
            else
            {
                string pathToFolder = path.Substring(0, path.LastIndexOf("\\"));
                if (!Directory.Exists(rootPath + pathToFolder))
                {
                    WriteLineToLog("Invalid path to folder '" + rootPath + pathToFolder + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                    fileProducedErrors = true;
                } else
                {
                    string[] filesNames = Directory.GetFiles(rootPath + pathToFolder, "*", SearchOption.AllDirectories);

                    // File doesn't matter there just needs to be one file
                    if (path.EndsWith("*.*"))
                    {
                        if(filesNames.Length == 0)
                        {
                            WriteLineToLog("Empty folder for *.* file pattern. Path '" + rootPath + pathToFolder + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                            return;
                        }
                    }

                    // Looking for a file with whatever extension but a specific name
                    if(path.EndsWith("*"))
                    {
                        string searchedFileName = path.Substring(path.LastIndexOf("\\") + 1, path.IndexOf("*") - path.LastIndexOf("\\") - 1);

                        bool foundFileFitting = false;
                        foreach (string subFilePath in filesNames)
                        {
                            String fileName = Path.GetFileName(subFilePath);

                            //TODO: Do capital letters matter in file names?
                            if (fileName.Contains(searchedFileName))
                            {
                                foundFileFitting = true;
                                break;
                            }
                        }

                        // Did not find a fitting file
                        if (!foundFileFitting)
                        {
                            WriteLineToLog("Folder did not contain any matching file with name '" + searchedFileName +  "' in line " + currentLine + ". Path '" + rootPath + pathToFolder + "'. Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                        }
                    }
                    // Looking for any file with a specific extension
                    else if(path.Contains("*."))
                    {
                        string searchedFileExtension = path.Substring(path.LastIndexOf("*.") + 2);

                        bool foundFileFitting = false;
                        foreach (string subFilePath in filesNames)
                        {
                            String fileName = Path.GetFileName(subFilePath);

                            // Do capital letters matter in file extensions?
                            if (fileName.ToLower().EndsWith("." + searchedFileExtension))
                            {
                                foundFileFitting = true;
                                break;
                            }
                        }

                        // Did not find a fitting file
                        if (!foundFileFitting)
                        {
                            WriteLineToLog("Folder did not contain any matching file with extension '" + searchedFileExtension + "' in line " + currentLine + ". Path '" + rootPath + pathToFolder + "'. Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                        }
                    }
                }
            }
        }

        private ArrayList GetTextWithinRecognition(string line, string begin, string end, bool allowEnumeration)
        {
            ArrayList textPieces = new ArrayList();
            if (line.Contains(begin) && !IsStringInComment(line, begin))
            {
                string startString = begin;
                int startIndex = line.IndexOf(startString);
                string remainingString = line.Substring(startIndex + startString.Length);
                int endIndex = remainingString.IndexOf(end);

                if(endIndex < 0)
                {
                    WriteLineToLog("Expected '" + end + "' in string '" + remainingString + "' to recognize the command. Failed to in string '" + line + "' in line " + currentLine + ".", false);
                    fileProducedErrors = true;
                    return textPieces;
                }

                string flagNames = remainingString.Substring(0, endIndex);

                // Check for multiple flagNames
                if (flagNames.Contains(","))
                {
                    if (allowEnumeration)
                    {
                        foreach (string substring in flagNames.Split(','))
                        {
                            textPieces.Add(StripUselssWhitespace(substring));
                        }
                    }
                    else
                    {
                        WriteLineToLog("Invalid enumaration '" + flagNames + "' in string '" + line + "' in line " + currentLine + ". Enumeration is not allowed for '" + begin + "'.", false);
                        fileProducedErrors = true;
                    }
                }
                else
                {
                    textPieces.Add(StripUselssWhitespace(flagNames));
                }
            }

            return textPieces;
        }

        private bool CheckLineForFlagUsage(string line, string flagRecognition, bool allowEnumeration)
        {
            int index = 0;
            string latestFlagNameFound = "";
            foreach(string flagName in GetTextWithinRecognition(line, flagRecognition, ")", allowEnumeration))
            {

                latestFlagNameFound = flagName;

                // Second argument is the goto line
                if (flagRecognition.StartsWith("@CheckFlag("))
                {
                    if (index == 1)
                    {
                        RegisterPointerPoint(flagName);
                        index++;
                        return true;
                    } else if(index > 1)
                    {
                        WriteLineToLog("@CheckFlag only allows a max of two arguments. More arguments found in string '" + line + "' in line " + currentLine + ". Usage: '@CheckFlag(flagName, gotoLine)'.", false);
                        fileProducedErrors = true;
                    }
                }

                RegisterFlagInteraction(flagName);
                index++;
            }

            //Register pointer because checkflag goes to a pointer either given or the flag name is the pointer name
            if (flagRecognition.StartsWith("@CheckFlag(") && index == 1)
            {
                //Latest FlagName should be the pointer name
                RegisterPointerPoint(latestFlagNameFound);
            }

            // Just reports whether flag in the line was found
            return index > 0;
        }

        //Not that effective
        private int GetAmountOfBracketsWithoutSmiley(string line, char bracket)
        {
            int amount = 0;
            for (int x = 0; x < line.Length; x++)
            {
                char charAt = line[x];
                if(charAt.Equals(bracket))
                {
                    if(x > 0)
                    {
                        char previousChar = line[x - 1];
                        if (previousChar.Equals(':'))
                        {
                            continue;
                        }
                        else if (x > 1)
                        {
                            char prePreChar = line[x - 2];
                            if (previousChar.Equals(':') && (previousChar.Equals('\'')))
                            {
                                continue;
                            }
                        }
                    }

                    /*if(x < line.Length - 1)
                    {
                        char nextChar = line[x + 1];
                    }*/

                    amount++;
                }
            }

            return amount;
        }

        private bool LineContainsComment(string line)
        {
            return line.Contains("//") || line.Contains("\\\\") || line.Contains("@Info(");
        }

        private bool IsStringInComment(string line, string sequence)
        {
            if(LineContainsComment(line) && line.Contains(sequence))
            {
                if(line.Contains("//"))
                {
                    return line.IndexOf("//") < line.IndexOf(sequence);
                }

                if (line.Contains("\\\\"))
                {
                    return line.IndexOf("\\\\") < line.IndexOf(sequence);
                }

                if (line.Contains("@Info("))
                {
                    return line.IndexOf("@Info(") < line.IndexOf(sequence);
                }
            }

            return false;
        }

        private void WriteLineToLog(String line, bool debug)
        {
            if(logCheckBox.Checked) {
                if (!debug || debugLogCheckBox.Checked)
                {
                    logFileWriter.WriteLine(line);
                }
            }

            if (!debug || debugLogCheckBox.Checked)
            {
                if(consoleLogCheckBox.Checked) {
                    logTextbox.Text += line + " \r\n";
                }
            }
        }

        private void debugLogCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void teaseAIFolderButton_Click(object sender, EventArgs e)
        {
            if (!IsValidPath(teaseAIFolderTextBox.Text))
            {
                teaseAIBrowserDialog.SelectedPath = Directory.GetCurrentDirectory();
            }
            else
            {
                teaseAIBrowserDialog.SelectedPath = teaseAIFolderTextBox.Text;
            }

            if (teaseAIBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                teaseAIFolderTextBox.Text = teaseAIBrowserDialog.SelectedPath;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (IsValidPath(textBox1.Text))
            {
                try
                {
                    DirectoryInfo directoryInfo = Directory.GetParent(textBox1.Text);
                    directoryInfo = directoryInfo.Parent;
                    string pathToTeaseAIDirectory = directoryInfo.FullName;
                    if (Directory.Exists(pathToTeaseAIDirectory + "\\System"))
                    {
                        ignoreTeaseAIPathChange = true;
                        teaseAIFolderTextBox.Text = pathToTeaseAIDirectory;
                        ignoreTeaseAIPathChange = false;
                    }
                } catch(Exception) { }
            }
        }

        private void teaseAIFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            if(!ignoreTeaseAIPathChange && IsValidPath(teaseAIFolderTextBox.Text))
            {
                if(Directory.Exists(teaseAIFolderTextBox.Text + "\\Scripts"))
                {
                    textBox1.Text = teaseAIFolderTextBox.Text + "\\Scripts";
                }
            }
        }
    }
}
