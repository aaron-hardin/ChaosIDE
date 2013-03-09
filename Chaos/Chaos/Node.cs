using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chaos
{
    class Node
    {
        public Node child { get; set; }
        public Node right { get; set; }
        public String parent { get; set; }
        public String value { get; set; }
        public String nodeType { get; set; }
        public int depth { get; set; }
        public Node parentNode { get; set; }
    }
}
