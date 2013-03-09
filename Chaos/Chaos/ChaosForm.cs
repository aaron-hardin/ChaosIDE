using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.IO;

namespace Chaos
{
    public partial class ChaosForm : Form
    {
        bool isKproj = true;
        string rootDir = "";
        Node headNode = null;
        List<CodeBlock> codeBoxes = new List<CodeBlock>();

        public ChaosForm()
        {
            InitializeComponent();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String chosenFile = "";
            openFileDialog1.Title = "Select project to load";
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "kide|*.kide|kproj|*.kproj";

            if (openFileDialog1.ShowDialog() != DialogResult.Cancel)
            {
                chosenFile = openFileDialog1.FileName;
                loadProject(chosenFile);
            }
        }

        /// <summary>
        /// loads a project into memory of file type kide or kproj
        /// kide is a compressed project, it is a zip
        /// kproj is an expanded project, it is an xml
        /// </summary>
        /// <param name="chosenFile">the file to load</param>
        private void loadProject(string chosenFile)
        {
            //check to see if kide or kproj, 
            //kide is a compressed project, it is a zip
            //kproj is an expanded project, it is an xml
            string filename = chosenFile.Split('\\').Last();
            rootDir = chosenFile.Substring(0, chosenFile.LastIndexOf(filename)-1)+"\\";
            string fileExtension = filename.Split('.').Last();
            string fileWOextension = filename.Replace("."+fileExtension,"");
            if (fileExtension == "kide")
            {
                //if its a kide, unzip and load as kproj, set global variable so we know to save it that way
                isKproj = false;
                unzipKIDE(chosenFile,rootDir);

                headNode = readProjFile(rootDir + fileWOextension + "\\" + fileWOextension + ".kproj");

                populateComboBox(headNode);

                //TODO remove, testing
                //zipKIDE(rootDir + fileWOextension + "\\", rootDir, filename);

            }
            else if (fileExtension == "kproj")
            {
                //if its a kproj then just edit files in place and make sure to update kproj
                isKproj = true;
            }
            else
            {
                MessageBox.Show("unrecognized file type");
                //System.Environment.Exit(1);
            }

            

            
            //throw new NotImplementedException();
        }

        /// <summary>
        /// unzips a KIDE file to allow for edits to be made
        /// </summary>
        /// <param name="chosenFile">the filename to be unzipped</param>
        /// <param name="workingPath">path to place the KIDE contents</param>
        private void unzipKIDE(string chosenFile, string workingPath)
        {
            string targetName = "";
            string filename = chosenFile.Split('\\').Last(); //because chosen file has the path
            if (chosenFile.Split('.').Last().ToLower().Equals("kide"))
            {
                targetName = workingPath + filename.Substring(0, filename.LastIndexOf(filename.Split('.').Last()) - 1);
            }
            else
            {
                targetName = workingPath + filename;
            }
            string sourceName = chosenFile;

            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = "7zipCMD\\7za.exe";

            p.Arguments = "x -tzip \"" + sourceName + "\" -y -o\"" + targetName + "\" -mx=9";
            p.WindowStyle = ProcessWindowStyle.Hidden;

            Process x = Process.Start(p);
            x.WaitForExit();

            //return workingPath; // need \dirname
        }

        /// <summary>
        /// zips to a KIDE archive
        /// </summary>
        /// <param name="zipDir">path to place in the KIDE</param>
        /// <param name="filePath">path to place the KIDE</param>
        /// <param name="fileName">name of the KIDE</param>
        private void zipKIDE(string zipDir, string filePath, string fileName)
        {
            string sourceName = zipDir + "\\*";
            string targetName = filePath + fileName + ((fileName.Split('.').Last().ToLower().Equals("kide"))?"":".kide");//append .kide if needed

            // 1
            // Initialize process information.
            //
            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = "7zipCMD\\7za.exe";

            // 2
            // Use 7-zip
            // specify a=archive and -tzip=zip
            // and then target file in quotes followed by source file in quotes
            //
            p.Arguments = "a -tzip \"" + targetName + "\" \"" + sourceName + "\" -mx=9";
            p.WindowStyle = ProcessWindowStyle.Hidden;

            // 3.
            // Start process and wait for it to exit
            //
            Process x = Process.Start(p);
            x.WaitForExit();
        }

