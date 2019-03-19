using System;
using System.Collections.Generic;
using System.Threading;
using VRCModLoader;

namespace CCom
{
    internal class CommandManager
    {

        private static readonly Random counter = new Random();

        private static Dictionary<string, Type> commands = new Dictionary<string, Type>();
        private static Dictionary<string, Dictionary<string, Command>> runningCommands = new Dictionary<string, Dictionary<string, Command>>();

        public static void RunCommand(String line, Client client)
        {
            string[] parts = line.Split(new char[] { ' ' }, 3);
            if (runningCommands.TryGetValue(parts[0], out Dictionary<string, Command> commandContainer) && commandContainer.TryGetValue(parts[1], out Command command))
            {
                if (parts[2].StartsWith("ERROR")) command.RemoteError(parts[2].Split(new char[] { ' ' }, 2)[1]);
                else command.Handle(parts[2]);
            }
            else
            {
                if (commands.TryGetValue(parts[0], out Type commandClass))
                {
                    try
                    {
                        command = (Command)Activator.CreateInstance(commandClass);
                        command.SetClient(client);
                        command.SetOutId(parts[0] + " " + parts[1]);
                        commandContainer = runningCommands[parts[0]];
                        if (commandContainer == null)
                        {
                            commandContainer = new Dictionary<String, Command>();
                            runningCommands.Add(parts[0], commandContainer);
                        }
                        commandContainer.Add(parts[1], command);
                        Command commandHandled = command;
                        Thread commandThread = new Thread(() =>
                        {
                            if (commandHandled != null)
                            {
                                try
                                {
                                    commandHandled.Handle(parts[2]);
                                }
                                catch (Exception e)
                                {
                                    commandHandled.WriteLine("ERROR " + e.Message.ToUpper());
                                }
                            }
                        });
                        commandThread.Name = "COMMAND_" + parts[0] + "_" + parts[1];
                        commandThread.Start();

                    }
                    catch (Exception e)
                    {
                        VRCModLogger.LogError(e.ToString());
                    }
                }
                else
                {
                    client.WriteLine(parts[0] + " " + parts[1] + " ERROR COMMAND_NOT_FOUND");
                }
            }
        }

        public static void RegisterCommand(string name, Type command)
        {
            if (!commands.TryGetValue(name, out Type cmd)) commands.Add(name, command);
            else VRCModLogger.LogError("[CCOM] Trying to register a command twice (" + name + ")");
        }

        public static void Remove(Command command)
        {
            String[] parts = command.GetOutId().Split(new char[] { ' ' }, 2);
            if (runningCommands.TryGetValue(parts[0], out Dictionary<string, Command> commandContainer) && commandContainer.TryGetValue(parts[1], out Command value))
            {
                commandContainer.Remove(parts[1]);
            }
        }

        public static Command CreateInstance(String className, Client client, bool log = true)
        {
            if(log) VRCModLogger.Log("Creating command instance " + className + ". Client: " + client);
            if (commands.TryGetValue(className, out Type commandClass))
            {
                try
                {
                    Command command = (Command)Activator.CreateInstance(commandClass);
                    long outId;
                    lock (counter)
                    {
                        outId = (long)(counter.NextDouble() * long.MaxValue);
                    }
                    command.SetLog(log);
                    command.SetClient(client);
                    command.SetOutId(className + " " + outId);
                    if (!runningCommands.TryGetValue(className, out Dictionary<string, Command> commandContainer))
                    {
                        commandContainer = new Dictionary<String, Command>();
                        runningCommands.Add(className, commandContainer);
                    }
                    commandContainer.Add("" + outId, command);
                    return command;
                }
                catch (Exception e)
                {
                    VRCModLogger.LogError(e.ToString());
                }
            }
            return null;
        }
    }
}