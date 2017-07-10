using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private Dictionary<string, int> gotoPointers;
        private Dictionary<string, int> jumpPoints;
        private ArrayList vocabularyFound;


        private int currentLine;
        private string currentLineString;
        private string currentFilePath;

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e) {}

        private void selectFolderButton_Click(object sender, EventArgs e)
        {

            if (!isValidPath(textBox1.Text))
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

            if (!isValidPath(textBox1.Text))
            {
                MessageBox.Show("Please use a valid path to a folder.");
                return;
            }

            bool checkPaths = isValidPath(teaseAIFolderTextBox.Text);

            if (checkPaths && !Directory.Exists(teaseAIFolderTextBox.Text + "\\System"))
            {
                MessageBox.Show("No 'System' folder found in the given Tease AI folder. Please select a valid folder.");
                return;
            }

            enableControls(false);

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

            writeLineToLog("Starting check of " + amount + " script files in folder '" + Path.GetFileName(textBox1.Text) + "'.", false);

            if(!checkPaths)
            {
                writeLineToLog("Skipping check of paths (videos/sounds/images) because no valid tease ai folder was set.", false);
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

                writeLineToLog("Checking script " + currentFilePath + ".", true);

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

                gotoPointers = new Dictionary<string, int>();
                jumpPoints = new Dictionary<string, int>();

                System.IO.StreamReader file = new System.IO.StreamReader(currentFilePath);
                while ((line = file.ReadLine()) != null)
                {
                    currentLine++;
                    line = stripUselssWhitespace(line);
                    currentLineString = line;

                    //@Goto
                    foreach (string targetPoint in getTextWithinRecognition(line, "@Goto(", ")", true))
                    {
                        registerPointerPoint(targetPoint);
                    }

                    //Then
                    foreach (string targetPoint in getTextWithinRecognition(line, "Then(", ")", true))
                    {
                        registerPointerPoint(targetPoint);
                    }

                    //Date difference interaction
                    foreach (string variableName in getTextWithinRecognition(line, "#DateDifference(", ",", false))
                    {
                        registerDateInteraction(variableName);
                    }


                    //SetDate interaction
                    foreach (string variableName in getTextWithinRecognition(line, "@SetDate(", ",", false))
                    {
                        registerDateInteraction(variableName);
                    }

                    //SetDate interaction
                    int checkDateIndex = 0;
                    foreach (string variableName in getTextWithinRecognition(line, "@CheckDate(", ",", false))
                    {
                        registerDateInteraction(variableName);
                        checkDateIndex++;
                    }

                    //CheckFlag
                    checkLineForFlagUsage(line, "@CheckFlag(", true);

                    //Flag
                    checkLineForFlagUsage(line, "@Flag(", true);

                    //SetFlag
                    checkLineForFlagUsage(line, "@SetFlag(", false);

                    //DeleteFlag
                    checkLineForFlagUsage(line, "@DeleteFlag(", false);

                    //NotFlag
                    checkLineForFlagUsage(line, "@NotFlag(", true);

                    //TempFlag
                    checkLineForFlagUsage(line, "@TempFlag(", false);

                    //PlaySound
                    if (line.Contains("@PlayAudio[") && !isStringInComment(line, "@PlayAudio["))
                    {
                        string startString = "@PlayAudio[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        checkPathValidity("Audio", path);
                    }

                    //ShowImage
                    if (line.Contains("@ShowImage[") && !isStringInComment(line, "@ShowImage["))
                    {
                        string startString = "@ShowImage[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        checkPathValidity("Images", path);
                    }

                    //ShowVideo
                    if (line.Contains("@ShowVideo[") && !isStringInComment(line, "@ShowVideo["))
                    {
                        string startString = "@ShowVideo[";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf("]");
                        string path = remainingString.Substring(0, endIndex);
                        checkPathValidity("Video", path);
                    }

                    //CallReturn
                    if (line.Contains("@CallReturn(") && !isStringInComment(line, "@CallReturn("))
                    {
                        string startString = "@CallReturn(";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf(")");
                        string path = remainingString.Substring(0, endIndex);
                        checkPathValidity("", textBox1.Text, path);
                    }

                    //Call
                    if (line.Contains("@Call(") && !isStringInComment(line, "@Call("))
                    {
                        string startString = "@Call(";
                        int startIndex = line.IndexOf(startString);
                        string remainingString = line.Substring(startIndex + startString.Length);
                        int endIndex = remainingString.IndexOf(")");
                        string path = remainingString.Substring(0, endIndex);
                        checkPathValidity("", textBox1.Text, path);
                    }

                    //Variable usage
                    if (line.Contains("[") && !isStringInComment(line, "["))
                    {
                        int splitIndex = 0;
                        foreach (string substring in line.Split('['))
                        {
                            if (substring.Contains("]"))
                            {
                                int endIndex = substring.IndexOf("]");
                                string variableName = substring.Substring(0, endIndex);

                                double variable;
                                //Not an int (adding stuff or whatever) and not a function like random
                                if (!double.TryParse(variableName, out variable) && !variableName.Contains("#"))
                                {
                                    //Not an answer to a question
                                    if (!line.StartsWith("[" + variableName + "]"))
                                    {
                                        //Not a path of some sort
                                        if (!variableName.Contains("/") && !variableName.Contains("\\"))
                                        {
                                            //Ignore the second part (@CountVar[var, stop]), might need to only allow this for @CountVar
                                            if(variableName.Contains(","))
                                            {
                                                variableName = variableName.Split(',')[0];
                                            }

                                            variableName = stripUselssWhitespace(variableName);
                                            registerVariableInteraction(variableName);
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

                    //Vocabulary Usage
                    if (line.Contains("#") && !isStringInComment(line, "#"))
                    {
                        int splitIndex = 0;
                        foreach (string substring in line.Split('#'))
                        {
                            //Skip stuff that we don't want
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

                            string vocabName = stripUselssWhitespace(substring.Substring(0, endIndex));
          
                            registerVocabInteraction(vocabName);

                            splitIndex++;
                        }
                    }

                    //Jump point
                    if (line.StartsWith("(") && line.Contains("(") && line.Contains(")"))
                    {
                        //No @ in ()
                        if (!line.Contains("@") || line.IndexOf("@") > line.IndexOf(")"))
                        {
                            foreach (string jumpPointName in getTextWithinRecognition(line, "(", ")", false))
                            {
                                int previousLine;
                                if (jumpPoints.TryGetValue(jumpPointName, out previousLine))
                                {
                                    writeLineToLog("Duplicated jump point '" + jumpPointName + "' in line " + currentLine + " at string '" + line + "'. Previous same named jump point was in line " + previousLine + ".", false);
                                    fileProducedErrors = true;
                                }

                                jumpPoints[jumpPointName] = currentLine;
                                if (debugLogCheckBox.Checked)
                                {
                                    writeLineToLog("Found jump point " + jumpPointName + " in line " + currentLine, true);
                                }
                            }
                        }
                    }

                    //Unclosed brackets
                    if (line.Contains("("))
                    {
                        int openCount = line.Split('(').Length - 1;
                        int closeCount = line.Split(')').Length - 1;

                        if (openCount != closeCount)
                        {
                            writeLineToLog("Disparate amount of '(' / ')' brackets found in line " + currentLine + " at string '" + line + "'.", false);
                            fileProducedErrors = true;
                        }
                    }

                    if (line.Contains("["))
                    {
                        int openCount = line.Split('[').Length - 1;
                        int closeCount = line.Split(']').Length - 1;

                        if (openCount != closeCount)
                        {
                            writeLineToLog("Disparate amount of '[' / ']' brackets found in line " + currentLine + " at string '" + line + "'.", false);
                            fileProducedErrors = true;
                        }
                    }

                    //Wrong usage of random
                    if (line.Contains("#Random["))
                    {
                        writeLineToLog("Wrong brackets at #Random '[]' instead of'()' found in line " + currentLine + " at string '" + line + "'.", false);
                        fileProducedErrors = true;
                    }
                }

                file.Close();

                foreach (KeyValuePair<string, int> entry in gotoPointers)
                {
                    int value;
                    if(!jumpPoints.TryGetValue(entry.Key, out value))
                    {
                        if (logCheckBox.Checked)
                        {
                            writeLineToLog("Found pointer '" + entry.Key + "' in line " + (entry.Value) + " with no jump point.", false);
                            fileProducedErrors = true;
                        }
                    }
                }

                if (fileProducedErrors)
                {
                    writeLineToLog(" ", false);
                    writeLineToLog("All above issues where found in file '" + currentFilePath + "'.", false);
                    writeLineToLog("----------------------------------------------------------------------------------------------------------------------------------------------------------------", false);
                }
            }

            //Variable usage
            foreach (KeyValuePair<string, ArrayList> entry in variableUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    writeLineToLog("Variable '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }
            }

            //Flag usage
            foreach (KeyValuePair<string, ArrayList> entry in flagUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    writeLineToLog("Flag '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }
            }

            //Vocab usage
            foreach (KeyValuePair<string, ArrayList> entry in vocabularyUsageInFiles)
            {
                if(!vocabularyFound.Contains(entry.Key))
                {
                    writeLineToLog("Vocabulary '" + entry.Key + "' was not known to the system" + entry.Value[0], false);
                }
            }

            //Date Usage
            foreach (KeyValuePair<string, ArrayList> entry in dateUsageInFiles)
            {
                if (entry.Value.Count <= 1)
                {
                    writeLineToLog("Date variable '" + entry.Key + "' was only used once" + entry.Value[0], false);
                }

                ArrayList variableUsages;
                if(variableUsageInFiles.TryGetValue(entry.Key, out variableUsages)) {
                    foreach(string variableUsage in variableUsages)
                    {
                        writeLineToLog("Date variable '" + entry.Key + "' was used as a normal variable too" + variableUsage, false);
                    }
                }
            }


            infoTextBox.Text = "Finished checking " + amount + " script files.";
            writeLineToLog("Finished checking " + amount + " script files.", false);

            logFileWriter.Close();

            enableControls(true);
        }

        private bool isValidPath(string path)
        {
            return path != null && path.Length > 0 && Directory.Exists(path);
        }

        private void enableControls(bool enable)
        {
            foreach (var control in this.Controls)
                ((Control)control).Enabled = enable;
        }

        private void registerPointerPoint(string targetPoint)
        {
            targetPoint = stripUselssWhitespace(targetPoint);

            gotoPointers[targetPoint] = currentLine;
            writeLineToLog("Found pointer to " + targetPoint + " in line " + currentLine, true);
        }

        private string stripUselssWhitespace(string s)
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

        private void registerVariableInteraction(String variableName)
        {

            ArrayList value;
            if(!variableUsageInFiles.TryGetValue(variableName, out value))
            {
                value = new ArrayList();
                variableUsageInFiles[variableName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            writeLineToLog("Found usage of variable '" + variableName + "' in line " + currentLine, true);
        }

        private void registerVocabInteraction(String vocabName)
        {
            vocabName = "#" + vocabName;

            ArrayList value;
            if (!vocabularyUsageInFiles.TryGetValue(vocabName, out value))
            {
                value = new ArrayList();
                vocabularyUsageInFiles[vocabName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            writeLineToLog("Found usage of vocabulary '" + vocabName + "' in line " + currentLine, true);
        }


        private void registerDateInteraction(String variableName)
        {
            ArrayList value;
            if (!dateUsageInFiles.TryGetValue(variableName, out value))
            {
                value = new ArrayList();
                dateUsageInFiles[variableName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            writeLineToLog("Found usage of date variable '" + variableName + "' in line " + currentLine, true);
        }


        private void registerFlagInteraction(String flagName)
        {

            ArrayList value;
            if (!flagUsageInFiles.TryGetValue(flagName, out value))
            {
                value = new ArrayList();
                flagUsageInFiles[flagName] = value;
            }

            value.Add(" in string '" + currentLineString + "' in line " + currentLine + " in file '" + currentFilePath + "'.");

            writeLineToLog("Found usage of flag '" + flagName + "' in line " + currentLine, true);
        }


        private void checkPathValidity(string subFolder, string path)
        {
            checkPathValidity(subFolder, teaseAIFolderTextBox.Text, path);
        }

        private void checkPathValidity(string subFolder, string rootPath, string path)
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

            writeLineToLog("Checking path '" + rootPath + path + "' for validity.", true);

            //Check for not random but set path
            if (!path.Contains("*"))
            {
                //Check for set path
                if (!File.Exists(rootPath + path))
                    {
                        writeLineToLog("Invalid path to file '" + rootPath + path + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                        fileProducedErrors = true;
                }
            }
            else
            {
                string pathToFolder = path.Substring(0, path.LastIndexOf("\\"));
                if (!Directory.Exists(rootPath + pathToFolder))
                {
                    writeLineToLog("Invalid path to folder '" + rootPath + pathToFolder + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                    fileProducedErrors = true;
                } else
                {
                    string[] filesNames = Directory.GetFiles(rootPath + pathToFolder, "*", SearchOption.AllDirectories);

                    //File doesn't matter there just needs to be one file
                    if (path.EndsWith("*.*"))
                    {
                        if(filesNames.Length == 0)
                        {
                            writeLineToLog("Empty folder for *.* file pattern. Path '" + rootPath + pathToFolder + "' in line " + currentLine + ". Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                            return;
                        }
                    }

                    //Looking for a file with whatever extension but a specific name
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

                        //Did not find a fitting file
                        if (!foundFileFitting)
                        {
                            writeLineToLog("Folder did not contain any matching file with name '" + searchedFileName +  "' in line " + currentLine + ". Path '" + rootPath + pathToFolder + "'. Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                        }
                    }
                    //Looking for any file with a specific extension
                    else if(path.Contains("*."))
                    {
                        string searchedFileExtension = path.Substring(path.LastIndexOf("*.") + 2);

                        bool foundFileFitting = false;
                        foreach (string subFilePath in filesNames)
                        {
                            String fileName = Path.GetFileName(subFilePath);

                            //Do capital letters matter in file extensions?
                            if (fileName.ToLower().EndsWith("." + searchedFileExtension))
                            {
                                foundFileFitting = true;
                                break;
                            }
                        }

                        //Did not find a fitting file
                        if (!foundFileFitting)
                        {
                            writeLineToLog("Folder did not contain any matching file with extension '" + searchedFileExtension + "' in line " + currentLine + ". Path '" + rootPath + pathToFolder + "'. Dynamic given path was '" + path + "'.", false);
                            fileProducedErrors = true;
                        }
                    }
                }
            }
        }

        private ArrayList getTextWithinRecognition(string line, string begin, string end, bool allowEnumeration)
        {
            ArrayList textPieces = new ArrayList();
            if (line.Contains(begin) && !isStringInComment(line, begin))
            {
                string startString = begin;
                int startIndex = line.IndexOf(startString);
                string remainingString = line.Substring(startIndex + startString.Length);
                int endIndex = remainingString.IndexOf(end);

                if(endIndex < 0)
                {
                    writeLineToLog("Expected '" + end + "' in string '" + remainingString + "' to recognize the command. Failed to in string '" + line + "' in line " + currentLine + ".", false);
                    fileProducedErrors = true;
                    return textPieces;
                }

                string flagNames = remainingString.Substring(0, endIndex);

                //Check for multiple flagNames
                if (flagNames.Contains(","))
                {
                    if (allowEnumeration)
                    {
                        foreach (string substring in flagNames.Split(','))
                        {
                            textPieces.Add(stripUselssWhitespace(substring));
                        }
                    }
                    else
                    {
                        writeLineToLog("Invalid enumaration '" + flagNames + "' in string '" + line + "' in line " + currentLine + ". Enumeration is not allowed for '" + begin + "'.", false);
                    }
                }
                else
                {
                    textPieces.Add(stripUselssWhitespace(flagNames));
                }
            }

            return textPieces;
        }

        private void checkLineForFlagUsage(string line, string flagRecognition, bool allowEnumeration)
        {
            int index = 0;
            foreach(string flagName in getTextWithinRecognition(line, flagRecognition, ")", allowEnumeration))
            {
                //Second argument is the goto line
                if(flagRecognition.StartsWith("@CheckFlag(") && index == 1)
                {
                    registerPointerPoint(flagName);
                    return;
                }

                registerFlagInteraction(flagName);
                index++;
            }
        }

        //Not that effective
        private int getAmountOfBracketsWithoutSmiley(string line, char bracket)
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

        private bool lineContainsComment(string line)
        {
            return line.Contains("//") || line.Contains("\\\\") || line.Contains("@Info(");
        }

        private bool isStringInComment(string line, string sequence)
        {
            if(lineContainsComment(line) && line.Contains(sequence))
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

        private void writeLineToLog(String line, bool debug)
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
            if (!isValidPath(teaseAIFolderTextBox.Text))
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
            if (isValidPath(textBox1.Text))
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
            if(!ignoreTeaseAIPathChange && isValidPath(teaseAIFolderTextBox.Text))
            {
                if(Directory.Exists(teaseAIFolderTextBox.Text + "\\Scripts"))
                {
                    textBox1.Text = teaseAIFolderTextBox.Text + "\\Scripts";
                }
            }
        }
    }
}
