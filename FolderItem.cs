using System;

namespace FaceitDemoManager
{
    public class FolderItem
    {
        public string DisplayName { get; set; }
        public string RelativePath { get; set; }
        public int Depth { get; set; }

        public override string ToString()
        {
            return RelativePath;
        }
    }
}
