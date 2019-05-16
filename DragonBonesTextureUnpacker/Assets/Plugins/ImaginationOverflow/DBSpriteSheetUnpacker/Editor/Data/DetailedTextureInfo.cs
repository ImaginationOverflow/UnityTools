using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImaginationOverflow.DBSpriteSheetUnpacker.Editor.Data
{

    public class DBAnimation
    {
        private string _newName;

        public string Name { get; set; }

        public string NewName
        {
            get { return string.IsNullOrEmpty(_newName) ? Name : _newName; }
            set { _newName = value; }
        }

        public string DestinationPath { get; set; }

        public ICollection<SubTexture> Sprites { get; set; }

        public DBAnimation()
        {
            Sprites = new List<SubTexture>();
        }

        public override string ToString()
        {
            return Name + " " + Sprites.Count;
        }
    }
    public class DetailedTextureInfo
    {
        public string SpritePath { get; set; }
        public List<DBAnimation> Animations { get; set; }
        public TextureInfo Original { get; set; }
        public string FileName { get; set; }
    }
}
