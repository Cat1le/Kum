namespace Kum;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.Write("> ");
            var code = Console.ReadLine();
            try
            {
                foreach (var token in new Parser().Parse(code))
                {
                    Console.WriteLine(token);
                }
            } catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
        }
    }

    static string GetFile(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        if (idx == -1 || idx == args.Length - 1)
        {
            throw new Exception($"Param '{name}' not present");
        }
        var file = args[idx + 1];
        if (!File.Exists(file))
        {
            throw new Exception($"File '{file}' does not exist");
        }
        return file;
    }
}