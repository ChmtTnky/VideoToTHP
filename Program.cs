namespace VideoToTHP
{
    public static class Program
    {
        public static void Main(params string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Error: Invalid arguments\n" +
                    "Example Args: VideoToTHP.exe <Path to Video> <optional: Path to Output>\n" +
                    "Default Output Name: DOKAPON.THP\n");
            }
            else if (args.Length <= 2)
            {
                Console.WriteLine(
                    "Converting Video...\n" +
                    "Note: This process may take some time.\n");
                if (args.Length == 1)
                    Converter.Convert(args[0]);
                else
                    Converter.Convert(args[0], args[1]);
            }
        }
    }
}