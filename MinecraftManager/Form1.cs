using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.FSharp.Core;

using MinecraftManager.Lib;

namespace MinecraftManager
{
    public partial class Form1 : Form
    {
        bool worldsDoLazy;
        bool yamlsDoLazy;
        LogDisplay _logDisplay;
        Process mcEdit;
        Process bukkit;
        Process nbtExplorer;

        public Form1()
        {
            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;
            InitializeComponent();
            this.Text = this.Text + " " + System.Reflection.Assembly.GetAssembly(typeof(Form1)).GetName().Version.ToString();
        }

        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Expression<Func<string>> compilerSafetyFunc = () => Properties.Settings.Default.MinecraftServerPath;
            var settingName = ((MemberExpression)compilerSafetyFunc.Body).Member.Name;
            if (e.PropertyName == settingName)
                OnServerPathFoundOrChanged();
        }

        long GetLogSize() =>
            Logs.getServerLogSize(Properties.Settings.Default.MinecraftServerPath) ?? 0;

        void RunOrSetupProcess(string title, Func<string> pathGetter, ref Process process, Action<string> pathSetter)
        {
            Application.DoEvents(); //let menus hide themselves if a click invoked this
            var path = pathGetter();
            if (File.Exists(path) == false)
            {
                this.SetFilePath(title, pathSetter);
                path = pathGetter();
            }
            if (File.Exists(path) == false)
                return;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            if (process == null || process.HasExited)
                process = Process.Start(path);
        }

        TreeNode FindServerNode() => treeView1.Nodes.Find("server", false).First();

        IEnumerable<string> FindWorlds(string serverPath)
        {
            foreach (var dir in Directory.GetDirectories(serverPath))
                if (Directory.GetFiles(dir, "level.dat").Any())
                    yield return dir;
        }

        void SetupWorldsUI()
        {
            var serverPath = FindServerPath();
            if (serverPath.IsNullOrEmpty() || !Directory.Exists(serverPath))
                return;
            var worlds = FindWorlds(Properties.Settings.Default.MinecraftServerPath);
            var serverNode = FindServerNode();
            serverNode.ToolTipText = "Server dir: " + Properties.Settings.Default.MinecraftServerPath;
            foreach (var worldPath in worlds)
            {
                var closurePath = worldPath;
                var worldName = Path.GetFileName(worldPath);
                var worldsNode = serverNode.Nodes.Find("worlds", false).FirstOrDefault();
                if (worldsNode == null)
                {
                    return;
                }
                var worldNode = worldsNode.Nodes.Find(worldName, false).FirstOrDefault();
                if (worldNode == null)
                {
                    worldNode = new TreeNode(worldName) { Name = worldName };
                    worldNode.Tag = worldPath;
                    worldNode.ToolTipText = worldPath;
                    if (worldsDoLazy)
                        worldNode.Nodes.Add(string.Empty); //Place holder for lazy expansion
                    doubleClickActions.Add(worldNode, () => Process.Start(closurePath));
                    worldsNode.Nodes.Add(worldNode);
                }
            }
        }

        void SetupTextUI()
        {
            var textNode = FindServerNode().TryFindNode("text");

            foreach (var t in Directory.GetFiles(Properties.Settings.Default.MinecraftServerPath, "*.txt"))
            {
                var closure = t;
                var textName = Path.GetFileNameWithoutExtension(t);
                var newNode = new TreeNode(textName) { Name = textName, Tag = t, ToolTipText = t };
                textNode.Nodes.Add(newNode);
                doubleClickActions.Add(newNode, () => Process.Start(closure));
            }
        }

        void SetupPluginsUI()
        {
            if (Directory.Exists(Properties.Settings.Default.MinecraftServerPath) == false)
                return;
            var pluginFolder = Path.Combine(Properties.Settings.Default.MinecraftServerPath, "plugins");
            if (Directory.Exists(pluginFolder) == false)
                return;

            var plugins = Directory.GetFiles(pluginFolder, "*.jar");
            var pluginNode = FindServerNode().TryFindNode("plugins");
            foreach (var p in plugins)
            {
                var pluginName = Path.GetFileNameWithoutExtension(p);
                var newNode = new TreeNode(pluginName) { Name = pluginName, Tag = p, ToolTipText = p };
                pluginNode.Nodes.Add(newNode);
                doubleClickActions.Add(newNode, () => Process.Start(Path.GetDirectoryName(p)));
            }
        }

        void SetupLogFileUI()
        {
            try
            {
                OnServerLogChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read logfile size", ex.Message);
            }
        }

        void OnServerPathFoundOrChanged()
        {
            SetupLogFileUI();
            SetupWorldsUI();
            SetupYamlUI();
            SetupBukkitMenus();
            FindServerPath();
        }


