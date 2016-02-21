using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static BReusable.Files;
using static BReusable.Files.FindRefExtensions;

namespace MinecraftManager
{
    public partial class LogDisplay : Form
    {
        FileRef _fileRef;
        readonly IEnumerable<string> _keywords;
        LogDisplay()
        {
            InitializeComponent();
        }

        public LogDisplay(FileRef pathName, params string[] keywords)
            : this()
        {
            _fileRef = pathName;
            _keywords = keywords;

        }

        string ReadFile(string path)
        {
            StringBuilder sb = new StringBuilder();
            byte[] b = new byte[1024];
            using (var f = File.Open(_fileRef.GetPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int readLength;
                do
                {
                    readLength = f.Read(b, 0, b.Length);
                    var value = System.Text.Encoding.Default.GetString(b.Take(readLength).ToArray());
                    sb.Append(value);

                } while (readLength > 0);

            }
            return sb.ToString();
        }

        public void ReadFile()
        {

            var text = ReadFile(_fileRef.GetPath());
            var originalLength = text.Length;
            var wasWrapped = richTextBox1.WordWrap;
            richTextBox1.WordWrap = false;
            richTextBox1.Text = text;

            richTextBox1.SuspendLayout();

            // richTextBox1.HideSelection = true;
            richTextBox1.Enabled = false;
            richTextBox1.Cursor = Cursors.WaitCursor;

            for (int i = 0; i < richTextBox1.Lines.Length; i++)
            {
                var charIndex = richTextBox1.GetFirstCharIndexFromLine(i);
                if (charIndex < richTextBox1.Text.Length)
                    Debug.Assert(richTextBox1.Text.Substring(charIndex, 1)[0] == richTextBox1.Lines[i][0]);

                foreach (var item in FindItemsToColor(richTextBox1.Lines[i]))
                {

                    richTextBox1.SelectionStart = charIndex + item.Item1;
                    var itemText = item.Item2;
                    richTextBox1.SelectionLength = itemText.Length;
                    Debug.Assert(string.Compare(richTextBox1.SelectedText, itemText, true) == 0);

                    richTextBox1.SelectionColor = item.Item3;
                }
                System.Threading.Thread.Sleep(1);
                Application.DoEvents();
            }

            richTextBox1.ResumeLayout();
            richTextBox1.HideSelection = false;
            richTextBox1.Enabled = true;
            richTextBox1.WordWrap = wasWrapped;
            richTextBox1.Cursor = null;
        }

        public IEnumerable<Tuple<int, string, Color>> FindItemsToColor(string line)
        {
            var serverLineRegex = new Regex("^20[0-1][0-9]-[0-1][0-9]-[0-3][0-9] [0-2][0-9]:[0-5][0-9]:[0-6][0-9] ", RegexOptions.Compiled);
            var indexInCurrentLine = 0;
            if (serverLineRegex.IsMatch(line))
            {
#if DEBUG
                Debug.WriteLine("target is " + line);
#endif

                //date/time
                var timing = serverLineRegex.Match(line);

                Debug.Assert(line.Contains("\r") == false && line.Contains("\n") == false);
                yield return Tuple.Create(timing.Index, timing.Value, Color.DimGray);
                indexInCurrentLine = indexInCurrentLine + timing.Length;

                var type = Regex.Match(line.Substring(indexInCurrentLine), "\\[[a-z]+\\]", RegexOptions.IgnoreCase);
                Debug.Assert(type.Value.HasValue());
                Debug.Assert(type.Value.StartsWith("["));


                switch (type.Value)
                {
                    case "[SEVERE]":

                        yield return Tuple.Create(indexInCurrentLine, type.Value, Color.DarkRed);

                        break;
                    case "[WARNING]":
                        yield return Tuple.Create(indexInCurrentLine, type.Value, Color.DarkGoldenrod);
                        break;
                    case "[INFO]":

                        yield return Tuple.Create(indexInCurrentLine, type.Value, Color.Gray);
                        break;
                    default:
                        //richTextBox1.SelectionColor=Color.Black;
                        break;
                }

                indexInCurrentLine += type.Length;
                var key = GetKeyWord(line.Substring(indexInCurrentLine));
                if (key != null)
                    yield return Tuple.Create(indexInCurrentLine + key.Item1, key.Item2, Color.Blue);
            }
            else // assume sub text of previous message
            {
                //richTextBox1.ForeColor = Color.LightPink;
                // var color = oddPink ? Color.LightPink : Color.DeepPink;
                yield return Tuple.Create(0, line, Color.LightPink);
                //oddPink = !oddPink;
            }
        }

        Tuple<int, string> GetKeyWord(string remainder)
        {
            var words = Regex.Matches(remainder, "\\s+\\[?([a-z\\-]*)\\]?\\s+", RegexOptions.IgnoreCase);
            var keyword = from w in words.Cast<Match>()
                          join k in _keywords
                          on w.Groups[1].Value.ToUpper() equals k.ToUpper()
                          select new { w, k };

            var toHighlight = keyword.FirstOrDefault();
            if (toHighlight == null)
                return null;
            return Tuple.Create(toHighlight.w.Groups[1].Index, toHighlight.k);
#warning Not implemented

        }

        void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            ReadFile();
        }

        void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.FileName = _fileRef.GetPath();
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileRef = FileRefModule.tryMakeFile(ofd.FileName);
                    _fileRef = fileRef.Value;
                    ReadFile();
                }
            }
        }
        void LogDisplay_Load(object sender, EventArgs e)
        {
            ReadFile();
        }
    }
}