        /// <summary>
        /// reads in the project file, returns head node
        /// </summary>
        /// <param name="filename">file to read in, including path</param>
        /// <returns>head node of tree structure</returns>
        private Node readProjFile(String filename)
        {
            Node current = null;
            Node head = null;
            Node next = null;
            bool headInit = false;

            XmlTextReader reader = new XmlTextReader(filename);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        if (headInit == false)
                        {
                            head = new Node();
                            current = head;
                            head.depth = reader.Depth;
                            headInit = true;
                        }
                        else
                        {
                            if (reader.Depth > current.depth) //we just moved in a layer
                            {
                                Node temp = new Node();
                                temp.parentNode = current;
                                current.child = temp;
                                current = temp;
                            }
                            else if (reader.Depth == current.depth)//same layer
                            {
                                Node temp = new Node();
                                temp.parentNode = current.parentNode;
                                current.right = temp;
                                current = temp;
                            }
                            else if (reader.Depth < current.depth) //we moved out one
                            {
                                Node temp = new Node();
                                Node parenty = current;
                                while (parenty.depth > reader.Depth)
                                {
                                    parenty = parenty.parentNode;
                                }
                                temp.parentNode = parenty.parentNode;
                                parenty.right = temp;
                                current = temp;
                            }
                        }
                        current.depth = reader.Depth;
                        current.nodeType = reader.Name;
                        // check to see if the current node has attributes
                        if (reader.HasAttributes)
                        {
                            // move to the first attribute
                            reader.MoveToFirstAttribute();
                            current.parent = reader.Value;
                        }
                        //Console.Write("<" + reader.Name);
                        //Console.WriteLine(">");
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        //Console.WriteLine(reader.Value);
                        current.value = reader.Value;
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        //Console.Write("</" + reader.Name);
                        //Console.WriteLine(">");
                        break;
                }
            }
            printTree(head);