        void OnServerLogChanged()
        {
            var logSize = GetLogSize();
            var mbSize = Math.Round(((decimal)logSize / 1024 / 1024), 2).ToString() + "Mb";
            archiveLogToolStripMenuItem.ToolTipText = mbSize;

            var serverNode = FindServerNode();
            var log = serverNode.TryFindNode("logsize");
            if (log == null)
            {
                log = new TreeNode("logsize:" + archiveLogToolStripMenuItem.ToolTipText) { Name = "logsize" };

                serverNode.Nodes.Add(log);
            }
            log.Text = "logsize:" + archiveLogToolStripMenuItem.ToolTipText;
            if (logSize > 1024 * 1024 * 2)
            {
                this.archiveLogToolStripMenuItem.BackColor = Color.Red;

            }
            else if (logSize > 1024 * 1024 * 1)
            {
                this.archiveLogToolStripMenuItem.BackColor = Color.Yellow;

            }
            else this.archiveLogToolStripMenuItem.BackColor = Color.White;

        }

        void Form1_Load(object sender, EventArgs e)
        {

            toolStripStatusLabel1.Visible = false;


            var serverPath = FindServerPath(false);

            if (serverPath.HasValue() && Directory.Exists(serverPath))
            {
                OnServerPathFoundOrChanged();
            }

            SetupLinksMenus();
            SetupTextUI();
            SetupSettingsUI();
            SetupPluginsUI();
        }

        void SetupSettingsUI()
        {
            var settingProps =
                Properties.Settings.Default.GetType().GetProperties().Where(
                    p => p.PropertyType.Name == typeof(string).Name && p.Name != "SettingsKey");

            foreach (var setting in settingProps)
            {
                var closure = setting;
                var newItem = settingsToolStripMenuItem.DropDownItems.Add(setting.Name);
                var accessors = setting.GetAccessors();
                var value = accessors.First().Invoke(Properties.Settings.Default, null).ToString();
                if (value.HasValue() && value.Length > 2 && value[1] == ':')
                {
                    if (closure.Name.EndsWith("path", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (Directory.Exists(value) == false)
                            newItem.BackColor = Color.Red;
                    }
                    else if (File.Exists(value) == false)
                        newItem.BackColor = Color.Red;
                }

                newItem.ToolTipText = value;
                if (closure.CanWrite)
                    newItem.Click += (sender, e) =>
                                         {
                                             if (closure.Name.EndsWith("path", StringComparison.CurrentCultureIgnoreCase))
                                             {
                                                 this.SetFolderPath("Locate the " + closure.Name, p =>
                                                     {
                                                         closure.SetValue(Properties.Settings.Default, p, null);
                                                         Properties.Settings.Default.Save();
                                                         newItem.BackColor = Color.White;
                                                     });
                                             }
                                             else
                                             {
                                                 this.SetFilePath("Locate " + setting.Name, p =>
                                                      {
                                                          closure.SetValue(Properties.Settings.Default, p, null);
                                                          Properties.Settings.Default.Save();
                                                          newItem.BackColor = Color.White;
                                                      });
                                             }

                                         };

            }
        }


        void SetupYamlUI()
        {
            var serverPath = Properties.Settings.Default.MinecraftServerPath;
            var yamlNode = FindServerNode().Nodes.Find("yml", false).First();
            foreach (var yaml in Directory.GetFiles(serverPath, "*.yml"))
            {
                var closureYml = yaml;
                var yamlName = Path.GetFileNameWithoutExtension(yaml);
                if (yamlNode.Nodes.Find(yamlName, false).Any())
                    continue;
                var newYaml = new TreeNode(yamlName);
                if (yamlsDoLazy)//add a child for lazy expansion
                    newYaml.Nodes.Add(string.Empty);
                doubleClickActions.Add(newYaml, () => Process.Start(closureYml));
                yamlNode.Nodes.Add(newYaml);
            }

        }

        IDictionary<TreeNode, Action> doubleClickActions = new Dictionary<TreeNode, Action>();

        void SetupLinksMenus()
        {
            foreach (var item in Properties.Settings.Default.Links)
            {
                var title = item.SubstringBefore(",");
                var link = item.SubstringAfter(",");
                var newMenu = linksToolStripMenuItem.DropDownItems.Add(title);
                newMenu.ToolTipText = link;
                newMenu.Click += delegate { Process.Start(link); };

            }
        }
        string FindServerPath(bool promptUser = true)
        {
            var path = Properties.Settings.Default.MinecraftServerPath;
            if (Directory.Exists(path))
                return path;
            if (promptUser)
                this.SetFolderPath("Please locate the minecraft server folder", p =>
                {
                    var closure = p;
                    Properties.Settings.Default.MinecraftServerPath = closure;
                    Properties.Settings.Default.Save();
                }
                       );
            return Properties.Settings.Default.MinecraftServerPath;
        }

        bool GetIsNone<T>(FSharpOption<T> opt) => FSharpOption<T>.get_IsNone(opt);

        FindFileResult FindServerLogOpt()
        {
            var serverPath = FindServerPath();
            if (serverPath.IsNullOrEmpty() || Directory.Exists(serverPath) == false)
                return null;
            return Logs.findServerLogOpt(serverPath);
        }

        void ArchiveLogFile(FileRef fr)
        {
            var logPath = fr.GetPath();
            var archiveName = Path.Combine(Path.GetDirectoryName(logPath), "server" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
            if (File.Exists(archiveName))
            {
                MessageBox.Show("today's backup already exists");
                return;
            }

            var creation = File.GetCreationTimeUtc(logPath);
            var age = DateTime.UtcNow - creation;
            if (age.TotalHours < 8)
            {
                MessageBox.Show("backfile is only " + (age).TotalHours.ToString("0.0") +
                                " hours old");
                return;
            }

            try
            {
                File.Move(logPath, archiveName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to archive log, the file may be in use by the server", ex.Message);
            }
        }

        void archiveLogToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var logPath = FindServerLogOpt();
            logPath.WithFoundValue(ArchiveLogFile, searchedPaths =>
            {
                toolStripStatusLabel1.Text = "Failed to find server log";
                toolStripStatusLabel1.Visible = true;
                MessageBox.Show("Could not find server log at any of " + Environment.NewLine + (string.Join(Environment.NewLine + "\t ", searchedPaths)));
                var t = new Task(() => { System.Threading.Thread.Sleep(3000); toolStripStatusLabel1.Visible = false; });
                t.Start();
            });
        }

        void mCEditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(Properties.Settings.Default.NbtExplorer) == false)
                mCEditToolStripMenuItem.BackColor = Color.Red;
            else
                mCEditToolStripMenuItem.BackColor = Color.White;

            RunOrSetupProcess("McEdit", () => Properties.Settings.Default.MCEdit, ref mcEdit, s => Properties.Settings.Default.NbtExplorer = s);
        }

