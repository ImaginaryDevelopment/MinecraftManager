using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MinecraftManager
{
    public static class WinFormExtensions
    {
        public static TreeNode TryFindNode(this TreeNode tn, string key, bool searchAllChildren=false)
        {
            var findResults=tn.Nodes.Find(key, searchAllChildren);
            if (findResults.Length < 1)
                return null;
            return findResults[0];
        }

        public static void SetFolderPath(this IWin32Window owner, string description, Action<string> pathSetter, string startPath = null)
        {
            using (var ofd = new FolderBrowserDialog())
            {
                ofd.ShowNewFolderButton = false;
                if (startPath.IsNullOrEmpty() == false)
                    ofd.SelectedPath = startPath;
                ofd.Description = description;

                if (ofd.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                pathSetter(ofd.SelectedPath);
            }
        }

       public static void SetFilePath(this IWin32Window owner,string title, Action<string> pathSetter)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = title;
                if (ofd.ShowDialog(owner) != DialogResult.OK)
                    return;
                pathSetter(ofd.FileName);

            }
        }

    }
}