            return head;
        }

        /// <summary>
        /// primarily a test function, prints tree for debugging
        /// </summary>
        /// <param name="head">head node to start from</param>
        private void printTree(Node head)
        {
            for (int i = 0; i < head.depth; ++i)
            {
                Console.Write(" ");
            }
            Console.WriteLine(head.nodeType + ((head.parent == null)?"":": "+head.parent));
            Console.WriteLine("value:"+head.value);
            //richTextBox1.Text += head.value+"\n";
            if (head.child != null)
            {
                printTree(head.child);
            }
            if (head.right != null)
            {
                printTree(head.right);
            }
        }

        /*
        /// <summary>
        /// gets all codeblocks and inserts them into text box
        /// </summary>
        /// <param name="head">where to start</param>
        /// <param name="head">do i color this</param>
        /// <returns>boolean, is colored</returns>
        private bool populateTextBoxWithCode(Node head, bool color = false)
        {
            bool isColored = false;
            bool counts = false;
            if (color)
            {
                richTextBox1.SelectionBackColor = Color.Yellow;
            }
            else
            {
                richTextBox1.SelectionBackColor = Color.White;
            }
            if (head.nodeType.Equals("documentation") || head.nodeType.Equals("declaration") || head.nodeType.Equals("code"))
            {
                if (head.nodeType.Equals("documentation"))
                {
                    richTextBox1.AppendText("\n\n");
                }
                richTextBox1.AppendText(head.value.TrimEnd());
                if (head.nodeType.Equals("declaration"))
                {
                    richTextBox1.AppendText("\n" + "{");
                }
                if (color)
                {
                    isColored = true;
                }
                counts = true;
            }
            if (head.child != null)
            {
                color = populateTextBoxWithCode(head.child, ((counts) ? !color : color));
                if (head.nodeType.Equals("functioncode"))
                {
                    richTextBox1.SelectionBackColor = Color.White;
                    richTextBox1.AppendText("\n" + "}");
                }
                //counts = true;
            }
            if (head.right != null)
            {
                color = populateTextBoxWithCode(head.right, ((counts) ? !color : color));
                //counts = true;
            }
            return ((counts) ? !color : color);
        }
        */

        /// <summary>
        /// gets all codeblocks and inserts them into text box
        /// </summary>
        /// <param name="head">where to start</param>
        private void populateTextBoxsWithCode(Node head)
        {
            if (head.nodeType.Equals("documentation") || head.nodeType.Equals("declaration") || head.nodeType.Equals("code"))
            {
                if (head.nodeType.Equals("documentation"))
                {
                    //richTextBox1.AppendText("\n\n");
                }
                //richTextBox1.AppendText(head.value.TrimEnd());
                CodeBlock rtb = new CodeBlock();
                rtb.node = head;
                rtb.parent = head.parent;
                if (rtb.parent != null)
                {
                    Console.WriteLine("breakpoint");
                }
                rtb.AppendText(head.value.TrimEnd());
                if (codeBoxes.Count == 0)
                {
                    rtb.Location = new Point(0, 0);
                }
                else
                {
                    Point prevLoc = codeBoxes.LastOrDefault().Location;
                    int prevY = codeBoxes.LastOrDefault().Size.Height;
                    rtb.Location = new Point(prevLoc.X,prevLoc.Y+prevY);
                }
                rtb.ScrollBars = RichTextBoxScrollBars.None;
                rtb.WordWrap = false;
                rtb.AcceptsTab = true;
                //rtb.Anchor = AnchorStyles.Left;
                rtb.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
                rtb.Width = panel1.Width;
                
                //Amount of padding to add
                const int padding = 3;
                //get number of lines (first line is 0)
                int numLines = rtb.GetLineFromCharIndex(rtb.TextLength) + 1;
                //get border thickness
                int border = rtb.Height - rtb.ClientSize.Height;
                //set height (height of one line * number of lines + spacing)
                rtb.Height = rtb.Font.Height * numLines + padding + border;

                panel1.Controls.Add(rtb);
                codeBoxes.Add(rtb);

                //panel1.AutoScrollMinSize = new Size(panel1.AutoScrollMinSize.Width, rtb.Location.Y + rtb.Height + padding);
                //panel1.AutoScrollMinSize = new Size(panel1.AutoScrollMinSize.Width, codeBoxes.LastOrDefault().Location.Y + codeBoxes.LastOrDefault().Height + padding);


                if (head.nodeType.Equals("declaration"))
                {
                    //richTextBox1.AppendText("\n" + "{");
                }
            }
            if (head.child != null)
            {
                populateTextBoxsWithCode(head.child);
                if (head.nodeType.Equals("functioncode"))
                {
                    //richTextBox1.AppendText("\n" + "}");
                }
                //counts = true;
            }
            if (head.right != null)
            {
                populateTextBoxsWithCode(head.right);
                //counts = true;
            }
        }
        
        /// <summary>
        /// finds the filename in the tree structure then calls populateTextBoxWithCode to fill the tb
        /// </summary>
        /// <param name="filename">file we want to read</param>
        /// <param name="node">where to start, since its recursive</param>
        /// <param name="parentIsFile">helps to find the node more efficiently</param>
        private void populateTB(String filename, Node node, bool parentIsFile = false)
        {
            if (node.nodeType.Equals("file"))
            {
                //richTextBox1.Text += node.value + "\n";
                parentIsFile = true;
            }
            else if (parentIsFile && node.nodeType.Equals("name") && node.value.Equals(filename))
            {
                //populateTextBoxWithCode(node.parentNode);
                populateTextBoxsWithCode(node.parentNode);
                return;
            }
            if (node.child != null)
            {
                populateTB(filename, node.child, parentIsFile);
            }
            if (node.right != null)
            {
                populateTB(filename, node.right, parentIsFile);
            }
        }

        /// <summary>
        /// adds the selectable code filenames to the combobox
        /// </summary>
        /// <param name="node">recursive function, node to start with</param>
        private void populateComboBox(Node node)
        {
            if (node.nodeType.Equals("file"))
            {
                //richTextBox1.Text += node.value + "\n";
                Node name = node.child;
                if (name.nodeType.Equals("name"))
                {
                    Node type = name.right;
                    if (type.nodeType.Equals("type") && type.value.Equals("codefile"))
                    {
                        comboBox1.Items.Add(name.value);
                    }
                }
            }
            else
            {
                if (node.child != null)
                {
                    populateComboBox(node.child);
                }
                if (node.right != null)
                {
                    populateComboBox(node.right);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //richTextBox1.Text = "";
            panel1.Controls.Clear();
            codeBoxes.Clear();
            populateTB(comboBox1.SelectedItem.ToString(), headNode);
            resizePanel();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            resizePanel();
        }

        /// <summary>
        /// resizes the panel based on what is in it
        /// </summary>
        private void resizePanel()
        {
            if (codeBoxes.Count > 0)
            {
                const int padding = 3;
                int maxWidth = 0;
                foreach (CodeBlock rtb in codeBoxes)
                {
                    Graphics g = Graphics.FromHwnd(rtb.Handle);
                    SizeF f = g.MeasureString(rtb.Text, rtb.Font);
                    int width = (int)Math.Ceiling(f.Width);
                    if (maxWidth < width)
                    {
                        maxWidth = width;
                    }
                }
                foreach (CodeBlock rtb in codeBoxes)
                {
                    rtb.Width = maxWidth;
                }
                panel1.Width = Math.Min((this.Width) / 2, maxWidth + 50);
                panel1.AutoScrollMinSize = new Size(maxWidth + padding, codeBoxes.LastOrDefault().Location.Y + codeBoxes.LastOrDefault().Height + padding);
            }
        }

        /// <summary>
        /// marks the code blocks for the given parent yellow
        /// </summary>
        /// <param name="parent">parents name</param>
        private void markChildren(String parent)
        {
            foreach (CodeBlock block in codeBoxes)
            {
                if (block.parent != null && block.parent.Equals(parent))
                {
                    block.BackColor = Color.Yellow;
                }
            }
        }

        /// <summary>
        /// saves the project to the given filename
        /// </summary>
        /// <param name="filename">project filename</param>
        private void saveProject(String filename)
        {
            foreach (CodeBlock cb in codeBoxes)
            {
                cb.node.value = cb.Text;
            }

            using (StreamWriter file = new StreamWriter(filename))
            {
                outputToFile(file, headNode);
            }
        }

        private void outputToFile(StreamWriter file, Node node)
        {
            bool printNewLine = true;
            if (node.child == null)// && (node.value==null || node.value.Split('\n').Count()<2))
            {
                printNewLine = false;
            }

            for (int i = 0; i < node.depth; ++i)
            {
                file.Write("\t");
            }
            if (node.parent == null || node.parent.Equals(""))
            {
                file.Write("<" + node.nodeType + ">");
                if (printNewLine)
                {
                    file.WriteLine();
                }
            }
            else
            {
                file.WriteLine("<" + node.nodeType + " parent="+node.parent+">");
            }

            if (node.value != null && node.value.Split('\n').Count() > 1)
            {
                String printy = node.value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
                file.WriteLine(printy);
            }
            else if (node.value != null && !node.value.Trim().Equals(""))
            {
                /*for (int i = 0; i <= node.depth; ++i)
                {
                    file.Write("\t");
                }*/
                String printy = node.value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
                file.Write(printy);
                
            }

            if (node.child != null)
            {
                outputToFile(file, node.child);
            }

            if (printNewLine || (node.value != null && node.value.Split('\n').Count() > 1))
            {
                for (int i = 0; i < node.depth; ++i)
                {
                    file.Write("\t");
                }
            }

            file.WriteLine("</" + node.nodeType + ">");


            if (node.right != null)
            {
                outputToFile(file, node.right);
            }
        }

        private void generateProject()
        {
            throw new NotImplementedException();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveProject(rootDir + "..\\eek.xml");
        }

        private void ChaosForm_Load(object sender, EventArgs e)
        {

        }
    }
}
