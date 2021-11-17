using System.Reflection;

namespace PokerParty.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"PokerParty Server v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}");

            Console.WriteLine("Type \"stop\" to save and stop the server.op");
            Console.ReadKey();
        }
    }
}