        void nbtExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOrSetupProcess("NbtExplorer", () => Properties.Settings.Default.NbtExplorer, ref nbtExplorer,
                              s => Properties.Settings.Default.NbtExplorer = s);
        }

        void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (doubleClickActions.ContainsKey(e.Node))
                doubleClickActions[e.Node]();
        }

        void exitToolStripMenuItem_Click(object sender, EventArgs e) =>
            this.Close();

        string FindJava()
        {
            var java = FSharpOption<string>.None;
            if (Properties.Settings.Default.Java.IsNullOrEmpty() == false)
                java = Lib.JavaExe.InPathBehavior.TryLocate(Lib.CrossCutting.Logging.log);
            if (GetIsNone(java))
            {
                java = Lib.JavaExe.PropertyBehavior.TryLocate(() => Properties.Settings.Default.Java, () =>
                {
                    this.SetFilePath("Locate java.exe", p =>
                    {
                        Properties.Settings.Default.Java = p;
                        Properties.Settings.Default.Save();

                    });
                    return Properties.Settings.Default.Java;
                });
                if (GetIsNone(java))
                    return null;
            }
            return java.Value;
        }

        void RunMinecraftAs(string java, string minecraftBinPath, string alias = null)
        {
            if (alias.IsNullOrEmpty())
                alias = InputForm.ShowDialog("Minecraft Alias", "Minecraft as whom?");
            if (alias.IsNullOrEmpty())
                return;
            MineCraftLaunching.MinecraftAs(minecraftBinPath, java, clientMemoryArguments, alias, s => MessageBox.Show(s));
        }

        const string clientMemoryArguments = "-Xms512m -Xmx1024m";
        const string serverMemoryArguments = "-Xincgc -Xmx1024M";

        void SetupBukkitMenus()
        {
            var serverPath = FindServerPath();
            if (serverPath.IsNullOrEmpty() || Directory.Exists(serverPath) == false)
                return;

            var bukkits = Directory.GetFiles(serverPath, "*.jar").Where(p => p.Contains("bukkit"));
            var bukkitInfo = from b in bukkits
                             let info = new FileInfo(b)
                             orderby info.LastWriteTimeUtc descending
                             select new { b, info };
            foreach (var bukkit in bukkitInfo)
            {
                var newMenu = craftBukkitToolStripMenuItem.DropDownItems.Add(bukkit.info.Name);
                newMenu.ToolTipText = bukkit.info.LastWriteTime.ToShortDateString();
                newMenu.Click += (sender, e) =>
                    {
                        var newBukkit = LaunchBukkit(bukkit.info);
                        if (newBukkit != null)
                            this.bukkit = newBukkit;
                    };
            }
        }

        Process LaunchBukkit(FileInfo bukkitInfo)
        {
            var java = FindJava();
            if (java.IsNullOrEmpty())
                return null;

            var startInfo = new ProcessStartInfo(java, serverMemoryArguments + " -jar \"" + bukkitInfo.Name + "\"")
            {
                WorkingDirectory = bukkitInfo.DirectoryName,

                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = true,
                //CreateNoWindow=true,
                ErrorDialog = true,
                ErrorDialogParentHandle = this.Handle,

            };
            Debug.WriteLine("Arguments:" + startInfo.Arguments);
            Debug.WriteLine("WorkingDirectory:" + startInfo.WorkingDirectory);

            var bukkit = new Process() { StartInfo = startInfo };


            bukkit.Start();
            //Natives.AttachToProcess(bukkit);
            System.Threading.Thread.Sleep(1000);
            if (bukkit.HasExited == false)
            {
                craftBukkitToolStripMenuItem.Enabled = false;
                bukkit.Exited += (senderE, eE) => { craftBukkitToolStripMenuItem.Enabled = true; };

            }
            return bukkit;
        }

        void craftBukkitToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var serverPath = FindServerPath();
            if (serverPath.IsNullOrEmpty() || Directory.Exists(serverPath) == false)
                return;

            var bukkits = System.IO.Directory.GetFiles(serverPath, "*.jar").Where(p => p.Contains("bukkit"));
            var bukkitInfo = from b in bukkits
                             let info = new System.IO.FileInfo(b)
                             orderby info.LastWriteTimeUtc descending
                             select new { b, info };
            var latestBukkit = bukkitInfo.FirstOrDefault();
            LaunchBukkit(latestBukkit.info);
        }

        string FindMinecraftBinPath()
        {
            var minecraftBinPath = Properties.Settings.Default.MinecraftBinPath;
            if (!Directory.Exists(minecraftBinPath))
            {
                var appData = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var title = "Minecraft Bin, usually at %AppData%\\.minecraft\\bin \"" + appData + "\"";
                string startPath = null;
                if (Directory.Exists(appData))
                {
                    startPath = appData;
                    var minecraftAppDataPath = Path.Combine(appData, ".minecraft");
                    if (File.Exists(minecraftAppDataPath))
                        startPath = minecraftAppDataPath;
                }

                this.SetFolderPath(title, p =>
                {
                    Properties.Settings.Default.MinecraftBinPath = p;
                    Properties.Settings.Default.Save();
                }, startPath);
            }
            return minecraftBinPath;
        }

        void minecraftAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string java = FindJava();
            if (java.IsNullOrEmpty())
                return;
            string minecraftExe = FindMinecraftBinPath();
            if (!Directory.Exists(Properties.Settings.Default.MinecraftBinPath))
                return;
            RunMinecraftAs(java, Properties.Settings.Default.MinecraftBinPath);
        }

        void minecraftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process mc = null;
            RunOrSetupProcess("Minecraft", () => Properties.Settings.Default.Minecraft, ref mc,
                              s => Properties.Settings.Default.Minecraft = s);
        }

        void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Application.ProductVersion);
        }

        IEnumerable<TreeNode> FindPlugins()
        {
            var server = FindServerNode();
            if (server == null)
                return Enumerable.Empty<TreeNode>();
            var pluginsNode = server.TryFindNode("plugins");
            if (pluginsNode == null)
                return Enumerable.Empty<TreeNode>();
            return pluginsNode.Nodes.Cast<TreeNode>();
        }

        void viewLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_logDisplay != null && _logDisplay.IsDisposed == false && _logDisplay.Disposing == false)
            {
                _logDisplay.Show(this);
                return;
            }
            var findResultOpt = FindServerLogOpt();
            FileRef serverLogFileRef = null;
            findResultOpt.WithFoundValue(fr => serverLogFileRef = fr, searchedPaths =>
            {
                toolStripStatusLabel1.Text = "Failed to find server log";
                toolStripStatusLabel1.Visible = true;
                MessageBox.Show("Could not find server log at any of " + Environment.NewLine + (string.Join(Environment.NewLine + "\t ", searchedPaths)));
                var t = new Task(() => { System.Threading.Thread.Sleep(3000); toolStripStatusLabel1.Visible = false; });
                t.Start();
            });
            if (serverLogFileRef == null)
                return;
            var plugins = FindPlugins();

            _logDisplay = new LogDisplay(serverLogFileRef, plugins.Select(p => p.Name).ToArray());

            _logDisplay.Show(this);
        }
    }
}
