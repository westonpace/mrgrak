using System;

namespace grakall
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Grak1.Program.Main(args);
            Grak2.Program.Main(args);
            Grak3.Program.Main(args);
            Grak4.Program.Main(args);
            Grak4b.Program.Main(args);
            LongThread.Program.Main(args);
            SeparateThread.Program.Main(args);
            Structs.Program.Main(args);
        }
    }
}
