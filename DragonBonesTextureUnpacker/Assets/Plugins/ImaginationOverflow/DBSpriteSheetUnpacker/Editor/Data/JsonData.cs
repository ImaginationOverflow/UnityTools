using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImaginationOverflow.DBSpriteSheetUnpacker.Editor.Data
{

    [Serializable]
    public class SubTexture
    {
        public float width;
        public float height;
        public float y;
        public float x;
        public string name;

        public string SpriteName { get; set; }
    }

    [Serializable]
    public class TextureInfo
    {
        public string name;
        public string imagePath;
        public SubTexture[] SubTexture;

    }
}
