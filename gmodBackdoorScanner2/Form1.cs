using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Timers;

namespace gmodBackdoorScanner2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            rtx_badwords.Text =
            "STEAM_" + "\n" +
            "http.Post" + "\n" +
            "http.Fetch" + "\n" +
            "CompileString" + "\n" +
            "RunString" + "\n" +
            "RunConsoleCommand" + "\n" +
            "file.Delete" + "\n" +
            "net.Receive" + "\n" +
            "char(" + "\n" +
            "byte(" + "\n" +
            "bxor" + "\n" +
            "bit." + "\n" +
            "<rgx>0[xX][0-9a-fA-F]+" + "\n" +
            "<rgx>\\[0-9]+" + "\n" +
            "<rgx>\\[xX][0-9a-fA-F]";

            

            rtx_baddirs.Text =
            ".zip" + "\n" +
            ".gma" + "\n" +
            "wire-" + "\n" +
            "3d2d_textscreens_" + "\n" +
            "ulx" + "\n" +
            "ulib" + "\n" +
            "dlib";
        }

        private void button1_Click(object sender, EventArgs e)
        {

            // make lists of bad words and bad directories

            var badwordsarray = rtx_badwords.Lines;
            List<string> badwords = new List<string>(badwordsarray);

            var baddirsarray = rtx_baddirs.Lines;
            List<string> baddirs = new List<string>(baddirsarray);


            // create a new thread and start a tree scan of the current directory, passing our lists to our function that's in our new thread

            Thread t1 = new Thread(() =>
            {
                TreeScan(Directory.GetCurrentDirectory(), badwords, baddirs);
            });

            t1.Start();
        }

        // make a function to add to the textbox, since we can't directly access our richTextBox UI element from the new thread
        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            richTextBox1.Text += value;
        }

        // scan the directory recursively

        void TreeScan(string sDir, List<string> badwords, List<string> baddirs) { 

            // make sure this directory isn't in the list

            bool directoryisblacklisted = false;

            foreach (string baddir in baddirs)
            {
                if (baddir != "" && sDir.Contains(baddir)) {
                    directoryisblacklisted = true;
                }
            }

            // if this isn't in the list then start scanning all the files in the directory

            if (!directoryisblacklisted)
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    
                    var current_file = File.ReadLines(f, Encoding.UTF8);

                    // the baddir list actually needs to be checked here as well, since compressed folders aren't recognized as directories, but as files ( makes sense I guess )
                    foreach (string baddir in baddirs)
                    {
                        if (baddir != "" && f.Contains(baddir))
                        {
                            directoryisblacklisted = true;
                        }
                    }

                    // and so we check yet again.  I would call this fileisblacklisted, but I can save a miniscule amount of memory not making a new variable for no reason.
                    if (!directoryisblacklisted)
                    {
                        // now scan each line in those files
                        foreach (string line in current_file)
                        {
                            // now take the line and compare it to each bad word
                            foreach (string badword in badwords)
                            {
                                // make sure we dont have any blanks
                                if (badword != "" && badword != "\n" && line != "" && line != "\n")
                                {
                                    string badword_fixed = badword.Replace("\n", "");
                                    // create a line for printing to the output
                                    string printline = line;
                                    //create a line for searching that doesn't have any pretty things only humans like, such as spaces and quotes
                                    string searchline = line;

                                    searchline = Regex.Replace(searchline, "string.lower((.*))", "$1");
                                    searchline = Regex.Replace(searchline, "string.upper((.*))", "$1");

                                    searchline = searchline.Replace(" ", "");
                                    searchline = searchline.Replace("..", "");
                                    searchline = searchline.Replace("\"", "");
                                    searchline = searchline.Replace("'", "");
                                    searchline = searchline.Replace("[[", "");
                                    searchline = searchline.Replace("]]", "");
                                    searchline = searchline.Replace("(", "");
                                    searchline = searchline.Replace(")", "");



                                    printline = searchline;

                                    // if the line is super long chop it off at 400 characters
                                    if (line.Length > 403)
                                    {
                                        printline = line.Substring(0, 400);

                                    }

                                    // if the search string starts with # then it's regex, so use regex.IsMatch, otherwise check it without regex with string.Contains

                                    if (badword_fixed.Contains("<rgx>"))
                                    {
                                        // pull the <rgx> out
                                        searchline = searchline.Replace("<rgx>", "");

                                        Regex regx = new Regex(badword_fixed);
                                        if (regx.IsMatch(searchline))
                                        {
                                            // if it was regex and the regex matched add this line to the output box
                                            AppendTextBox("\nFile: " + f + "\n Seems to use (obfuscated) Regex: " + badword_fixed + "\n" + "Line:" + printline + "\n");
                                        }
                                    }
                                    else if (searchline.Contains(badword))
                                    {
                                        // if it wasn't a regex, print it normally
                                        AppendTextBox("\nFile: " + f + "\n Seems to use: " + badword_fixed + "\n" + "Line: " + printline + "\n");
                                    }
                                }
                            }
                        }
                    }
                }

                // use this same function on the directories that are in this directory, creating our recursive loop
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    TreeScan(d, badwords, baddirs);
                }
            }
            else
            {
                AppendTextBox("\n\nSkipping Directory: " + sDir + "\n\n");
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
