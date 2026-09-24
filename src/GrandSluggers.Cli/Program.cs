using GrandSluggers.Cli;
using GrandSluggers.Sim;

return CommandRunner.Run(args);

namespace GrandSluggers.Cli
{
    static class CommandRunner
    {
        /// <summary>
        /// Exit codes: 0 ran, 1 the command ran and failed (a validator, a lesson, a data root it cannot have),
        /// 2 the command line was refused (an unknown command, flag or value, or an id the catalog does not have).
        /// </summary>
        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
            {
                Console.Write(Commands.Help());
                return 0;
            }
            if (Commands.Find(args[0]) is not { } command)
            {
                Console.Error.WriteLine($"grand-sluggers: unknown command '{args[0]}'");
                Console.Error.Write(Commands.Help());
                return 2;
            }

            CommandLine line;
            try
            {
                line = CommandLine.Parse(args[1..], command.Options, command.MaxPositionals);
            }
            catch (UsageException e)
            {
                return Refuse(command, e.Message);
            }

            ContentCatalog content;
            try
            {
                content = ContentCatalog.Load();
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException)
            {
                // A data root or trial overlay this run named and cannot have. The message names the offending
                // path, so report it as a failure rather than a crash — and non-zero, so nothing reads on.
                Console.Error.WriteLine("grand-sluggers: " + ex.Message);
                return 1;
            }

            // Provenance, on stderr so stdout stays exactly the game's own output and a control run of any
            // command is still byte-comparable. A trace whose data root has to be reconstructed from memory
            // is not evidence, so every run says which root and which trial overlay produced it (#716).
            Console.Error.WriteLine(content.Root.Provenance);

            try
            {
                return command.Run(content, line);
            }
            catch (UsageException e)
            {
                return Refuse(command, e.Message);
            }
            catch (KeyNotFoundException e)
            {
                Console.Error.WriteLine($"{command.Name}: {e.Message}");
                return 2;
            }
        }

        static int Refuse(Command command, string message)
        {
            Console.Error.WriteLine($"{command.Name}: {message}");
            foreach (var usage in command.Usage) Console.Error.WriteLine("  use: " + usage);
            return 2;
        }
    }
}
