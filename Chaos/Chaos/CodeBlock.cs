using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Chaos
{
    class CodeBlock : RichTextBox
    {
        public Node node { get; set; }
        public String parent { get; set; }

    }
}
